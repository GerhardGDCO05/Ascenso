using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Becas.Application.Correlacion;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Infrastructure.Clientes;

public sealed class PeriodosAcademicosClient : IPeriodosAcademicosClient
{
    private readonly HttpClient _cliente;
    private readonly ICorrelacionContexto _correlacion;

    public PeriodosAcademicosClient(
        HttpClient cliente,
        ICorrelacionContexto correlacion)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _correlacion = correlacion ?? throw new ArgumentNullException(nameof(correlacion));
    }

    public async Task<ResultadoPeriodoAcademico> ObtenerActualAsync(
        CancellationToken cancellationToken = default)
    {
        using var solicitud = SolicitudIntegracion.Crear(
            HttpMethod.Get,
            "internal/v1/periodos-academicos/actual",
            _correlacion);

        using var respuesta = await _cliente.SendAsync(
            solicitud,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MS-PLT respondió {(int)respuesta.StatusCode} al consultar el periodo académico actual.",
                inner: null,
                respuesta.StatusCode);
        }

        var cuerpo = await SolicitudIntegracion.LeerCuerpoAsync(
            respuesta,
            cancellationToken);

        RespuestaPeriodoActual? periodo;

        try
        {
            periodo = JsonSerializer.Deserialize<RespuestaPeriodoActual>(
                cuerpo,
                SolicitudIntegracion.OpcionesJson);
        }
        catch (JsonException excepcion)
        {
            throw new HttpRequestException(
                "MS-PLT devolvió un periodo académico actual inválido.",
                excepcion,
                respuesta.StatusCode);
        }

        if (periodo is null
            || periodo.PeriodoAcademicoId == Guid.Empty
            || string.IsNullOrWhiteSpace(periodo.Codigo)
            || string.IsNullOrWhiteSpace(periodo.Nombre)
            || periodo.OrdenSecuencia <= 0
            || periodo.FechaInicio == default
            || periodo.FechaFin < periodo.FechaInicio
            || !periodo.EsActual
            || !string.Equals(periodo.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException(
                "MS-PLT no devolvió un periodo académico activo y actual completo.",
                inner: null,
                respuesta.StatusCode);
        }

        return new ResultadoPeriodoAcademico(
            periodo.PeriodoAcademicoId,
            periodo.Codigo,
            periodo.Nombre,
            periodo.OrdenSecuencia,
            periodo.FechaInicio,
            periodo.FechaFin,
            periodo.Estado,
            periodo.EsActual);
    }

    private sealed record RespuestaPeriodoActual
    {
        [JsonPropertyName("periodoAcademicoId")]
        public Guid PeriodoAcademicoId { get; init; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; init; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; init; } = string.Empty;

        [JsonPropertyName("ordenSecuencia")]
        public int OrdenSecuencia { get; init; }

        [JsonPropertyName("fechaInicio")]
        public DateOnly FechaInicio { get; init; }

        [JsonPropertyName("fechaFin")]
        public DateOnly FechaFin { get; init; }

        [JsonPropertyName("estado")]
        public string Estado { get; init; } = string.Empty;

        [JsonPropertyName("esActual")]
        public bool EsActual { get; init; }
    }
}
