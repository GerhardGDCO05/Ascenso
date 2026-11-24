using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Asignaciones.Comandos.AsignarEvaluadoresPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerAsignacionesPostulacion;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerCargaEvaluadoresComite;
using Ascenso.Postulaciones.Application.Asignaciones.Consultas.ObtenerMisAsignaciones;
using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/asignaciones-evaluacion")]
public sealed class AsignacionesEvaluacionController : ControllerBase
{
    private readonly AsignarEvaluadoresPostulacionCommandHandler _asignarEvaluadoresHandler;
    private readonly ObtenerAsignacionesPostulacionQueryHandler _obtenerAsignacionesPostulacionHandler;
    private readonly ObtenerCargaEvaluadoresComiteQueryHandler _obtenerCargaComiteHandler;
    private readonly ObtenerMisAsignacionesQueryHandler _obtenerMisAsignacionesHandler;

    public AsignacionesEvaluacionController(
        AsignarEvaluadoresPostulacionCommandHandler asignarEvaluadoresHandler,
        ObtenerAsignacionesPostulacionQueryHandler obtenerAsignacionesPostulacionHandler,
        ObtenerCargaEvaluadoresComiteQueryHandler obtenerCargaComiteHandler,
        ObtenerMisAsignacionesQueryHandler obtenerMisAsignacionesHandler)
    {
        _asignarEvaluadoresHandler = asignarEvaluadoresHandler ?? throw new ArgumentNullException(nameof(asignarEvaluadoresHandler));
        _obtenerAsignacionesPostulacionHandler = obtenerAsignacionesPostulacionHandler ?? throw new ArgumentNullException(nameof(obtenerAsignacionesPostulacionHandler));
        _obtenerCargaComiteHandler = obtenerCargaComiteHandler ?? throw new ArgumentNullException(nameof(obtenerCargaComiteHandler));
        _obtenerMisAsignacionesHandler = obtenerMisAsignacionesHandler ?? throw new ArgumentNullException(nameof(obtenerMisAsignacionesHandler));
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion)]
    [ProducesResponseType(typeof(AsignacionesPostulacionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AsignacionesPostulacionDto>> AsignarEvaluadores(
        [FromBody] AsignarEvaluadoresPostulacionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new AsignarEvaluadoresPostulacionCommand(
            request.ComiteId,
            request.PostulacionId,
            request.FechaLimiteEvaluacion,
            request.UsuariosEvaluadoresIds,
            request.CantidadEvaluadores);

        var resultado = await _asignarEvaluadoresHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorPostulacion),
            new { postulacionId = resultado.PostulacionId },
            resultado);
    }

    [HttpGet("postulaciones/{postulacionId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion)]
    [ProducesResponseType(typeof(AsignacionesPostulacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AsignacionesPostulacionDto>> ObtenerPorPostulacion(
        [FromRoute] Guid postulacionId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerAsignacionesPostulacionHandler.HandleAsync(
            new ObtenerAsignacionesPostulacionQuery(postulacionId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("comites/{comiteId:guid}/carga")]
    [Authorize(Policy = PoliticasAutorizacionAsignaciones.GestionAsignacionesEvaluacion)]
    [ProducesResponseType(typeof(IReadOnlyList<CargaEvaluadorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CargaEvaluadorDto>>> ObtenerCargaPorComite(
        [FromRoute] Guid comiteId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerCargaComiteHandler.HandleAsync(
            new ObtenerCargaEvaluadoresComiteQuery(comiteId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("mis-asignaciones")]
    [Authorize(Policy = PoliticasAutorizacionAsignaciones.ConsultaMisAsignaciones)]
    [ProducesResponseType(typeof(PaginaDto<MiAsignacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaDto<MiAsignacionDto>>> ObtenerMisAsignaciones(
        [FromQuery] string? estado = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = ObtenerMisAsignacionesQuery.TamanoPorDefecto,
        CancellationToken cancellationToken = default)
    {
        EstadoAsignacionEvaluacion? estadoFiltro = string.IsNullOrWhiteSpace(estado)
            ? null
            : CodigoEnum.DesdeCodigo<EstadoAsignacionEvaluacion>(estado);

        var resultado = await _obtenerMisAsignacionesHandler.HandleAsync(
            new ObtenerMisAsignacionesQuery(estadoFiltro, pagina, tamano),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class AsignarEvaluadoresPostulacionRequest(
        Guid ComiteId,
        Guid PostulacionId,
        DateTimeOffset FechaLimiteEvaluacion,
        IReadOnlyCollection<Guid>? UsuariosEvaluadoresIds = null,
        int? CantidadEvaluadores = null);
}
