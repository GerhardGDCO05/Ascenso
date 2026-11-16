using System.Text;
using System.Text.Json;
using Ascenso.Becas.Application.Correlacion;

namespace Ascenso.Becas.Infrastructure.Clientes;

/// <summary>
/// Utilidades compartidas por los clientes HTTP internos: serialización
/// camelCase y propagación del identificador de correlación.
/// </summary>
internal static class SolicitudIntegracion
{
    public static readonly JsonSerializerOptions OpcionesJson =
        new(JsonSerializerDefaults.Web);

    public static HttpRequestMessage Crear(
        HttpMethod metodo,
        string ruta,
        ICorrelacionContexto correlacion)
    {
        ArgumentNullException.ThrowIfNull(metodo);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruta);
        ArgumentNullException.ThrowIfNull(correlacion);

        var solicitud = new HttpRequestMessage(metodo, ruta);
        var correlationId = correlacion.CorrelationId;

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            solicitud.Headers.Remove(CorrelacionConstantes.Encabezado);
            solicitud.Headers.TryAddWithoutValidation(
                CorrelacionConstantes.Encabezado,
                correlationId);
        }

        return solicitud;
    }

    public static StringContent ContenidoJson<T>(T valor)
        => new(
            JsonSerializer.Serialize(valor, OpcionesJson),
            Encoding.UTF8,
            "application/json");

    public static async Task<string> LeerCuerpoAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(respuesta);

        return await respuesta.Content.ReadAsStringAsync(cancellationToken);
    }
}
