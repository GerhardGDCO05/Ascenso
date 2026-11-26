using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.EnviarEvaluacionDefinitiva;
using Ascenso.Postulaciones.Application.Evaluaciones.Comandos.GuardarBorradorEvaluacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesCoevaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerEvaluacionesPostulacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMiEvaluacionPorAsignacion;
using Ascenso.Postulaciones.Application.Evaluaciones.Consultas.ObtenerMisEvaluaciones;
using Ascenso.Postulaciones.Application.Evaluaciones.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/evaluaciones")]
public sealed class EvaluacionesController : ControllerBase
{
    private readonly GuardarBorradorEvaluacionCommandHandler _guardarBorradorHandler;
    private readonly EnviarEvaluacionDefinitivaCommandHandler _enviarEvaluacionHandler;
    private readonly ObtenerMiEvaluacionPorAsignacionQueryHandler _obtenerMiEvaluacionHandler;
    private readonly ObtenerMisEvaluacionesQueryHandler _obtenerMisEvaluacionesHandler;
    private readonly ObtenerEvaluacionesPostulacionQueryHandler _obtenerEvaluacionesPostulacionHandler;
    private readonly ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler _obtenerEvaluacionesCoevaluadoresHandler;

    public EvaluacionesController(
        GuardarBorradorEvaluacionCommandHandler guardarBorradorHandler,
        EnviarEvaluacionDefinitivaCommandHandler enviarEvaluacionHandler,
        ObtenerMiEvaluacionPorAsignacionQueryHandler obtenerMiEvaluacionHandler,
        ObtenerMisEvaluacionesQueryHandler obtenerMisEvaluacionesHandler,
        ObtenerEvaluacionesPostulacionQueryHandler obtenerEvaluacionesPostulacionHandler,
        ObtenerEvaluacionesCoevaluadoresPostulacionQueryHandler obtenerEvaluacionesCoevaluadoresHandler)
    {
        _guardarBorradorHandler = guardarBorradorHandler ?? throw new ArgumentNullException(nameof(guardarBorradorHandler));
        _enviarEvaluacionHandler = enviarEvaluacionHandler ?? throw new ArgumentNullException(nameof(enviarEvaluacionHandler));
        _obtenerMiEvaluacionHandler = obtenerMiEvaluacionHandler ?? throw new ArgumentNullException(nameof(obtenerMiEvaluacionHandler));
        _obtenerMisEvaluacionesHandler = obtenerMisEvaluacionesHandler ?? throw new ArgumentNullException(nameof(obtenerMisEvaluacionesHandler));
        _obtenerEvaluacionesPostulacionHandler = obtenerEvaluacionesPostulacionHandler ?? throw new ArgumentNullException(nameof(obtenerEvaluacionesPostulacionHandler));
        _obtenerEvaluacionesCoevaluadoresHandler = obtenerEvaluacionesCoevaluadoresHandler ?? throw new ArgumentNullException(nameof(obtenerEvaluacionesCoevaluadoresHandler));
    }

    [HttpPut("asignaciones/{asignacionEvaluacionId:guid}/borrador")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [ProducesResponseType(typeof(EvaluacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(EvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EvaluacionDto>> GuardarBorrador(
        [FromRoute] Guid asignacionEvaluacionId,
        [FromBody] GuardarBorradorEvaluacionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var calificaciones = (request.Calificaciones ?? [])
            .Select(item => new DatosCalificacionCriterioCommand(
                item.CriterioEvaluacionId,
                item.PuntajeAsignado,
                item.Comentario))
            .ToArray();

        var resultado = await _guardarBorradorHandler.HandleAsync(
            new GuardarBorradorEvaluacionCommand(
                asignacionEvaluacionId,
                calificaciones,
                request.ComentariosGenerales),
            cancellationToken);

        if (resultado.EsCreacion)
        {
            return CreatedAtAction(
                nameof(ObtenerMiEvaluacionPorAsignacion),
                new { asignacionEvaluacionId = resultado.Evaluacion.AsignacionEvaluacionId },
                resultado.Evaluacion);
        }

        return Ok(resultado.Evaluacion);
    }

    [HttpPost("asignaciones/{asignacionEvaluacionId:guid}/enviar")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [ProducesResponseType(typeof(EvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EvaluacionDto>> EnviarEvaluacionDefinitiva(
        [FromRoute] Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _enviarEvaluacionHandler.HandleAsync(
            new EnviarEvaluacionDefinitivaCommand(asignacionEvaluacionId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("asignaciones/{asignacionEvaluacionId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [ProducesResponseType(typeof(EvaluacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvaluacionDto>> ObtenerMiEvaluacionPorAsignacion(
        [FromRoute] Guid asignacionEvaluacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerMiEvaluacionHandler.HandleAsync(
            new ObtenerMiEvaluacionPorAsignacionQuery(asignacionEvaluacionId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("mis-evaluaciones")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [ProducesResponseType(typeof(PaginaDto<ResumenMiEvaluacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaDto<ResumenMiEvaluacionDto>>> ObtenerMisEvaluaciones(
        [FromQuery] string? estado = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = ObtenerMisEvaluacionesQuery.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        EstadoEvaluacion? estadoFiltro = string.IsNullOrWhiteSpace(estado)
            ? null
            : CodigoEnum.DesdeCodigo<EstadoEvaluacion>(estado);

        var resultado = await _obtenerMisEvaluacionesHandler.HandleAsync(
            new ObtenerMisEvaluacionesQuery(estadoFiltro, pagina, tamano),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("postulaciones/{postulacionId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.SupervisionEvaluaciones)]
    [ProducesResponseType(typeof(EvaluacionesPostulacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EvaluacionesPostulacionDto>> ObtenerPorPostulacion(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerEvaluacionesPostulacionHandler.HandleAsync(
            new ObtenerEvaluacionesPostulacionQuery(postulacionId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("postulaciones/{postulacionId:guid}/coevaluadores")]
    [Authorize(Policy = PoliticasAutorizacionEvaluaciones.GestionMisEvaluaciones)]
    [ProducesResponseType(typeof(EvaluacionesCoevaluadoresPostulacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EvaluacionesCoevaluadoresPostulacionDto>> ObtenerEvaluacionesCoevaluadores(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerEvaluacionesCoevaluadoresHandler.HandleAsync(
            new ObtenerEvaluacionesCoevaluadoresPostulacionQuery(postulacionId),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class CalificacionCriterioRequest(
        Guid CriterioEvaluacionId,
        decimal PuntajeAsignado,
        string? Comentario = null);

    public sealed record class GuardarBorradorEvaluacionRequest(
        IReadOnlyCollection<CalificacionCriterioRequest>? Calificaciones,
        string? ComentariosGenerales = null);
}
