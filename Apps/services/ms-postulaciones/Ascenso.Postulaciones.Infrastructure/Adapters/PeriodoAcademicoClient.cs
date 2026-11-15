using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Domain.Interfaces.Contratos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Cliente HTTP fail-closed para consultar el periodo académico vigente en MS-PLT.
/// </summary>
public sealed class PeriodoAcademicoClient : IPeriodoAcademicoClient
{
    private static readonly Uri _rutaPeriodoActual =
        new("internal/v1/periodos-academicos/actual", UriKind.Relative);

    private static readonly JsonSerializerOptions _opcionesJson = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Exception?> _logFalloConsulta =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1, nameof(PeriodoAcademicoClient)),
            "No fue posible consultar el periodo académico vigente desde MS-PLT.");

    private static readonly Action<ILogger, Guid, Exception?> _logUsoFallback =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(2, nameof(PeriodoAcademicoClient)),
            "Usando PeriodoActual:FallbackId ({PeriodoId}) en entorno Development ante indisponibilidad de MS-PLT.");

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiciosExternosOptions> _serviciosOptions;
    private readonly IOptions<PeriodoActualOptions> _periodoOptions;
    private readonly IHostEnvironment _entorno;
    private readonly ILogger<PeriodoAcademicoClient> _logger;

    public PeriodoAcademicoClient(
        HttpClient httpClient,
        IOptions<ServiciosExternosOptions> serviciosOptions,
        IOptions<PeriodoActualOptions> periodoOptions,
        IHostEnvironment entorno,
        ILogger<PeriodoAcademicoClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviciosOptions = serviciosOptions ?? throw new ArgumentNullException(nameof(serviciosOptions));
        _periodoOptions = periodoOptions ?? throw new ArgumentNullException(nameof(periodoOptions));
        _entorno = entorno ?? throw new ArgumentNullException(nameof(entorno));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PeriodoAcademicoActual> ObtenerPeriodoActualAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IntentarResolverUriConsulta(out var uriConsulta))
        {
            return ResolverFallbackOFallar("El servicio de calendario académico no está configurado.");
        }

        try
        {
            using var respuesta = await _httpClient.GetAsync(uriConsulta, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, null);
                return ResolverFallbackOFallar(
                    $"El servicio de calendario académico respondió con código {(int)respuesta.StatusCode}.");
            }

            var dto = await respuesta.Content
                .ReadFromJsonAsync<PeriodoAcademicoRespuestaDto>(_opcionesJson, cancellationToken);

            if (dto is null || dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Estado))
            {
                _logFalloConsulta(_logger, null);
                return ResolverFallbackOFallar(
                    "La respuesta del servicio de calendario académico es inválida.");
            }

            var estado = CodigoEnum.DesdeCodigo<EstadoPeriodoAcademico>(dto.Estado);
            var nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? dto.Id.ToString() : dto.Nombre.Trim();

            return new PeriodoAcademicoActual(dto.Id, nombre, estado);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("La consulta del periodo académico fue cancelada.", ex, cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logFalloConsulta(_logger, ex);
            return ResolverFallbackOFallar("Tiempo de espera agotado al consultar el calendario académico.");
        }
        catch (HttpRequestException ex)
        {
            _logFalloConsulta(_logger, ex);
            return ResolverFallbackOFallar("Error de comunicación al consultar el calendario académico.");
        }
        catch (JsonException ex)
        {
            _logFalloConsulta(_logger, ex);
            return ResolverFallbackOFallar("Respuesta JSON inválida del calendario académico.");
        }
        catch (ArgumentException ex)
        {
            _logFalloConsulta(_logger, ex);
            return ResolverFallbackOFallar("Estado de periodo académico desconocido en la respuesta externa.");
        }
    }

    private bool IntentarResolverUriConsulta(out Uri uriConsulta)
    {
        var urlBase = _serviciosOptions.Value.Plataforma.UrlBase;
        if (!string.IsNullOrWhiteSpace(urlBase)
            && Uri.TryCreate(urlBase.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            uriConsulta = new Uri(baseUri, _rutaPeriodoActual);
            return true;
        }

        if (_httpClient.BaseAddress is not null)
        {
            uriConsulta = new Uri(_httpClient.BaseAddress, _rutaPeriodoActual);
            return true;
        }

        uriConsulta = _rutaPeriodoActual;
        return false;
    }

    private PeriodoAcademicoActual ResolverFallbackOFallar(string mensajeFallo)
    {
        if (_entorno.IsDevelopment()
            && Guid.TryParse(_periodoOptions.Value.FallbackId, out var fallbackId)
            && fallbackId != Guid.Empty)
        {
            _logUsoFallback(_logger, fallbackId, null);
            return new PeriodoAcademicoActual(
                fallbackId,
                "Periodo Development (Fallback)",
                EstadoPeriodoAcademico.Vigente);
        }

        throw new DependenciaNoDisponibleException(mensajeFallo);
    }

    private sealed record class PeriodoAcademicoRespuestaDto(
        Guid Id,
        string? Nombre,
        string? Estado);
}
