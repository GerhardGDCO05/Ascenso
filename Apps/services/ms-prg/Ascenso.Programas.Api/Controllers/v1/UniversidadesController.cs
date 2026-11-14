using System.Security.Claims;
using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;
using Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;
using Ascenso.Programas.Application.Universidades.Comandos.MarcarConvenioVencido;
using Ascenso.Programas.Application.Universidades.Comandos.RegistrarNuevoConvenio;
using Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;
using Ascenso.Programas.Application.Universidades.Consultas.ConsultarHistorialCostosCarrera;
using Ascenso.Programas.Application.Universidades.Consultas.ConsultarUniversidades;
using Ascenso.Programas.Application.Universidades.Consultas.ObtenerUniversidadPorId;
using Ascenso.Programas.Application.Universidades.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Controllers.v1;

[ApiController]
[Route("api/v1/universidades")]
public sealed class UniversidadesController : ControllerBase
{
    private static readonly Guid _usuarioSistemaPorDefecto = Guid.Parse("01966000-0000-7000-8000-000000000000");

    private readonly RegistrarUniversidadConvenioCommandHandler _registrarUniversidadHandler;
    private readonly RegistrarNuevoConvenioCommandHandler _registrarConvenioHandler;
    private readonly MarcarConvenioVencidoCommandHandler _marcarVencidoHandler;
    private readonly AgregarCarreraElegibleCommandHandler _agregarCarreraHandler;
    private readonly ActualizarCostoMatriculaCommandHandler _actualizarCostoHandler;
    private readonly ConsultarUniversidadesQueryHandler _consultarUniversidadesHandler;
    private readonly ObtenerUniversidadPorIdQueryHandler _obtenerPorIdHandler;
    private readonly ConsultarHistorialCostosCarreraQueryHandler _consultarCostosHandler;

