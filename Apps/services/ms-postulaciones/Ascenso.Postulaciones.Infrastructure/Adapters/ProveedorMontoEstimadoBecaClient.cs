using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Cliente HTTP fail-closed que proyecta en lote el monto estimado de beca por postulación a partir de la política de cobertura del programa en MS-PRG.
/// </summary>
public sealed class ProveedorMontoEstimadoBecaClient : IProveedorMontoEstimadoBeca
{
    private const string MarcadorProgramaId = "{programaId}";

    private static readonly JsonSerializerOptions _opcionesJson = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Guid, Exception?> _logFalloConsulta =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(ProveedorMontoEstimadoBecaClient)),
            "No fue posible consultar la política de cobertura del programa {ProgramaBecaId} desde MS-PRG.");

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiciosExternosOptions> _serviciosOptions;
    private readonly IOptions<RankingOptions> _rankingOptions;
    private readonly ILogger<ProveedorMontoEstimadoBecaClient> _logger;

    public ProveedorMontoEstimadoBecaClient(
        HttpClient httpClient,
        IOptions<ServiciosExternosOptions> serviciosOptions,
        IOptions<RankingOptions> rankingOptions,
        ILogger<ProveedorMontoEstimadoBecaClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviciosOptions = serviciosOptions ?? throw new ArgumentNullException(nameof(serviciosOptions));
        _rankingOptions = rankingOptions ?? throw new ArgumentNullException(nameof(rankingOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> ObtenerMontosEstimadosPorPostulacionAsync(
        Guid convocatoriaId,
        Guid programaBecaId,
        IReadOnlyCollection<Guid> postulacionIds,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (programaBecaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa de beca es obligatorio.",
                nameof(programaBecaId));
        }

        ArgumentNullException.ThrowIfNull(postulacionIds);

        foreach (var postulacionId in postulacionIds)
        {
            if (postulacionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Los identificadores de postulación no pueden ser vacíos.",
                    nameof(postulacionIds));
            }
        }

        if (postulacionIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var plantillaRuta = _rankingOptions.Value.RutaDetallePrograma;
        if (string.IsNullOrWhiteSpace(plantillaRuta))
        {
            throw new DependenciaNoDisponibleException(
                "La ruta de detalle del programa de beca no está configurada.");
        }

        var rutaPrograma = plantillaRuta
            .Trim()
            .TrimStart('/')
            .Replace(MarcadorProgramaId, programaBecaId.ToString("D"), StringComparison.OrdinalIgnoreCase);

        if (!IntentarResolverUriRelativa(rutaPrograma, out var uriPrograma))
        {
            throw new DependenciaNoDisponibleException(
                "El servicio de programas de beca no está configurado.");
        }

        try
        {
            using var respuesta = await _httpClient.GetAsync(uriPrograma, cancellationToken);

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ProgramaBecaNoEncontradoException();
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, programaBecaId, null);
                throw new DependenciaNoDisponibleException(
                    $"El servicio de programas respondió con código {(int)respuesta.StatusCode}.");
            }

            var dtoPrograma = await respuesta.Content
                .ReadFromJsonAsync<ProgramaDetalleRespuestaDto>(_opcionesJson, cancellationToken);

            if (dtoPrograma is null
                || (dtoPrograma.Id.HasValue
                    && dtoPrograma.Id.Value != Guid.Empty
                    && dtoPrograma.Id.Value != programaBecaId))
            {
                _logFalloConsulta(_logger, programaBecaId, null);
                throw new DependenciaNoDisponibleException(
                    "La respuesta de detalle del programa de beca es inválida.");
            }

            var montoEstimado = CalcularMontoEstimado(dtoPrograma.PoliticaCobertura);
            if (montoEstimado <= 0.00m)
            {
                _logFalloConsulta(_logger, programaBecaId, null);
                throw new DependenciaNoDisponibleException(
                    "No fue posible determinar un monto estimado de beca mayor a cero desde la política de cobertura del programa.");
            }

            var resultado = new Dictionary<Guid, decimal>(postulacionIds.Count);
            foreach (var postulacionId in postulacionIds)
            {
                resultado[postulacionId] = montoEstimado;
            }

            return resultado;
        }
        catch (ProgramaBecaNoEncontradoException)
        {
            throw;
        }
        catch (DependenciaNoDisponibleException)
        {
            throw;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(
                "La consulta de monto estimado de beca fue cancelada.",
                ex,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logFalloConsulta(_logger, programaBecaId, ex);
            throw new DependenciaNoDisponibleException(
                "Tiempo de espera agotado al consultar el monto estimado de beca.");
        }
        catch (HttpRequestException ex)
        {
            _logFalloConsulta(_logger, programaBecaId, ex);
            throw new DependenciaNoDisponibleException(
                "Error de comunicación al consultar el monto estimado de beca.");
        }
        catch (JsonException ex)
        {
            _logFalloConsulta(_logger, programaBecaId, ex);
            throw new DependenciaNoDisponibleException(
                "Respuesta JSON inválida al consultar el monto estimado de beca.");
        }
    }

    private decimal CalcularMontoEstimado(PoliticaCoberturaRespuestaDto? politica)
    {
        if (politica?.MontoFijoPeriodo is > 0.00m)
        {
            return decimal.Round(politica.MontoFijoPeriodo.Value, 2, MidpointRounding.AwayFromZero);
        }

        var montoReferencia = _rankingOptions.Value.MontoEstimadoBecaReferenciaPorDefecto;
        if (montoReferencia <= 0.00m)
        {
            return 0.00m;
        }

        if (string.Equals(politica?.Modalidad?.Trim(), "PARCIAL", StringComparison.OrdinalIgnoreCase)
            && politica?.PorcentajeCobertura is > 0.00m)
        {
            return decimal.Round(
                montoReferencia * (politica.PorcentajeCobertura.Value / 100m),
                2,
                MidpointRounding.AwayFromZero);
        }

        return decimal.Round(montoReferencia, 2, MidpointRounding.AwayFromZero);
    }

    private bool IntentarResolverUriRelativa(string rutaRelativaTexto, out Uri uriConsulta)
    {
        var rutaRelativa = new Uri(rutaRelativaTexto.TrimStart('/'), UriKind.Relative);
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

    private sealed record class ProgramaDetalleRespuestaDto(
        Guid? Id,
        PoliticaCoberturaRespuestaDto? PoliticaCobertura);

    private sealed record class PoliticaCoberturaRespuestaDto(
        string? Modalidad,
        decimal? PorcentajeCobertura,
        decimal? MontoFijoPeriodo,
        string? Moneda);
}
