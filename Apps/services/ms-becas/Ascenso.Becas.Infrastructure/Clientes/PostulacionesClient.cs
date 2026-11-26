using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Cliente de <c>MS-POS</c> para consultar la adjudicabilidad y marcar una
/// postulación como otorgada. Propaga <c>X-Correlation-Id</c>, aplica el
/// timeout del <see cref="HttpClient"/> y traduce los rechazos del proveedor.
/// </summary>
public sealed class PostulacionesClient : IPostulacionesClient
{
    public const string EncabezadoIdempotencia = "Idempotency-Key";

    private readonly HttpClient _cliente;
    private readonly ICorrelacionContexto _correlacion;

    public PostulacionesClient(
        HttpClient cliente,
        ICorrelacionContexto correlacion)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _correlacion = correlacion ?? throw new ArgumentNullException(nameof(correlacion));
    }

    public async Task<ResultadoAdjudicabilidad> ObtenerAdjudicabilidadAsync(
        Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        using var solicitud = SolicitudIntegracion.Crear(
            HttpMethod.Get,
            $"internal/v1/postulaciones/{postulacionId}/adjudicable",
            _correlacion);

        using var respuesta = await _cliente.SendAsync(
            solicitud,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            throw new PostulacionNoEncontradaException(postulacionId);
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-POS respondió {(int)respuesta.StatusCode} al consultar la adjudicabilidad.",
                inner: null,
                respuesta.StatusCode);
        }

        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        RespuestaAdjudicabilidad? resultado;

        try
        {
            resultado = JsonSerializer.Deserialize<RespuestaAdjudicabilidad>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);
        }
        catch (JsonException excepcion)
        {
            throw new HttpRequestException(
                "MS-POS devolvió una respuesta de adjudicabilidad inválida.",
                excepcion,
                respuesta.StatusCode);
        }

        if (resultado is null)
        {
            throw new HttpRequestException(
                "MS-POS devolvió una respuesta de adjudicabilidad vacía.",
                inner: null,
                respuesta.StatusCode);
        }

        return new ResultadoAdjudicabilidad(
            resultado.PostulacionId == Guid.Empty
                ? postulacionId
                : resultado.PostulacionId,
            resultado.Adjudicable,
            resultado.ProgramaBecaId,
            resultado.PeriodoAcademicoId,
            resultado.PeriodoCodigo ?? string.Empty,
            resultado.UniversidadAliadaId,
            resultado.CarreraId,
            resultado.PostulanteEstudianteId,
            resultado.NombreEstudiante ?? string.Empty,
            resultado.NumeroDocumentoEstudiante ?? string.Empty,
            resultado.ConvocatoriaOrigenId,
            resultado.MotivoNoAdjudicable);
    }

    public async Task OtorgarAsync(
        Guid postulacionId,
        Guid ofertaId,
        CancellationToken cancellationToken = default)
    {
        if (postulacionId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la postulación es obligatorio.",
                nameof(postulacionId));
        }

        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la oferta es obligatorio.",
                nameof(ofertaId));
        }

        using var solicitud = SolicitudIntegracion.Crear(
            HttpMethod.Post,
            $"internal/v1/postulaciones/{postulacionId}/otorgar",
            _correlacion);

        solicitud.Headers.TryAddWithoutValidation(
            EncabezadoIdempotencia,
            $"otorgar-{ofertaId}");

        using var respuesta = await _cliente.SendAsync(
            solicitud,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (respuesta.StatusCode is HttpStatusCode.Conflict
            or HttpStatusCode.UnprocessableEntity)
        {
            var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
                respuesta,
                cancellationToken);
            var error = LeerError(cuerpo);
            var estado = (int)respuesta.StatusCode;

            throw new PostulacionOtorgamientoRechazadaException(
                string.IsNullOrWhiteSpace(error.Codigo)
                    ? estado == 409
                        ? "POSTULACION_NO_TRANSICIONABLE"
                        : "POSTULACION_NO_OTORGABLE"
                    : error.Codigo,
                string.IsNullOrWhiteSpace(error.Mensaje)
                    ? "MS-POS rechazó el otorgamiento de la postulación."
                    : error.Mensaje,
                estado);
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-POS respondió {(int)respuesta.StatusCode} al otorgar la postulación.",
                inner: null,
                respuesta.StatusCode);
        }
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

    private sealed record RespuestaAdjudicabilidad
    {
        [JsonPropertyName("postulacionId")]
        public Guid PostulacionId { get; init; }

        [JsonPropertyName("adjudicable")]
        public bool Adjudicable { get; init; }

        [JsonPropertyName("programaBecaId")]
        public Guid ProgramaBecaId { get; init; }

        [JsonPropertyName("periodoAcademicoId")]
        public Guid PeriodoAcademicoId { get; init; }

        [JsonPropertyName("periodoCodigo")]
        public string? PeriodoCodigo { get; init; }

        [JsonPropertyName("universidadAliadaId")]
        public Guid UniversidadAliadaId { get; init; }

        [JsonPropertyName("carreraId")]
        public Guid CarreraId { get; init; }

        [JsonPropertyName("postulanteEstudianteId")]
        public Guid PostulanteEstudianteId { get; init; }

        [JsonPropertyName("nombreEstudiante")]
        public string? NombreEstudiante { get; init; }

        [JsonPropertyName("numeroDocumentoEstudiante")]
        public string? NumeroDocumentoEstudiante { get; init; }

        [JsonPropertyName("convocatoriaOrigenId")]
        public Guid ConvocatoriaOrigenId { get; init; }

        [JsonPropertyName("motivoNoAdjudicable")]
        public string? MotivoNoAdjudicable { get; init; }
    }
}
