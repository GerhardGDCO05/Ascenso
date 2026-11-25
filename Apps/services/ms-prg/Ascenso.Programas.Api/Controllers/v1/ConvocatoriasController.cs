using System.Security.Claims;
using Ascenso.Programas.Api.Seguridad;
using Ascenso.Programas.Application.Convocatorias.Comandos.AgregarDocumentoRequerido;
using Ascenso.Programas.Application.Convocatorias.Comandos.CrearConvocatoriaBorrador;
using Ascenso.Programas.Application.Convocatorias.Comandos.EditarConvocatoriaBorrador;
using Ascenso.Programas.Application.Convocatorias.Comandos.EliminarDocumentoRequerido;
using Ascenso.Programas.Application.Convocatorias.Comandos.PublicarConvocatoria;
using Ascenso.Programas.Application.Convocatorias.Consultas.ConsultarConvocatorias;
using Ascenso.Programas.Application.Convocatorias.Consultas.ObtenerConvocatoriaPorId;
using Ascenso.Programas.Application.Convocatorias.DTOs;
using Ascenso.Programas.Application.Donantes.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Controllers.v1;

/// <summary>
/// API pública de convocatorias: borradores, publicación, consulta y documentos requeridos.
/// </summary>
[ApiController]
[Route("api/v1/convocatorias")]
public sealed class ConvocatoriasController : ControllerBase
{
    private static readonly Guid _usuarioSistemaPorDefecto = Guid.Parse("01966000-0000-7000-8000-000000000002");

    private readonly CrearConvocatoriaBorradorCommandHandler _crearHandler;
    private readonly EditarConvocatoriaBorradorCommandHandler _editarHandler;
    private readonly PublicarConvocatoriaCommandHandler _publicarHandler;
    private readonly AgregarDocumentoRequeridoCommandHandler _agregarDocumentoHandler;
    private readonly EliminarDocumentoRequeridoCommandHandler _eliminarDocumentoHandler;
    private readonly ConsultarConvocatoriasQueryHandler _consultarHandler;
    private readonly ObtenerConvocatoriaPorIdQueryHandler _obtenerPorIdHandler;

    public ConvocatoriasController(
        CrearConvocatoriaBorradorCommandHandler crearHandler,
        EditarConvocatoriaBorradorCommandHandler editarHandler,
        PublicarConvocatoriaCommandHandler publicarHandler,
        AgregarDocumentoRequeridoCommandHandler agregarDocumentoHandler,
        EliminarDocumentoRequeridoCommandHandler eliminarDocumentoHandler,
        ConsultarConvocatoriasQueryHandler consultarHandler,
        ObtenerConvocatoriaPorIdQueryHandler obtenerPorIdHandler)
    {
        _crearHandler = crearHandler ?? throw new ArgumentNullException(nameof(crearHandler));
        _editarHandler = editarHandler ?? throw new ArgumentNullException(nameof(editarHandler));
        _publicarHandler = publicarHandler ?? throw new ArgumentNullException(nameof(publicarHandler));
        _agregarDocumentoHandler = agregarDocumentoHandler ?? throw new ArgumentNullException(nameof(agregarDocumentoHandler));
        _eliminarDocumentoHandler = eliminarDocumentoHandler ?? throw new ArgumentNullException(nameof(eliminarDocumentoHandler));
        _consultarHandler = consultarHandler ?? throw new ArgumentNullException(nameof(consultarHandler));
        _obtenerPorIdHandler = obtenerPorIdHandler ?? throw new ArgumentNullException(nameof(obtenerPorIdHandler));
    }

