using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Domain.Excepciones;
using Microsoft.Extensions.Options;

namespace Ascenso.Programas.Infrastructure.Integraciones;

/// <summary>
/// Cliente *fail-closed* hacia MS-PLT para validar el periodo académico por identificador.
/// </summary>
public sealed class PeriodoAcademicoClient : IPeriodoAcademicoClient
{
    private readonly HttpClient _cliente;
    private readonly PeriodoAcademicoOptions _opciones;

    public PeriodoAcademicoClient(HttpClient cliente, IOptions<PeriodoAcademicoOptions> opciones)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        ArgumentNullException.ThrowIfNull(opciones);
        _opciones = opciones.Value;
    }

    public async Task<PeriodoAcademicoInformacion?> ObtenerPorIdAsync(
        string periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(periodoAcademicoId, out var periodoId) || periodoId == Guid.Empty)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_opciones.UrlBase))
        {
            throw new ServicioPeriodoAcademicoNoDisponibleException(
                "no está configurada la URL base de MS-PLT (ServiciosExternos:PeriodoAcademico:UrlBase).");
        }

        if (!Uri.TryCreate(_opciones.UrlBase, UriKind.Absolute, out var uriBase)
            || uriBase.Scheme is not ("http" or "https"))
        {
            throw new ServicioPeriodoAcademicoNoDisponibleException(
                "la URL base de MS-PLT no es una dirección HTTP o HTTPS absoluta válida.");
        }

        var url = new Uri(
            uriBase.ToString().TrimEnd('/') + $"/internal/v1/periodos-academicos/{periodoId:D}",
            UriKind.Absolute);

        try
        {
            using var respuesta = await _cliente.GetAsync(url, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new ServicioPeriodoAcademicoNoDisponibleException(
                    $"MS-PLT respondió {(int)respuesta.StatusCode} al consultar el periodo '{periodoId}'.");
            }

            var periodo = await respuesta.Content.ReadFromJsonAsync<PeriodoAcademicoRespuesta>(cancellationToken);
            if (periodo is null
                || periodo.Id == Guid.Empty
                || periodo.Id != periodoId
                || string.IsNullOrWhiteSpace(periodo.Nombre)
                || string.IsNullOrWhiteSpace(periodo.Estado))
            {
                throw new ServicioPeriodoAcademicoNoDisponibleException(
                    $"MS-PLT devolvió una respuesta inválida para el periodo '{periodoId}'.");
            }

            return new PeriodoAcademicoInformacion(
                periodo.Id,
                periodo.Nombre,
                periodo.Estado);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioPeriodoAcademicoNoDisponibleException(
                $"se agotó el tiempo de espera al consultar el periodo '{periodoId}'.");
        }
        catch (HttpRequestException ex)
        {
            throw new ServicioPeriodoAcademicoNoDisponibleException(
                $"no fue posible conectar con MS-PLT: {ex.Message}");
        }
        catch (JsonException ex)
        {
            throw new ServicioPeriodoAcademicoNoDisponibleException(
                $"MS-PLT devolvió JSON inválido para el periodo '{periodoId}': {ex.Message}");
        }
    }

    private sealed record PeriodoAcademicoRespuesta(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("nombre")] string? Nombre,
        [property: JsonPropertyName("estado")] string? Estado);
}
