using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Comites.Comandos.AgregarMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComiteEvaluador;
using Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComitePorPrograma;
using Ascenso.Postulaciones.Application.Comites.DTOs;
using Ascenso.Postulaciones.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/comites-evaluadores")]
[Authorize(Policy = PoliticasAutorizacionComites.GestionComitesEvaluadores)]
public sealed class ComitesEvaluadoresController : ControllerBase
{
    private readonly CrearComiteEvaluadorCommandHandler _crearComiteHandler;
    private readonly ObtenerComiteEvaluadorQueryHandler _obtenerComiteHandler;
    private readonly ObtenerComitePorProgramaQueryHandler _obtenerPorProgramaHandler;
    private readonly AgregarMiembroComiteCommandHandler _agregarMiembroHandler;
    private readonly DesvincularMiembroComiteCommandHandler _desvincularMiembroHandler;

    public ComitesEvaluadoresController(
        CrearComiteEvaluadorCommandHandler crearComiteHandler,
        ObtenerComiteEvaluadorQueryHandler obtenerComiteHandler,
        ObtenerComitePorProgramaQueryHandler obtenerPorProgramaHandler,
        AgregarMiembroComiteCommandHandler agregarMiembroHandler,
        DesvincularMiembroComiteCommandHandler desvincularMiembroHandler)
    {
        _crearComiteHandler = crearComiteHandler ?? throw new ArgumentNullException(nameof(crearComiteHandler));
        _obtenerComiteHandler = obtenerComiteHandler ?? throw new ArgumentNullException(nameof(obtenerComiteHandler));
        _obtenerPorProgramaHandler = obtenerPorProgramaHandler ?? throw new ArgumentNullException(nameof(obtenerPorProgramaHandler));
        _agregarMiembroHandler = agregarMiembroHandler ?? throw new ArgumentNullException(nameof(agregarMiembroHandler));
        _desvincularMiembroHandler = desvincularMiembroHandler ?? throw new ArgumentNullException(nameof(desvincularMiembroHandler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ComiteEvaluadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ComiteEvaluadorDto>> Crear(
        [FromBody] CrearComiteEvaluadorRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new CrearComiteEvaluadorCommand(
            request.ProgramaBecaId,
            request.ConvocatoriaId,
            request.Nombre,
            request.JefaturaUsuarioId);

        var resultado = await _crearComiteHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ComiteEvaluadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComiteEvaluadorDto>> ObtenerPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerComiteHandler.HandleAsync(
            new ObtenerComiteEvaluadorQuery(id),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("programas/{programaBecaId:guid}/convocatorias/{convocatoriaId:guid}")]
    [ProducesResponseType(typeof(ComiteEvaluadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComiteEvaluadorDto>> ObtenerPorProgramaYConvocatoria(
        [FromRoute] Guid programaBecaId,
        [FromRoute] Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorProgramaHandler.HandleAsync(
            new ObtenerComitePorProgramaQuery(programaBecaId, convocatoriaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("{id:guid}/miembros")]
    [ProducesResponseType(typeof(ComiteEvaluadorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ComiteEvaluadorDto>> AgregarMiembro(
        [FromRoute] Guid id,
        [FromBody] AgregarMiembroComiteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipo = CodigoEnum.DesdeCodigo<TipoEvaluador>(request.Tipo);
        var comando = new AgregarMiembroComiteCommand(
            id,
            request.UsuarioEvaluadorId,
            tipo);

        var resultado = await _agregarMiembroHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
    }

    [HttpDelete("{id:guid}/miembros/{miembroId:guid}")]
    [ProducesResponseType(typeof(ComiteEvaluadorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ComiteEvaluadorDto>> DesvincularMiembro(
        [FromRoute] Guid id,
        [FromRoute] Guid miembroId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _desvincularMiembroHandler.HandleAsync(
            new DesvincularMiembroComiteCommand(id, miembroId),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class CrearComiteEvaluadorRequest(
        Guid ProgramaBecaId,
        Guid ConvocatoriaId,
        string Nombre,
        Guid JefaturaUsuarioId);

    public sealed record class AgregarMiembroComiteRequest(
        Guid UsuarioEvaluadorId,
        string Tipo);
}