    public UniversidadesController(
        RegistrarUniversidadConvenioCommandHandler registrarUniversidadHandler,
        RegistrarNuevoConvenioCommandHandler registrarConvenioHandler,
        MarcarConvenioVencidoCommandHandler marcarVencidoHandler,
        AgregarCarreraElegibleCommandHandler agregarCarreraHandler,
        ActualizarCostoMatriculaCommandHandler actualizarCostoHandler,
        ConsultarUniversidadesQueryHandler consultarUniversidadesHandler,
        ObtenerUniversidadPorIdQueryHandler obtenerPorIdHandler,
        ConsultarHistorialCostosCarreraQueryHandler consultarCostosHandler)
    {
        _registrarUniversidadHandler = registrarUniversidadHandler ?? throw new ArgumentNullException(nameof(registrarUniversidadHandler));
        _registrarConvenioHandler = registrarConvenioHandler ?? throw new ArgumentNullException(nameof(registrarConvenioHandler));
        _marcarVencidoHandler = marcarVencidoHandler ?? throw new ArgumentNullException(nameof(marcarVencidoHandler));
        _agregarCarreraHandler = agregarCarreraHandler ?? throw new ArgumentNullException(nameof(agregarCarreraHandler));
        _actualizarCostoHandler = actualizarCostoHandler ?? throw new ArgumentNullException(nameof(actualizarCostoHandler));
        _consultarUniversidadesHandler = consultarUniversidadesHandler ?? throw new ArgumentNullException(nameof(consultarUniversidadesHandler));
        _obtenerPorIdHandler = obtenerPorIdHandler ?? throw new ArgumentNullException(nameof(obtenerPorIdHandler));
        _consultarCostosHandler = consultarCostosHandler ?? throw new ArgumentNullException(nameof(consultarCostosHandler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UniversidadDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UniversidadDetalleDto>> Registrar(
        [FromBody] RegistrarUniversidadConvenioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new RegistrarUniversidadConvenioCommand(
            request.CodigoInstitucional,
            request.Nombre,
            request.Direccion,
            request.ContactoEnlace,
            request.CuentaBancariaInstitucional,
            request.UsuarioEnlaceId,
            request.NumeroConvenio,
            request.TerminosResumen,
            request.FechaInicioConvenio,
            request.FechaFinConvenio,
            request.UrlDocumentoPdfFirebase,
            request.PermiteIntercambioAutomaticoDatos,
            ResolverActorId(request.UsuarioCreacionId));

        var resultado = await _registrarUniversidadHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { universidadId = resultado.Id }, resultado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginadoResponse<UniversidadResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginadoResponse<UniversidadResumenDto>>> Consultar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarUniversidadesQuery(pagina, tamanoPagina, estado, busqueda);
        var resultado = await _consultarUniversidadesHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{universidadId:guid}")]
    [ProducesResponseType(typeof(UniversidadDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UniversidadDetalleDto>> ObtenerPorId(
        Guid universidadId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorIdHandler.HandleAsync(
            new ObtenerUniversidadPorIdQuery(universidadId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPost("{universidadId:guid}/convenios")]
    [ProducesResponseType(typeof(ConvenioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConvenioDto>> RegistrarNuevoConvenio(
        Guid universidadId,
        [FromBody] RegistrarNuevoConvenioRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new RegistrarNuevoConvenioCommand(
            universidadId,
            request.NumeroConvenio,
            request.TerminosResumen,
            request.FechaInicio,
            request.FechaFin,
            request.UrlDocumentoPdfFirebase,
            request.PermiteIntercambioAutomaticoDatos,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _registrarConvenioHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { universidadId }, resultado);
    }

    [HttpPost("{universidadId:guid}/convenios/{convenioId:guid}/vencer")]
    [ProducesResponseType(typeof(ConvenioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConvenioDto>> MarcarConvenioVencido(
        Guid universidadId,
        Guid convenioId,
        [FromBody] AccionUniversidadRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var comando = new MarcarConvenioVencidoCommand(
            universidadId,
            convenioId,
            ResolverActorId(request?.UsuarioModificacionId));

        var resultado = await _marcarVencidoHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{universidadId:guid}/carreras")]
    [ProducesResponseType(typeof(CarreraElegibleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CarreraElegibleDto>> AgregarCarrera(
        Guid universidadId,
        [FromBody] AgregarCarreraElegibleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new AgregarCarreraElegibleCommand(
            universidadId,
            request.CodigoCarrera,
            request.Nombre,
            request.DuracionSemestres,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _agregarCarreraHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(
            nameof(ConsultarHistorialCostos),
            new { universidadId, carreraId = resultado.Id },
            resultado);
    }

    [HttpPost("{universidadId:guid}/carreras/{carreraId:guid}/costos-matricula")]
    [ProducesResponseType(typeof(CostoMatriculaReferenciaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CostoMatriculaReferenciaDto>> ActualizarCostoMatricula(
        Guid universidadId,
        Guid carreraId,
        [FromBody] ActualizarCostoMatriculaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ActualizarCostoMatriculaCommand(
            universidadId,
            carreraId,
            request.PeriodoAcademicoId,
            request.MontoReferenciaMatricula,
            request.Moneda,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _actualizarCostoHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(
            nameof(ConsultarHistorialCostos),
            new { universidadId, carreraId },
            resultado);
    }

    [HttpGet("{universidadId:guid}/carreras/{carreraId:guid}/costos-matricula")]
    [ProducesResponseType(typeof(IReadOnlyList<CostoMatriculaReferenciaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CostoMatriculaReferenciaDto>>> ConsultarHistorialCostos(
        Guid universidadId,
        Guid carreraId,
        [FromQuery] string? periodoAcademicoId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarHistorialCostosCarreraQuery(universidadId, carreraId, periodoAcademicoId);
        var resultado = await _consultarCostosHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    private Guid ResolverActorId(Guid? actorOpcional)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var claimId) && claimId != Guid.Empty)
        {
            return claimId;
        }

        if (Request.Headers.TryGetValue("X-Usuario-Id", out var headerValor)
            && Guid.TryParse(headerValor.FirstOrDefault(), out var headerId)
            && headerId != Guid.Empty)
        {
            return headerId;
        }

        if (actorOpcional.HasValue && actorOpcional.Value != Guid.Empty)
        {
            return actorOpcional.Value;
        }

        return _usuarioSistemaPorDefecto;
    }

    public sealed record class RegistrarUniversidadConvenioRequest(
        string CodigoInstitucional,
        string Nombre,
        string Direccion,
        ContactoEnlaceDto ContactoEnlace,
        CuentaBancariaInstitucionalDto? CuentaBancariaInstitucional,
        Guid? UsuarioEnlaceId,
        string NumeroConvenio,
        string TerminosResumen,
        DateOnly FechaInicioConvenio,
        DateOnly FechaFinConvenio,
        string UrlDocumentoPdfFirebase,
        bool PermiteIntercambioAutomaticoDatos = true,
        Guid? UsuarioCreacionId = null);

    public sealed record class RegistrarNuevoConvenioRequest(
        string NumeroConvenio,
        string TerminosResumen,
        DateOnly FechaInicio,
        DateOnly FechaFin,
        string UrlDocumentoPdfFirebase,
        bool PermiteIntercambioAutomaticoDatos = true,
        Guid? UsuarioModificacionId = null);

    public sealed record class AgregarCarreraElegibleRequest(
        string CodigoCarrera,
        string Nombre,
        int DuracionSemestres,
        Guid? UsuarioModificacionId = null);

    public sealed record class ActualizarCostoMatriculaRequest(
        string PeriodoAcademicoId,
        decimal MontoReferenciaMatricula,
        string Moneda,
        Guid? UsuarioModificacionId = null);

    public sealed record class AccionUniversidadRequest(Guid? UsuarioModificacionId = null);
}
