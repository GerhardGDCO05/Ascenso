using System.Security.Claims;
using Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;
using Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.ReactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;
using Ascenso.Programas.Application.Donantes.Consultas.ObtenerDonantePorId;
using Ascenso.Programas.Application.Donantes.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Controllers.v1;

[ApiController]
[Route("api/v1/donantes")]
public sealed class DonantesController : ControllerBase
{
    private static readonly Guid _usuarioSistemaPorDefecto = Guid.Parse("01966000-0000-7000-8000-000000000000");

    private readonly RegistrarDonanteCommandHandler _registrarDonanteHandler;
    private readonly ActualizarContactoDonanteCommandHandler _actualizarContactoHandler;
    private readonly DesactivarDonanteCommandHandler _desactivarDonanteHandler;
    private readonly ReactivarDonanteCommandHandler _reactivarDonanteHandler;
    private readonly RegistrarAporteDonanteCommandHandler _registrarAporteHandler;
    private readonly ConsultarDonantesQueryHandler _consultarDonantesHandler;
    private readonly ObtenerDonantePorIdQueryHandler _obtenerPorIdHandler;
    private readonly ConsultarHistorialAportesQueryHandler _consultarHistorialHandler;

    public DonantesController(
        RegistrarDonanteCommandHandler registrarDonanteHandler,
        ActualizarContactoDonanteCommandHandler actualizarContactoHandler,
        DesactivarDonanteCommandHandler desactivarDonanteHandler,
        ReactivarDonanteCommandHandler reactivarDonanteHandler,
        RegistrarAporteDonanteCommandHandler registrarAporteHandler,
        ConsultarDonantesQueryHandler consultarDonantesHandler,
        ObtenerDonantePorIdQueryHandler obtenerPorIdHandler,
        ConsultarHistorialAportesQueryHandler consultarHistorialHandler)
    {
        _registrarDonanteHandler = registrarDonanteHandler ?? throw new ArgumentNullException(nameof(registrarDonanteHandler));
        _actualizarContactoHandler = actualizarContactoHandler ?? throw new ArgumentNullException(nameof(actualizarContactoHandler));
        _desactivarDonanteHandler = desactivarDonanteHandler ?? throw new ArgumentNullException(nameof(desactivarDonanteHandler));
        _reactivarDonanteHandler = reactivarDonanteHandler ?? throw new ArgumentNullException(nameof(reactivarDonanteHandler));
        _registrarAporteHandler = registrarAporteHandler ?? throw new ArgumentNullException(nameof(registrarAporteHandler));
        _consultarDonantesHandler = consultarDonantesHandler ?? throw new ArgumentNullException(nameof(consultarDonantesHandler));
        _obtenerPorIdHandler = obtenerPorIdHandler ?? throw new ArgumentNullException(nameof(obtenerPorIdHandler));
        _consultarHistorialHandler = consultarHistorialHandler ?? throw new ArgumentNullException(nameof(consultarHistorialHandler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(DonanteDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DonanteDetalleDto>> Registrar(
        [FromBody] RegistrarDonanteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new RegistrarDonanteCommand(
            request.TipoDonante,
            request.RazonSocialNombre,
            request.IdentificacionFiscal,
            request.Email,
            request.Telefono,
            request.DireccionFiscal,
            request.ContactoPrincipal,
            ResolverActorId(request.UsuarioCreacionId));

        var resultado = await _registrarDonanteHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { donanteId = resultado.Id }, resultado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginadoResponse<DonanteResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginadoResponse<DonanteResumenDto>>> Consultar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? tipoDonante = null,
        [FromQuery] string? estadoDonante = null,
        [FromQuery] string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarDonantesQuery(pagina, tamanoPagina, tipoDonante, estadoDonante, busqueda);
        var resultado = await _consultarDonantesHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{donanteId:guid}")]
    [ProducesResponseType(typeof(DonanteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonanteDetalleDto>> ObtenerPorId(
        Guid donanteId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorIdHandler.HandleAsync(
            new ObtenerDonantePorIdQuery(donanteId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpPut("{donanteId:guid}/contacto")]
    [ProducesResponseType(typeof(DonanteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonanteDetalleDto>> ActualizarContacto(
        Guid donanteId,
        [FromBody] ActualizarContactoDonanteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ActualizarContactoDonanteCommand(
            donanteId,
            request.Email,
            request.Telefono,
            request.DireccionFiscal,
            request.ContactoPrincipal,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _actualizarContactoHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{donanteId:guid}/desactivar")]
    [ProducesResponseType(typeof(DonanteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DonanteDetalleDto>> Desactivar(
        Guid donanteId,
        CancellationToken cancellationToken = default)
    {
        var comando = new DesactivarDonanteCommand(donanteId, ResolverActorId(null));
        var resultado = await _desactivarDonanteHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{donanteId:guid}/reactivar")]
    [ProducesResponseType(typeof(DonanteDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DonanteDetalleDto>> Reactivar(
        Guid donanteId,
        CancellationToken cancellationToken = default)
    {
        var comando = new ReactivarDonanteCommand(donanteId, ResolverActorId(null));
        var resultado = await _reactivarDonanteHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{donanteId:guid}/aportes")]
    [ProducesResponseType(typeof(AporteDonanteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AporteDonanteDto>> RegistrarAporte(
        Guid donanteId,
        [FromBody] RegistrarAporteDonanteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new RegistrarAporteDonanteCommand(
            donanteId,
            request.MontoAporte,
            request.Moneda,
            request.PeriodoAcademicoId,
            request.FechaAporte,
            request.BancoEmisor,
            request.NumeroReferenciaBancaria,
            request.UrlComprobanteFirebase,
            request.Observaciones,
            ResolverActorId(request.RegistradoPorUsuarioId));

        var resultado = await _registrarAporteHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(
            nameof(ConsultarHistorialAportes),
            new { donanteId },
            resultado);
    }

    [HttpGet("{donanteId:guid}/aportes")]
    [ProducesResponseType(typeof(IReadOnlyList<AporteDonanteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AporteDonanteDto>>> ConsultarHistorialAportes(
        Guid donanteId,
        [FromQuery] string? periodoAcademicoId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarHistorialAportesQuery(donanteId, periodoAcademicoId);
        var resultado = await _consultarHistorialHandler.HandleAsync(query, cancellationToken);
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

    public sealed record class RegistrarDonanteRequest(
        string TipoDonante,
        string RazonSocialNombre,
        string IdentificacionFiscal,
        string Email,
        string Telefono,
        string DireccionFiscal,
        ContactoDonanteDto ContactoPrincipal,
        Guid? UsuarioCreacionId = null);

    public sealed record class ActualizarContactoDonanteRequest(
        string Email,
        string Telefono,
        string DireccionFiscal,
        ContactoDonanteDto ContactoPrincipal,
        Guid? UsuarioModificacionId = null);

    public sealed record class RegistrarAporteDonanteRequest(
        decimal MontoAporte,
        string Moneda,
        string PeriodoAcademicoId,
        DateTimeOffset FechaAporte,
        string BancoEmisor,
        string NumeroReferenciaBancaria,
        string UrlComprobanteFirebase,
        string? Observaciones = null,
        Guid? RegistradoPorUsuarioId = null);
}

