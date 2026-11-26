using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Cliente de <c>MS-PRG</c> para reservar cupo y presupuesto. Usa una
/// <c>Idempotency-Key</c> estable por referencia y traduce los
/// rechazos de negocio a los códigos internos del servicio.
/// </summary>
public sealed class ProgramasClient : IProgramasClient
{
    public const string EncabezadoIdempotencia = "Idempotency-Key";

    private readonly HttpClient _cliente;
    private readonly ICorrelacionContexto _correlacion;

    public ProgramasClient(
        HttpClient cliente,
        ICorrelacionContexto correlacion)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _correlacion = correlacion ?? throw new ArgumentNullException(nameof(correlacion));
    }

    public async Task<ResultadoCondicionesPrograma?> ObtenerCondicionesAsync(
        Guid programaBecaId,
        Guid carreraId,
        Guid periodoAcademicoId,
        Guid universidadAliadaId,
        CancellationToken cancellationToken = default)
    {
        ValidarIdentificador(programaBecaId, nameof(programaBecaId));
        ValidarIdentificador(carreraId, nameof(carreraId));
        ValidarIdentificador(periodoAcademicoId, nameof(periodoAcademicoId));
        ValidarIdentificador(universidadAliadaId, nameof(universidadAliadaId));

        using var peticion = SolicitudIntegracion.Crear(
            HttpMethod.Get,
            $"internal/v1/programas/{programaBecaId}/condiciones"
                + $"?carreraId={carreraId}&periodoId={periodoAcademicoId}",
            _correlacion);

        using var respuesta = await _cliente.SendAsync(
            peticion,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (respuesta.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var cuerpoError = await SolicitudIntegracion.LeerCuerpoAsync(
                respuesta,
                cancellationToken);

            if (string.Equals(
                    LeerError(cuerpoError).Codigo,
                    ReservaPresupuestoRechazadaException.CodigoConvenioVencido,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw ReservaPresupuestoRechazadaException.PorConvenioVencido();
            }
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-PRG respondió {(int)respuesta.StatusCode} al consultar las condiciones del programa.",
                inner: null,
                respuesta.StatusCode);
        }

        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        RespuestaCondiciones? resultado;

        try
        {
            resultado = JsonSerializer.Deserialize<RespuestaCondiciones>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);
        }
        catch (JsonException excepcion)
        {
            throw new HttpRequestException(
                "MS-PRG devolvió condiciones de programa inválidas.",
                excepcion,
                respuesta.StatusCode);
        }

        if (resultado is null
            || resultado.PoliticaCobertura is null
            || resultado.CondicionesPermanencia is null
            || resultado.CostoMatricula is null
            || resultado.Convenio is null)
        {
            throw new ConfiguracionProgramaNoDisponibleException(
                programaBecaId,
                "MS-PRG devolvió condiciones de programa incompletas.");
        }

        try
        {
            if (resultado.ProgramaBecaId != programaBecaId
                || resultado.CarreraId != carreraId
                || resultado.PeriodoAcademicoId != periodoAcademicoId
                || resultado.UniversidadAliadaId != universidadAliadaId
                || resultado.CostoMatricula.CarreraId != carreraId
                || resultado.CostoMatricula.PeriodoAcademicoId != periodoAcademicoId
                || resultado.Convenio.UniversidadAliadaId != universidadAliadaId)
            {
                throw new HttpRequestException(
                    "MS-PRG devolvió condiciones de otra postulación.",
                    inner: null,
                    respuesta.StatusCode);
            }

            if (resultado.CostoMatricula.CostoMatriculaReferenciaId == Guid.Empty
                || resultado.CostoMatricula.MontoReferenciaMatricula <= 0m
                || !string.Equals(
                    resultado.CostoMatricula.Moneda,
                    "USD",
                    StringComparison.OrdinalIgnoreCase)
                || resultado.CostoMatricula.NumeroVersion <= 0
                || resultado.CostoMatricula.FechaVigenciaDesde == default
                || string.IsNullOrWhiteSpace(resultado.CostoMatricula.Estado)
                || resultado.Convenio.ConvenioId == Guid.Empty
                || resultado.Convenio.FechaFin == default
                || string.IsNullOrWhiteSpace(resultado.Convenio.Estado))
            {
                throw new ConfiguracionProgramaNoDisponibleException(
                    programaBecaId,
                    "MS-PRG devolvió condiciones de programa incompletas.");
            }

            if (!TryParseEnum(
                    resultado.PoliticaCobertura.Modalidad,
                    out TipoCobertura modalidad)
                || !TryParseEnum(
                    resultado.CondicionesPermanencia.ConsecuenciaIncumplimiento,
                    out ConsecuenciaPermanencia consecuencia))
            {
                throw new ConfiguracionProgramaNoDisponibleException(
                    programaBecaId,
                    "MS-PRG devolvió una política o condición de permanencia desconocida.");
            }

            return new ResultadoCondicionesPrograma(
                resultado.ProgramaBecaId,
                resultado.CarreraId,
                resultado.PeriodoAcademicoId,
                resultado.UniversidadAliadaId,
                new PoliticaCobertura(
                    modalidad,
                    resultado.PoliticaCobertura.PorcentajeCobertura,
                    resultado.PoliticaCobertura.MontoFijoPeriodo,
                    resultado.PoliticaCobertura.Moneda),
                new CondicionesPermanencia(
                    resultado.CondicionesPermanencia.PromedioMinimoRenovacion,
                    resultado.CondicionesPermanencia.MateriasMinimasAprobadas,
                    resultado.CondicionesPermanencia.MaximoMateriasReprobadas,
                    resultado.CondicionesPermanencia.HorasServicioPorPeriodo,
                    consecuencia),
                new CostoMatriculaVigente(
                    resultado.CostoMatricula.CostoMatriculaReferenciaId,
                    resultado.CostoMatricula.CarreraId,
                    resultado.CostoMatricula.PeriodoAcademicoId,
                    resultado.CostoMatricula.MontoReferenciaMatricula,
                    resultado.CostoMatricula.Moneda,
                    resultado.CostoMatricula.NumeroVersion,
                    resultado.CostoMatricula.FechaVigenciaDesde,
                    resultado.CostoMatricula.FechaVigenciaHasta,
                    resultado.CostoMatricula.Estado),
                new ConvenioVigente(
                    resultado.Convenio.UniversidadAliadaId,
                    resultado.Convenio.ConvenioId,
                    resultado.Convenio.Estado,
                    resultado.Convenio.FechaFin));
        }
        catch (ArgumentException excepcion)
        {
            throw new ConfiguracionProgramaNoDisponibleException(
                programaBecaId,
                $"MS-PRG devolvió condiciones de programa inválidas: {excepcion.Message}");
        }
        catch (InvalidOperationException excepcion)
        {
            throw new ConfiguracionProgramaNoDisponibleException(
                programaBecaId,
                $"MS-PRG devolvió condiciones de programa inválidas: {excepcion.Message}");
        }
    }

    public async Task<ResultadoReservaPresupuesto> ReservarAsync(
        Guid programaBecaId,
        SolicitudReservaPresupuesto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);

        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa es obligatorio.",
                nameof(programaBecaId));
        }

        if (solicitud.ReferenciaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la referencia es obligatorio.",
                nameof(solicitud));
        }

        var prefijoIdempotencia = solicitud.ReferenciaTipo switch
        {
            "OFERTA_BECA" => "oferta",
            "RENOVACION" => "renovacion",
            _ => throw new ArgumentException(
                "El tipo de referencia de la reserva no es válido.", nameof(solicitud))
        };

        using var peticion = SolicitudIntegracion.Crear(
            HttpMethod.Post,
            $"internal/v1/programas/{programaBecaId}/reservas",
            _correlacion);

        peticion.Headers.TryAddWithoutValidation(
            EncabezadoIdempotencia,
            $"{prefijoIdempotencia}-{solicitud.ReferenciaId}");

        peticion.Content = SolicitudIntegracion.ContenidoJson(new
        {
            referenciaTipo = solicitud.ReferenciaTipo,
            referenciaId = solicitud.ReferenciaId,
            periodoAcademicoId = solicitud.PeriodoAcademicoId,
            monto = solicitud.Monto,
            cupos = solicitud.Cupos
        });

        using var respuesta = await _cliente.SendAsync(
            peticion,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode is HttpStatusCode.Conflict
            or HttpStatusCode.UnprocessableEntity)
        {
            throw await CrearRechazoAsync(respuesta, cancellationToken);
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-PRG respondió {(int)respuesta.StatusCode} al reservar presupuesto.",
                inner: null,
                respuesta.StatusCode);
        }

        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        RespuestaReserva? resultado;

        try
        {
            resultado = JsonSerializer.Deserialize<RespuestaReserva>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);
        }
        catch (JsonException excepcion)
        {
            throw new HttpRequestException(
                "MS-PRG devolvió una respuesta de reserva inválida.",
                excepcion,
                respuesta.StatusCode);
        }

        if (resultado is null)
        {
            throw new HttpRequestException(
                "MS-PRG devolvió una respuesta de reserva vacía.",
                inner: null,
                respuesta.StatusCode);
        }

        return new ResultadoReservaPresupuesto(
            resultado.ReservaId,
            resultado.Estado ?? string.Empty,
            resultado.Monto,
            resultado.Cupos,
            resultado.ExpiraEn);
    }

    public Task ConfirmarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaId,
        CancellationToken cancellationToken = default)
        => CambiarEstadoReservaAsync(
            programaBecaId, reservaId, referenciaId, "confirmar", cancellationToken);

    public Task ActivarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaId,
        CancellationToken cancellationToken = default)
        => CambiarEstadoReservaAsync(
            programaBecaId, reservaId, referenciaId, "activar", cancellationToken);

    public Task LiberarReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaId,
        CancellationToken cancellationToken = default)
        => CambiarEstadoReservaAsync(
            programaBecaId, reservaId, referenciaId, "liberar", cancellationToken);

    private async Task CambiarEstadoReservaAsync(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaId,
        string operacion,
        CancellationToken cancellationToken)
    {
        ValidarIdentificador(programaBecaId, nameof(programaBecaId));
        ValidarIdentificador(reservaId, nameof(reservaId));
        ValidarIdentificador(referenciaId, nameof(referenciaId));

        using var peticion = SolicitudIntegracion.Crear(
            HttpMethod.Post,
            $"internal/v1/programas/{programaBecaId}/reservas/{reservaId}/{operacion}",
            _correlacion);
        peticion.Headers.TryAddWithoutValidation(
            EncabezadoIdempotencia,
            $"{operacion}-{referenciaId}");

        using var respuesta = await _cliente.SendAsync(
            peticion,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-PRG respondió {(int)respuesta.StatusCode} al {operacion} la reserva.",
                inner: null,
                respuesta.StatusCode);
        }
    }

    private static async Task<ReservaPresupuestoRechazadaException> CrearRechazoAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        var error = LeerError(cuerpo);
        var codigo = error.Codigo ?? string.Empty;
        var mensaje = error.Mensaje ?? "MS-PRG rechazó la reserva de presupuesto.";

        if (string.Equals(
            codigo,
            ReservaPresupuestoRechazadaException.CodigoSinCupoOPresupuesto,
            StringComparison.OrdinalIgnoreCase))
        {
            return ReservaPresupuestoRechazadaException.PorFaltaDeCupoOPresupuesto();
        }

        if (string.Equals(
            codigo,
            ReservaPresupuestoRechazadaException.CodigoConvenioVencido,
            StringComparison.OrdinalIgnoreCase))
        {
            return ReservaPresupuestoRechazadaException.PorConvenioVencido();
        }

        return new ReservaPresupuestoRechazadaException(
            string.IsNullOrWhiteSpace(codigo)
                ? "RESERVA_PRESUPUESTO_RECHAZADA"
                : codigo,
            mensaje,
            (int)respuesta.StatusCode);
    }

    private static ErrorIntegracion LeerError(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return new ErrorIntegracion(null, null);
        }

        try
        {
            var error = JsonSerializer.Deserialize<RespuestaError>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);

            return new ErrorIntegracion(
                error?.Codigo ?? error?.Code,
                error?.Mensaje ?? error?.Message ?? error?.Detail);
        }
        catch (JsonException)
        {
            return new ErrorIntegracion(null, null);
        }
    }

    private sealed record ErrorIntegracion(string? Codigo, string? Mensaje);

    private static void ValidarIdentificador(Guid identificador, string nombre)
    {
        if (identificador == Guid.Empty)
        {
            throw new ArgumentException("El identificador es obligatorio.", nombre);
        }
    }

    private static bool TryParseEnum<TEnum>(string valor, out TEnum resultado)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            resultado = default;
            return false;
        }

        var normalizado = valor.Replace("_", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(normalizado, ignoreCase: true, out resultado)
            && Enum.IsDefined(resultado);
    }

    private sealed record RespuestaError
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; init; }

        [JsonPropertyName("code")]
        public string? Code { get; init; }

        [JsonPropertyName("mensaje")]
        public string? Mensaje { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }

        [JsonPropertyName("detail")]
        public string? Detail { get; init; }
    }

    private sealed record RespuestaCondiciones
    {
        [JsonPropertyName("programaBecaId")]
        public Guid ProgramaBecaId { get; init; }

        [JsonPropertyName("carreraId")]
        public Guid CarreraId { get; init; }

        [JsonPropertyName("periodoAcademicoId")]
        public Guid PeriodoAcademicoId { get; init; }

        [JsonPropertyName("universidadAliadaId")]
        public Guid UniversidadAliadaId { get; init; }

        [JsonPropertyName("politicaCobertura")]
        public RespuestaPoliticaCobertura? PoliticaCobertura { get; init; }

        [JsonPropertyName("condicionesPermanencia")]
        public RespuestaCondicionesPermanencia? CondicionesPermanencia { get; init; }

        [JsonPropertyName("costoMatricula")]
        public RespuestaCostoMatricula? CostoMatricula { get; init; }

        [JsonPropertyName("convenio")]
        public RespuestaConvenio? Convenio { get; init; }
    }

    private sealed record RespuestaPoliticaCobertura
    {
        [JsonPropertyName("modalidad")]
        public string Modalidad { get; init; } = string.Empty;

        [JsonPropertyName("porcentajeCobertura")]
        public decimal PorcentajeCobertura { get; init; }

        [JsonPropertyName("montoFijoPeriodo")]
        public decimal MontoFijoPeriodo { get; init; }

        [JsonPropertyName("moneda")]
        public string Moneda { get; init; } = string.Empty;
    }

    private sealed record RespuestaCondicionesPermanencia
    {
        [JsonPropertyName("promedioMinimoRenovacion")]
        public decimal PromedioMinimoRenovacion { get; init; }

        [JsonPropertyName("materiasMinimasAprobadas")]
        public int MateriasMinimasAprobadas { get; init; }

        [JsonPropertyName("maximoMateriasReprobadas")]
        public int MaximoMateriasReprobadas { get; init; }

        [JsonPropertyName("horasServicioPorPeriodo")]
        public int HorasServicioPorPeriodo { get; init; }

        [JsonPropertyName("consecuenciaIncumplimiento")]
        public string ConsecuenciaIncumplimiento { get; init; } = string.Empty;
    }

    private sealed record RespuestaCostoMatricula
    {
        [JsonPropertyName("costoMatriculaReferenciaId")]
        public Guid CostoMatriculaReferenciaId { get; init; }

        [JsonPropertyName("carreraId")]
        public Guid CarreraId { get; init; }

        [JsonPropertyName("periodoAcademicoId")]
        public Guid PeriodoAcademicoId { get; init; }

        [JsonPropertyName("montoReferenciaMatricula")]
        public decimal MontoReferenciaMatricula { get; init; }

        [JsonPropertyName("moneda")]
        public string Moneda { get; init; } = string.Empty;

        [JsonPropertyName("numeroVersion")]
        public int NumeroVersion { get; init; }

        [JsonPropertyName("fechaVigenciaDesde")]
        public DateOnly FechaVigenciaDesde { get; init; }

        [JsonPropertyName("fechaVigenciaHasta")]
        public DateOnly? FechaVigenciaHasta { get; init; }

        [JsonPropertyName("estado")]
        public string Estado { get; init; } = string.Empty;
    }

    private sealed record RespuestaConvenio
    {
        [JsonPropertyName("universidadAliadaId")]
        public Guid UniversidadAliadaId { get; init; }

        [JsonPropertyName("convenioId")]
        public Guid ConvenioId { get; init; }

        [JsonPropertyName("estado")]
        public string Estado { get; init; } = string.Empty;

        [JsonPropertyName("fechaFin")]
        public DateOnly FechaFin { get; init; }
    }

    private sealed record RespuestaReserva
    {
        [JsonPropertyName("reservaId")]
        public Guid ReservaId { get; init; }

        [JsonPropertyName("estado")]
        public string? Estado { get; init; }

        [JsonPropertyName("monto")]
        public decimal Monto { get; init; }

        [JsonPropertyName("cupos")]
        public int Cupos { get; init; }

        [JsonPropertyName("expiraEn")]
        public DateTimeOffset ExpiraEn { get; init; }
    }
}