    [HttpPost]
    [Authorize(Policy = PermisosConvocatoria.Crear)]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> Crear(
        [FromBody] CrearConvocatoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new CrearConvocatoriaBorradorCommand(
            request.ProgramaBecaId,
            request.PeriodoAcademicoId,
            request.Codigo,
            request.Nombre,
            request.FechaApertura,
            request.FechaCierre,
            request.FechaLimiteSubsanacionDocumentos,
            request.FechaInicioEvaluacion,
            request.FechaCierreEvaluacion,
            request.RequiereRevisionDocumentalPrevia,
            request.CuposAsignados,
            request.CriteriosElegibilidad,
            ResolverActorId(request.UsuarioCreacionId));

        var resultado = await _crearHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { convocatoriaId = resultado.Id }, resultado);
    }

    [HttpGet]
    [Authorize(Policy = PermisosConvocatoria.Leer)]
    [ProducesResponseType(typeof(PaginadoResponse<ConvocatoriaResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginadoResponse<ConvocatoriaResumenDto>>> Consultar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] Guid? programaBecaId = null,
        [FromQuery] string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarConvocatoriasQuery(
            pagina,
            tamanoPagina,
            estado,
            programaBecaId,
            busqueda);

        var resultado = await _consultarHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{convocatoriaId:guid}")]
    [Authorize(Policy = PermisosConvocatoria.Leer)]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> ObtenerPorId(
        Guid convocatoriaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorIdHandler.HandleAsync(
            new ObtenerConvocatoriaPorIdQuery(convocatoriaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPut("{convocatoriaId:guid}")]
    [Authorize(Policy = PermisosConvocatoria.Editar)]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> Editar(
        Guid convocatoriaId,
        [FromBody] EditarConvocatoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new EditarConvocatoriaBorradorCommand(
            convocatoriaId,
            request.PeriodoAcademicoId,
            request.Nombre,
            request.FechaApertura,
            request.FechaCierre,
            request.FechaLimiteSubsanacionDocumentos,
            request.FechaInicioEvaluacion,
            request.FechaCierreEvaluacion,
            request.RequiereRevisionDocumentalPrevia,
            request.CuposAsignados,
            request.CriteriosElegibilidad,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _editarHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{convocatoriaId:guid}/publicar")]
    [Authorize(Policy = PermisosConvocatoria.Publicar)]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> Publicar(
        Guid convocatoriaId,
        [FromBody] PublicarConvocatoriaRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var comando = new PublicarConvocatoriaCommand(
            convocatoriaId,
            ResolverActorId(request?.UsuarioModificacionId));

        var resultado = await _publicarHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{convocatoriaId:guid}/documentos-requeridos")]
    [Authorize(Policy = PermisosConvocatoria.Editar)]
    [ProducesResponseType(typeof(DocumentoRequeridoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentoRequeridoDto>> AgregarDocumentoRequerido(
        Guid convocatoriaId,
        [FromBody] AgregarDocumentoRequeridoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new AgregarDocumentoRequeridoCommand(
            convocatoriaId,
            request.TipoDocumento,
            request.Nombre,
            request.Descripcion,
            request.Obligatorio,
            request.TiposMimePermitidos,
            request.TamanioMaximoMb,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _agregarDocumentoHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { convocatoriaId },
            resultado);
    }

    [HttpDelete("{convocatoriaId:guid}/documentos-requeridos/{documentoId:guid}")]
    [Authorize(Policy = PermisosConvocatoria.Editar)]
    [ProducesResponseType(typeof(ConvocatoriaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConvocatoriaDetalleDto>> EliminarDocumentoRequerido(
        Guid convocatoriaId,
        Guid documentoId,
        CancellationToken cancellationToken = default)
    {
        var comando = new EliminarDocumentoRequeridoCommand(
            convocatoriaId,
            documentoId,
            ResolverActorId(null));

        var resultado = await _eliminarDocumentoHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    private Guid ResolverActorId(Guid? actorExplicito)
    {
        if (actorExplicito.HasValue && actorExplicito.Value != Guid.Empty)
        {
            return actorExplicito.Value;
        }

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var parsed) && parsed != Guid.Empty
            ? parsed
            : _usuarioSistemaPorDefecto;
    }
}

public sealed record CrearConvocatoriaRequest(
    Guid ProgramaBecaId,
    string PeriodoAcademicoId,
    string Codigo,
    string Nombre,
    DateOnly FechaApertura,
    DateOnly FechaCierre,
    DateOnly FechaLimiteSubsanacionDocumentos,
    DateOnly FechaInicioEvaluacion,
    DateOnly FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    Guid? UsuarioCreacionId = null);

public sealed record EditarConvocatoriaRequest(
    string PeriodoAcademicoId,
    string Nombre,
    DateOnly FechaApertura,
    DateOnly FechaCierre,
    DateOnly FechaLimiteSubsanacionDocumentos,
    DateOnly FechaInicioEvaluacion,
    DateOnly FechaCierreEvaluacion,
    bool RequiereRevisionDocumentalPrevia,
    int CuposAsignados,
    CriteriosElegibilidadCicloDto CriteriosElegibilidad,
    Guid? UsuarioModificacionId = null);

public sealed record PublicarConvocatoriaRequest(Guid? UsuarioModificacionId = null);

public sealed record AgregarDocumentoRequeridoRequest(
    string TipoDocumento,
    string Nombre,
    string? Descripcion,
    bool Obligatorio,
    IReadOnlyList<string> TiposMimePermitidos,
    int TamanioMaximoMb,
    Guid? UsuarioModificacionId = null);
