using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConfirmarRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ResolverCasoLimiteRanking;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerRankingConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/rankings")]
public sealed class RankingsController : ControllerBase
{
    private readonly ObtenerRankingConvocatoriaQueryHandler _obtenerRankingHandler;
    private readonly ConfirmarRankingConvocatoriaCommandHandler _confirmarRankingHandler;
    private readonly ResolverCasoLimiteRankingCommandHandler _resolverCasoLimiteHandler;

    public RankingsController(
        ObtenerRankingConvocatoriaQueryHandler obtenerRankingHandler,
        ConfirmarRankingConvocatoriaCommandHandler confirmarRankingHandler,
        ResolverCasoLimiteRankingCommandHandler resolverCasoLimiteHandler)
    {
        _obtenerRankingHandler = obtenerRankingHandler ?? throw new ArgumentNullException(nameof(obtenerRankingHandler));
        _confirmarRankingHandler = confirmarRankingHandler ?? throw new ArgumentNullException(nameof(confirmarRankingHandler));
        _resolverCasoLimiteHandler = resolverCasoLimiteHandler ?? throw new ArgumentNullException(nameof(resolverCasoLimiteHandler));
    }

    [HttpGet("convocatorias/{convocatoriaId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.ConsultaConsolidacion)]
    [ProducesResponseType(typeof(RankingConvocatoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RankingConvocatoriaDto>> ObtenerRankingPorConvocatoria(
        [FromRoute] Guid convocatoriaId,
        [FromQuery] string? clasificacion = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = ObtenerRankingConvocatoriaQuery.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerRankingHandler.HandleAsync(
            new ObtenerRankingConvocatoriaQuery(
                convocatoriaId,
                clasificacion,
                pagina,
                tamano),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/confirmar")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.GestionConsolidacion)]
    [ProducesResponseType(typeof(RankingConvocatoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RankingConvocatoriaDto>> ConfirmarRankingConvocatoria(
        [FromRoute] Guid convocatoriaId,
        [FromBody] ConfirmarRankingConvocatoriaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _confirmarRankingHandler.HandleAsync(
            new ConfirmarRankingConvocatoriaCommand(
                convocatoriaId,
                request?.ConfirmarConAdvertenciaDiscrepancias ?? false),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("convocatorias/{convocatoriaId:guid}/casos-limite/resolver")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.ResolucionCasosLimiteRanking)]
    [ProducesResponseType(typeof(RankingConvocatoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RankingConvocatoriaDto>> ResolverCasoLimiteRanking(
        [FromRoute] Guid convocatoriaId,
        [FromBody] ResolverCasoLimiteRankingRequest request,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _resolverCasoLimiteHandler.HandleAsync(
            new ResolverCasoLimiteRankingCommand(
                convocatoriaId,
                request?.PostulacionesSeleccionadasIds ?? [],
                request?.Justificacion ?? string.Empty),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class ConfirmarRankingConvocatoriaRequest(
        bool ConfirmarConAdvertenciaDiscrepancias = false);

    public sealed record class ResolverCasoLimiteRankingRequest(
        IReadOnlyList<Guid> PostulacionesSeleccionadasIds,
        string Justificacion);
}
