using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Cliente HTTP fail-closed para consultar la disponibilidad de cupo y presupuesto de una convocatoria en MS-PRG.
/// </summary>
public sealed class DisponibilidadRankingClient : IDisponibilidadRankingClient
{
    private const string MarcadorProgramaId = "{programaId}";

    private static readonly JsonSerializerOptions _opcionesJson = new(JsonSerializerDefaults.Web);

    private static readonly Action<ILogger, Guid, Exception?> _logFalloConsulta =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(1, nameof(DisponibilidadRankingClient)),
            "No fue posible consultar la disponibilidad de ranking de la convocatoria {ConvocatoriaId} desde MS-PRG.");

    private readonly HttpClient _httpClient;
    private readonly IOptions<ServiciosExternosOptions> _serviciosOptions;
    private readonly IOptions<RankingOptions> _rankingOptions;
    private readonly ILogger<DisponibilidadRankingClient> _logger;

    public DisponibilidadRankingClient(
        HttpClient httpClient,
        IOptions<ServiciosExternosOptions> serviciosOptions,
        IOptions<RankingOptions> rankingOptions,
        ILogger<DisponibilidadRankingClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _serviciosOptions = serviciosOptions ?? throw new ArgumentNullException(nameof(serviciosOptions));
        _rankingOptions = rankingOptions ?? throw new ArgumentNullException(nameof(rankingOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DisponibilidadProgramaRanking?> ObtenerDisponibilidadPorConvocatoriaAsync(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la convocatoria es obligatorio.",
                nameof(convocatoriaId));
        }

        if (!IntentarResolverUriRelativa(
                $"internal/v1/convocatorias/{convocatoriaId:D}",
                out var uriConvocatoria))
        {
            throw new DependenciaNoDisponibleException(
                "El servicio de programas y convocatorias no está configurado.");
        }

        try
        {
            using var respuestaConvocatoria = await _httpClient.GetAsync(uriConvocatoria, cancellationToken);

            if (respuestaConvocatoria.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!respuestaConvocatoria.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    $"El servicio de convocatorias respondió con código {(int)respuestaConvocatoria.StatusCode}.");
            }

            var dtoConvocatoria = await respuestaConvocatoria.Content
                .ReadFromJsonAsync<ConvocatoriaDisponibilidadRespuestaDto>(_opcionesJson, cancellationToken);

            if (dtoConvocatoria is null
                || dtoConvocatoria.Id == Guid.Empty
                || dtoConvocatoria.Id != convocatoriaId
                || dtoConvocatoria.ProgramaBecaId == Guid.Empty
                || dtoConvocatoria.CuposAsignados < 0)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    "La respuesta del servicio de convocatorias es inválida.");
            }

            var plantillaRuta = _rankingOptions.Value.RutaDisponibilidadPrograma;
            if (string.IsNullOrWhiteSpace(plantillaRuta))
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    "La ruta de disponibilidad de programa no está configurada.");
            }

            var rutaPrograma = plantillaRuta
                .Trim()
                .TrimStart('/')
                .Replace(MarcadorProgramaId, dtoConvocatoria.ProgramaBecaId.ToString("D"), StringComparison.OrdinalIgnoreCase);

            if (!IntentarResolverUriRelativa(rutaPrograma, out var uriDisponibilidad))
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    "No fue posible resolver la ruta de disponibilidad del programa.");
            }

            using var respuestaDisponibilidad = await _httpClient.GetAsync(uriDisponibilidad, cancellationToken);

            if (respuestaDisponibilidad.StatusCode == HttpStatusCode.NotFound)
            {
                throw new ProgramaBecaNoEncontradoException();
            }

            if (!respuestaDisponibilidad.IsSuccessStatusCode)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    $"El servicio de programas respondió con código {(int)respuestaDisponibilidad.StatusCode}.");
            }

            var dtoDisponibilidad = await respuestaDisponibilidad.Content
                .ReadFromJsonAsync<DisponibilidadProgramaRespuestaDto>(_opcionesJson, cancellationToken);

            if (dtoDisponibilidad is null
                || (dtoDisponibilidad.ProgramaBecaId.HasValue
                    && dtoDisponibilidad.ProgramaBecaId.Value != Guid.Empty
                    && dtoDisponibilidad.ProgramaBecaId.Value != dtoConvocatoria.ProgramaBecaId)
                || string.IsNullOrWhiteSpace(dtoDisponibilidad.Moneda)
                || dtoDisponibilidad.CuposDisponibles < 0
                || dtoDisponibilidad.PresupuestoDisponible < 0.00m
                || (dtoDisponibilidad.ComprometidoRenovacionesPendientes ?? 0.00m) < 0.00m)
            {
                _logFalloConsulta(_logger, convocatoriaId, null);
                throw new DependenciaNoDisponibleException(
                    "La respuesta de disponibilidad del programa de beca es inválida.");
            }

            var presupuestoBruto = decimal.Round(
                dtoDisponibilidad.PresupuestoDisponible,
                2,
                MidpointRounding.AwayFromZero);

            var comprometidoRenovaciones = decimal.Round(
                dtoDisponibilidad.ComprometidoRenovacionesPendientes ?? 0.00m,
                2,
                MidpointRounding.AwayFromZero);

            return new DisponibilidadProgramaRanking(
                convocatoriaId: dtoConvocatoria.Id,
                programaBecaId: dtoConvocatoria.ProgramaBecaId,
                cuposAsignadosConvocatoria: dtoConvocatoria.CuposAsignados,
                cuposDisponiblesPrograma: dtoDisponibilidad.CuposDisponibles,
                presupuestoDisponibleBruto: presupuestoBruto,
                presupuestoComprometidoRenovaciones: comprometidoRenovaciones,
                moneda: dtoDisponibilidad.Moneda);
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
                "La consulta de disponibilidad de ranking fue cancelada.",
                ex,
                cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Tiempo de espera agotado al consultar la disponibilidad de ranking.");
        }
        catch (HttpRequestException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Error de comunicación al consultar la disponibilidad de ranking.");
        }
        catch (JsonException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Respuesta JSON inválida al consultar la disponibilidad de ranking.");
        }
        catch (RankingInvalidoException ex)
        {
            _logFalloConsulta(_logger, convocatoriaId, ex);
            throw new DependenciaNoDisponibleException(
                "Datos de disponibilidad inválidos en la respuesta del servicio de programas.");
        }
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

    private sealed record class ConvocatoriaDisponibilidadRespuestaDto(
        Guid Id,
        Guid ProgramaBecaId,
        int CuposAsignados);

    private sealed record class DisponibilidadProgramaRespuestaDto(
        Guid? ProgramaBecaId,
        string? Moneda,
        decimal PresupuestoDisponible,
        decimal? ComprometidoRenovacionesPendientes,
        int CuposDisponibles);
}
