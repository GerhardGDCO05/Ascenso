using Ascenso.Postulaciones.Api.Seguridad;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.PublicarRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;
using Ascenso.Postulaciones.Application.Rubricas.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Postulaciones.Api.Controllers.v1;

[ApiController]
[Route("api/v1/rubricas")]
public sealed class RubricasController : ControllerBase
{
    private readonly CrearRubricaCommandHandler _crearRubricaHandler;
    private readonly ObtenerRubricaQueryHandler _obtenerRubricaHandler;
    private readonly AgregarCriterioCommandHandler _agregarCriterioHandler;
    private readonly EditarCriterioCommandHandler _editarCriterioHandler;
    private readonly EliminarCriterioCommandHandler _eliminarCriterioHandler;
    private readonly PublicarRubricaCommandHandler _publicarRubricaHandler;

    public RubricasController(
        CrearRubricaCommandHandler crearRubricaHandler,
        ObtenerRubricaQueryHandler obtenerRubricaHandler,
        AgregarCriterioCommandHandler agregarCriterioHandler,
        EditarCriterioCommandHandler editarCriterioHandler,
        EliminarCriterioCommandHandler eliminarCriterioHandler,
        PublicarRubricaCommandHandler publicarRubricaHandler)
    {
        _crearRubricaHandler = crearRubricaHandler ?? throw new ArgumentNullException(nameof(crearRubricaHandler));
        _obtenerRubricaHandler = obtenerRubricaHandler ?? throw new ArgumentNullException(nameof(obtenerRubricaHandler));
        _agregarCriterioHandler = agregarCriterioHandler ?? throw new ArgumentNullException(nameof(agregarCriterioHandler));
        _editarCriterioHandler = editarCriterioHandler ?? throw new ArgumentNullException(nameof(editarCriterioHandler));
        _eliminarCriterioHandler = eliminarCriterioHandler ?? throw new ArgumentNullException(nameof(eliminarCriterioHandler));
        _publicarRubricaHandler = publicarRubricaHandler ?? throw new ArgumentNullException(nameof(publicarRubricaHandler));
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> Crear(
        [FromBody] CrearRubricaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new CrearRubricaCommand(
            request.ProgramaBecaId,
            request.ConvocatoriaId,
            request.Nombre);

        var resultado = await _crearRubricaHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PoliticasAutorizacionRubricas.LecturaRubrica)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubricaDto>> ObtenerPorId(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerRubricaHandler.HandleAsync(
            new ObtenerRubricaQuery(id),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("{id:guid}/criterios")]
    [Authorize(Policy = PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> AgregarCriterio(
        [FromRoute] Guid id,
        [FromBody] GuardarCriterioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new AgregarCriterioCommand(
            id,
            request.Nombre,
            request.Descripcion,
            request.PesoPorcentual,
            request.PuntajeMinimo,
            request.PuntajeMaximo);

        var resultado = await _agregarCriterioHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
    }

    [HttpPut("{id:guid}/criterios/{criterioId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> EditarCriterio(
        [FromRoute] Guid id,
        [FromRoute] Guid criterioId,
        [FromBody] GuardarCriterioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new EditarCriterioCommand(
            id,
            criterioId,
            request.Nombre,
            request.Descripcion,
            request.PesoPorcentual,
            request.PuntajeMinimo,
            request.PuntajeMaximo);

        var resultado = await _editarCriterioHandler.HandleAsync(comando, cancellationToken);

        return Ok(resultado);
    }

    [HttpDelete("{id:guid}/criterios/{criterioId:guid}")]
    [Authorize(Policy = PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RubricaDto>> EliminarCriterio(
        [FromRoute] Guid id,
        [FromRoute] Guid criterioId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _eliminarCriterioHandler.HandleAsync(
            new EliminarCriterioCommand(id, criterioId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("{id:guid}/publicar")]
    [Authorize(Policy = PoliticasAutorizacionRubricas.CoordinadorPrograma)]
    [ProducesResponseType(typeof(RubricaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RubricaDto>> Publicar(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _publicarRubricaHandler.HandleAsync(
            new PublicarRubricaCommand(id),
            cancellationToken);

        return Ok(resultado);
    }

    public sealed record class CrearRubricaRequest(
        Guid ProgramaBecaId,
        Guid ConvocatoriaId,
        string Nombre);

    public sealed record class GuardarCriterioRequest(
        string Nombre,
        string Descripcion,
        decimal PesoPorcentual,
        decimal PuntajeMinimo,
        decimal PuntajeMaximo);
}
