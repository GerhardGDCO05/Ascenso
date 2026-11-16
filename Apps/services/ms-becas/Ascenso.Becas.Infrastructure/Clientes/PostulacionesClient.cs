using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Cliente de <c>MS-POS</c> para consultar la adjudicabilidad de una
/// postulación. Propaga <c>X-Correlation-Id</c>, aplica el timeout del
/// <see cref="HttpClient"/> y traduce el <c>404</c> al código interno
/// <c>POSTULACION_NO_ENCONTRADA</c>.
/// </summary>
public sealed class PostulacionesClient : IPostulacionesClient
{
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
