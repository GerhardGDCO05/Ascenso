using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Cliente HTTP fail-closed para consultar universidades aliadas y carreras en MS-PRG.
/// </summary>
public sealed class CatalogoAcademicoClient : ICatalogoAcademicoClient
{
    private static readonly JsonSerializerOptions _opcionesJson = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Guid, Exception?> _logFalloConsulta =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(CatalogoAcademicoClient)),
            "No fue posible consultar la universidad {UniversidadId} desde MS-PRG.");

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiciosExternosOptions> _serviciosOptions;
    private readonly ILogger<CatalogoAcademicoClient> _logger;

    public CatalogoAcademicoClient(
        HttpClient httpClient,
        IOptions<ServiciosExternosOptions> serviciosOptions,
        ILogger<CatalogoAcademicoClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviciosOptions = serviciosOptions ?? throw new ArgumentNullException(nameof(serviciosOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UniversidadAliadaCatalogo> ObtenerUniversidadAsync(
        Guid universidadAliadaId,
        CancellationToken cancellationToken = default)
    {
        if (universidadAliadaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la universidad aliada es obligatorio.",
                nameof(universidadAliadaId));
        }

        if (!IntentarResolverUriConsulta(universidadAliadaId, out var uriConsulta))
        {
            throw new DependenciaNoDisponibleException(
                "El servicio de catálogo académico no está configurado.");
        }

        try
        {
            using var respuesta = await _httpClient.GetAsync(uriConsulta, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                throw new UniversidadNoElegibleException();
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, universidadAliadaId, null);
                throw new DependenciaNoDisponibleException(
                    $"El servicio de catálogo académico respondió con código {(int)respuesta.StatusCode}.");
            }

            var dto = await respuesta.Content
                .ReadFromJsonAsync<UniversidadDetalleRespuestaDto>(_opcionesJson, cancellationToken);

            if (dto is null || dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Estado))
            {
                _logFalloConsulta(_logger, universidadAliadaId, null);
                throw new DependenciaNoDisponibleException(
                    "La respuesta del servicio de catálogo académico es inválida.");
            }

            var estado = CodigoEnum.DesdeCodigo<EstadoUniversidadAliada>(dto.Estado);
            var carreras = (dto.Carreras ?? [])
                .Select(carrera => new CarreraCatalogo(
                    carrera.Id,
                    carrera.Nombre ?? string.Empty,
                    carrera.Activa))
                .ToArray();

            return new UniversidadAliadaCatalogo(
                dto.Id,
                dto.Nombre ?? string.Empty,
                estado,
                dto.TieneConvenioVigente,
                carreras);
        }
        catch (UniversidadNoElegibleException)
        {
            throw;
        }
        catch (DependenciaNoDisponibleException)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("La consulta del catálogo académico fue cancelada.", ex, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logFalloConsulta(_logger, universidadAliadaId, ex);
            throw new DependenciaNoDisponibleException(
                "Tiempo de espera agotado al consultar el catálogo académico.");
        }
        catch (HttpRequestException ex)
        {
            _logFalloConsulta(_logger, universidadAliadaId, ex);
            throw new DependenciaNoDisponibleException(
                "Error de comunicación al consultar el catálogo académico.");
        }
        catch (JsonException ex)
        {
            _logFalloConsulta(_logger, universidadAliadaId, ex);
            throw new DependenciaNoDisponibleException(
                "Respuesta JSON inválida del catálogo académico.");
        }
        catch (ArgumentException ex)
        {
            _logFalloConsulta(_logger, universidadAliadaId, ex);
            throw new DependenciaNoDisponibleException(
                "Estado de universidad desconocido en la respuesta del catálogo académico.");
        }
    }

    private bool IntentarResolverUriConsulta(Guid universidadAliadaId, out Uri uriConsulta)
    {
        var rutaRelativa = new Uri($"api/v1/universidades/{universidadAliadaId:D}", UriKind.Relative);
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

    private sealed record class UniversidadDetalleRespuestaDto(
        Guid Id,
        string? Nombre,
        string? Estado,
        bool TieneConvenioVigente,
        IReadOnlyList<CarreraDetalleRespuestaDto>? Carreras);

    private sealed record class CarreraDetalleRespuestaDto(
        Guid Id,
        string? Nombre,
        bool Activa);
}
