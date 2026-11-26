using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ConsultarAdjudicabilidadPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.Internal.v1;

[ApiController]
[Route("internal/v1/postulaciones")]
public sealed class PostulacionesInternasController : ControllerBase
{
    private readonly ConsultarAdjudicabilidadPostulacionQueryHandler _consultarAdjudicabilidadHandler;

    public PostulacionesInternasController(
        ConsultarAdjudicabilidadPostulacionQueryHandler consultarAdjudicabilidadHandler)
    {
        _consultarAdjudicabilidadHandler = consultarAdjudicabilidadHandler
            ?? throw new ArgumentNullException(nameof(consultarAdjudicabilidadHandler));
    }

    [HttpGet("{postulacionId:guid}/adjudicable")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.ConsultaAdjudicabilidadInterna)]
    [ProducesResponseType(typeof(AdjudicabilidadPostulacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdjudicabilidadPostulacionDto>> ConsultarAdjudicabilidad(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _consultarAdjudicabilidadHandler.HandleAsync(
            new ConsultarAdjudicabilidadPostulacionQuery(postulacionId),
            cancellationToken);

        return Ok(resultado);
    }
}
