using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Cliente de <c>MS-PRG</c> para reservar cupo y presupuesto al emitir una
/// oferta. Usa una <c>Idempotency-Key</c> estable por oferta y traduce los
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

        using var peticion = SolicitudIntegracion.Crear(
            HttpMethod.Post,
            $"internal/v1/programas/{programaBecaId}/reservas",
            _correlacion);

        peticion.Headers.TryAddWithoutValidation(
            EncabezadoIdempotencia,
            $"oferta-{solicitud.ReferenciaId}");

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
