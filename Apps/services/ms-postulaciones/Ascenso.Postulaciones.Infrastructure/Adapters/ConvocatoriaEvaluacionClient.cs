using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Cliente HTTP fail-closed para consultar la ventana formal de revisión de una convocatoria en MS-PRG.
/// </summary>
public sealed class ConvocatoriaEvaluacionClient : IConvocatoriaEvaluacionClient
{
    private static readonly JsonSerializerOptions _opcionesJson = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Guid, Exception?> _logFalloConsulta =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(ConvocatoriaEvaluacionClient)),
            "No fue posible consultar la ventana de revisión de la convocatoria {ConvocatoriaId} desde MS-PRG.");

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiciosExternosOptions> _serviciosOptions;
    private readonly ILogger<ConvocatoriaEvaluacionClient> _logger;

    public ConvocatoriaEvaluacionClient(
        HttpClient httpClient,
        IOptions<ServiciosExternosOptions> serviciosOptions,
        ILogger<ConvocatoriaEvaluacionClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviciosOptions = serviciosOptions ?? throw new ArgumentNullException(nameof(serviciosOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VentanaRevisionConvocatoria?> ObtenerVentanaRevisionAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (!IntentarResolverUriConsulta(convocatoriaId, out var uriConsulta))
        {
            throw new DependenciaNoDisponibleException(
                "El servicio de convocatorias no está configurado.");
        }

        try
        {
            using var respuesta = await _httpClient.GetAsync(uriConsulta, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    $"El servicio de convocatorias respondió con código {(int)respuesta.StatusCode}.");
            }

            var dto = await respuesta.Content
                .ReadFromJsonAsync<ConvocatoriaVentanaRevisionRespuestaDto>(_opcionesJson, cancellationToken);

            if (dto is null
                || dto.Id == Guid.Empty
                || dto.Id != convocatoriaId
                || string.IsNullOrWhiteSpace(dto.Estado)
                || dto.FechaInicioEvaluacion == default
                || dto.FechaCierreEvaluacion == default)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    "La respuesta del servicio de convocatorias es inválida.");
            }

            var estado = CodigoEnum.DesdeCodigo<EstadoConvocatoriaExterna>(dto.Estado);

            return new VentanaRevisionConvocatoria(
                dto.Id,
                estado,
                dto.FechaInicioEvaluacion,
                dto.FechaCierreEvaluacion);
        }
        catch (DependenciaNoDisponibleException)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "La consulta de la convocatoria fue cancelada.",
                ex,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Tiempo de espera agotado al consultar la convocatoria.");
        }
        catch (HttpRequestException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Error de comunicación al consultar la convocatoria.");
        }
        catch (JsonException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Respuesta JSON inválida al consultar la convocatoria.");
        }
        catch (ArgumentException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Datos de ventana de revisión inválidos en la respuesta del servicio de convocatorias.");
        }
    }

    private bool IntentarResolverUriConsulta(Guid convocatoriaId, out Uri uriConsulta)
    {
        var rutaRelativa = new Uri($"internal/v1/convocatorias/{convocatoriaId:D}", UriKind.Relative);
        var urlBase = _serviciosOptions.Value.Programas.UrlBase;

        if (!string.IsNullOrWhiteSpace(urlBase)
            && Uri.TryCreate(urlBase.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            uriConsulta = new Uri(baseUri, rutaRelativa);
            return true;
        }

        if (_httpClient.BaseAddress is not null)
        {
            uriConsulta = new Uri(_httpClient.BaseAddress, rutaRelativa);
            return true;
        }

        uriConsulta = rutaRelativa;
        return false;
    }

    private sealed record class ConvocatoriaVentanaRevisionRespuestaDto(
        Guid Id,
        string? Estado,
        DateTimeOffset FechaInicioEvaluacion,
        DateTimeOffset FechaCierreEvaluacion);
}
