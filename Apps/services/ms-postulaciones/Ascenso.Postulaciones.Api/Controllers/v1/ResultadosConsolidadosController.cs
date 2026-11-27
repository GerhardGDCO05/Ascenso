using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.ConsolidarPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.Comandos.RevisarDiscrepanciaConsolidacion;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ListarResultadosConsolidadosConvocatoria;
using Ascenso.Postulaciones.Application.Consolidacion.Consultas.ObtenerResultadoConsolidadoPostulacion;
using Ascenso.Postulaciones.Application.Consolidacion.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/resultados-consolidados")]
public sealed class ResultadosConsolidadosController : ControllerBase
{
    private readonly ConsolidarPostulacionCommandHandler _consolidarPostulacionHandler;
    private readonly RevisarDiscrepanciaConsolidacionCommandHandler _revisarDiscrepanciaHandler;
    private readonly ObtenerResultadoConsolidadoPostulacionQueryHandler _obtenerResultadoPostulacionHandler;
    private readonly ListarResultadosConsolidadosConvocatoriaQueryHandler _listarResultadosConvocatoriaHandler;

    public ResultadosConsolidadosController(
        ConsolidarPostulacionCommandHandler consolidarPostulacionHandler,
        RevisarDiscrepanciaConsolidacionCommandHandler revisarDiscrepanciaHandler,
        ObtenerResultadoConsolidadoPostulacionQueryHandler obtenerResultadoPostulacionHandler,
        ListarResultadosConsolidadosConvocatoriaQueryHandler listarResultadosConvocatoriaHandler)
    {
        _consolidarPostulacionHandler = consolidarPostulacionHandler ?? throw new ArgumentNullException(nameof(consolidarPostulacionHandler));
        _revisarDiscrepanciaHandler = revisarDiscrepanciaHandler ?? throw new ArgumentNullException(nameof(revisarDiscrepanciaHandler));
        _obtenerResultadoPostulacionHandler = obtenerResultadoPostulacionHandler ?? throw new ArgumentNullException(nameof(obtenerResultadoPostulacionHandler));
        _listarResultadosConvocatoriaHandler = listarResultadosConvocatoriaHandler ?? throw new ArgumentNullException(nameof(listarResultadosConvocatoriaHandler));
    }

    [HttpPost("postulaciones/{postulacionId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.GestionConsolidacion)]
    [ProducesResponseType(typeof(ResultadoConsolidadoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResultadoConsolidadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResultadoConsolidadoDto>> ConsolidarPostulacion(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _consolidarPostulacionHandler.HandleAsync(
            new ConsolidarPostulacionCommand(postulacionId),
            cancellationToken);

        if (resultado.EsCreacion)
        {
            return CreatedAtAction(
                nameof(ObtenerPorPostulacion),
                new { postulacionId = resultado.Resultado.PostulacionId },
                resultado.Resultado);
        }

        return Ok(resultado.Resultado);
    }

    [HttpPost("postulaciones/{postulacionId:guid}/discrepancia/revisar")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.GestionConsolidacion)]
    [ProducesResponseType(typeof(ResultadoConsolidadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResultadoConsolidadoDto>> RevisarDiscrepancia(
        [FromRoute] Guid postulacionId,
        [FromBody] RevisarDiscrepanciaConsolidacionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var resultado = await _revisarDiscrepanciaHandler.HandleAsync(
            new RevisarDiscrepanciaConsolidacionCommand(postulacionId, request.Comentario ?? string.Empty),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("postulaciones/{postulacionId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.ConsultaConsolidacion)]
    [ProducesResponseType(typeof(DetalleResultadoConsolidadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DetalleResultadoConsolidadoDto>> ObtenerPorPostulacion(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerResultadoPostulacionHandler.HandleAsync(
            new ObtenerResultadoConsolidadoPostulacionQuery(postulacionId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("convocatorias/{convocatoriaId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionConsolidacion.ConsultaConsolidacion)]
    [ProducesResponseType(typeof(PaginaDto<ResultadoConsolidadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaDto<ResultadoConsolidadoDto>>> ListarPorConvocatoria(
        [FromRoute] Guid convocatoriaId,
        [FromQuery] bool? alertaDiscrepancia = null,
        [FromQuery] bool? discrepanciaRevisada = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = ListarResultadosConsolidadosConvocatoriaQuery.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _listarResultadosConvocatoriaHandler.HandleAsync(
            new ListarResultadosConsolidadosConvocatoriaQuery(
                convocatoriaId,
                alertaDiscrepancia,
                discrepanciaRevisada,
                pagina,
                tamano),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class RevisarDiscrepanciaConsolidacionRequest(string? Comentario);
}
