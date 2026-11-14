using System.Security.Claims;
using Ascenso.Programas.Application.Donantes.DTOs;
using Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;
using Ascenso.Programas.Application.Programas.Comandos.CerrarPeriodoPrograma;
using Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;
using Ascenso.Programas.Application.Programas.Comandos.ConfirmarReservaPresupuesto;
using Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;
using Ascenso.Programas.Application.Programas.Comandos.LiberarReservaPresupuesto;
using Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;
using Ascenso.Programas.Application.Programas.Comandos.ReservarPresupuestoPrograma;
using Ascenso.Programas.Application.Programas.Consultas.ConsultarMovimientosPresupuestarios;
using Ascenso.Programas.Application.Programas.Consultas.ConsultarProgramas;
using Ascenso.Programas.Application.Programas.Consultas.ObtenerBalancePrograma;
using Ascenso.Programas.Application.Programas.Consultas.ObtenerProgramaPorId;
using Ascenso.Programas.Application.Programas.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Programas.Api.Controllers.v1;

[ApiController]
[Route("api/v1/programas")]
public sealed class ProgramasController : ControllerBase
{
    private static readonly Guid _usuarioSistemaPorDefecto = Guid.Parse("01966000-0000-7000-8000-000000000000");

    private readonly CrearProgramaBecaCommandHandler _crearProgramaHandler;
    private readonly AsignarPresupuestoDonantesCommandHandler _asignarPresupuestoHandler;
    private readonly ReajustarPresupuestoDonanteCommandHandler _reajustarPresupuestoHandler;
    private readonly ConfigurarCriteriosElegibilidadCommandHandler _configurarElegibilidadHandler;
    private readonly ReservarPresupuestoProgramaCommandHandler _reservarPresupuestoHandler;
    private readonly ConfirmarReservaPresupuestoCommandHandler _confirmarReservaHandler;
    private readonly LiberarReservaPresupuestoCommandHandler _liberarReservaHandler;
    private readonly CerrarPeriodoProgramaCommandHandler _cerrarPeriodoHandler;
    private readonly ConsultarProgramasQueryHandler _consultarProgramasHandler;
    private readonly ObtenerProgramaPorIdQueryHandler _obtenerPorIdHandler;
    private readonly ObtenerBalanceProgramaQueryHandler _obtenerBalanceHandler;
    private readonly ConsultarMovimientosPresupuestariosQueryHandler _consultarMovimientosHandler;

    public ProgramasController(
        CrearProgramaBecaCommandHandler crearProgramaHandler,
        AsignarPresupuestoDonantesCommandHandler asignarPresupuestoHandler,
        ReajustarPresupuestoDonanteCommandHandler reajustarPresupuestoHandler,
        ConfigurarCriteriosElegibilidadCommandHandler configurarElegibilidadHandler,
        ReservarPresupuestoProgramaCommandHandler reservarPresupuestoHandler,
        ConfirmarReservaPresupuestoCommandHandler confirmarReservaHandler,
        LiberarReservaPresupuestoCommandHandler liberarReservaHandler,
        CerrarPeriodoProgramaCommandHandler cerrarPeriodoHandler,
        ConsultarProgramasQueryHandler consultarProgramasHandler,
        ObtenerProgramaPorIdQueryHandler obtenerPorIdHandler,
        ObtenerBalanceProgramaQueryHandler obtenerBalanceHandler,
        ConsultarMovimientosPresupuestariosQueryHandler consultarMovimientosHandler)
    {
        _crearProgramaHandler = crearProgramaHandler ?? throw new ArgumentNullException(nameof(crearProgramaHandler));
        _asignarPresupuestoHandler = asignarPresupuestoHandler ?? throw new ArgumentNullException(nameof(asignarPresupuestoHandler));
        _reajustarPresupuestoHandler = reajustarPresupuestoHandler ?? throw new ArgumentNullException(nameof(reajustarPresupuestoHandler));
        _configurarElegibilidadHandler = configurarElegibilidadHandler ?? throw new ArgumentNullException(nameof(configurarElegibilidadHandler));
        _reservarPresupuestoHandler = reservarPresupuestoHandler ?? throw new ArgumentNullException(nameof(reservarPresupuestoHandler));
        _confirmarReservaHandler = confirmarReservaHandler ?? throw new ArgumentNullException(nameof(confirmarReservaHandler));
        _liberarReservaHandler = liberarReservaHandler ?? throw new ArgumentNullException(nameof(liberarReservaHandler));
        _cerrarPeriodoHandler = cerrarPeriodoHandler ?? throw new ArgumentNullException(nameof(cerrarPeriodoHandler));
        _consultarProgramasHandler = consultarProgramasHandler ?? throw new ArgumentNullException(nameof(consultarProgramasHandler));
        _obtenerPorIdHandler = obtenerPorIdHandler ?? throw new ArgumentNullException(nameof(obtenerPorIdHandler));
        _obtenerBalanceHandler = obtenerBalanceHandler ?? throw new ArgumentNullException(nameof(obtenerBalanceHandler));
        _consultarMovimientosHandler = consultarMovimientosHandler ?? throw new ArgumentNullException(nameof(consultarMovimientosHandler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProgramaBecaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramaBecaDetalleDto>> Crear(
        [FromBody] CrearProgramaBecaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new CrearProgramaBecaCommand(
            request.Codigo,
            request.Nombre,
            request.Descripcion,
            request.PoliticaCobertura,
            request.CupoTotal,
            request.DuracionPeriodos,
            request.PeriodoInicialId,
            request.VigenciaDesde,
            request.VigenciaHasta,
            request.PermiteAcumulacion,
            request.RequiereCompromisoFirmado,
            request.TerminosCompromisoEspecial,
            ResolverActorId(request.UsuarioCreacionId));

        var resultado = await _crearProgramaHandler.HandleAsync(comando, cancellationToken);

        return CreatedAtAction(nameof(ObtenerPorId), new { programaId = resultado.Id }, resultado);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginadoResponse<ProgramaBecaResumenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginadoResponse<ProgramaBecaResumenDto>>> Consultar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? estado = null,
        [FromQuery] string? periodoInicialId = null,
        [FromQuery] string? modalidadCobertura = null,
        [FromQuery] string? busqueda = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarProgramasQuery(
            pagina,
            tamanoPagina,
            estado,
            periodoInicialId,
            modalidadCobertura,
            busqueda);

        var resultado = await _consultarProgramasHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{programaId:guid}")]
    [ProducesResponseType(typeof(ProgramaBecaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramaBecaDetalleDto>> ObtenerPorId(
        Guid programaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerPorIdHandler.HandleAsync(
            new ObtenerProgramaPorIdQuery(programaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{programaId:guid}/balance")]
    [ProducesResponseType(typeof(BalancePresupuestarioProgramaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalancePresupuestarioProgramaResponse>> ObtenerBalance(
        Guid programaId,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _obtenerBalanceHandler.HandleAsync(
            new ObtenerBalanceProgramaQuery(programaId),
            cancellationToken);

        return Ok(resultado);
    }

    [HttpGet("{programaId:guid}/movimientos")]
    [ProducesResponseType(typeof(PaginadoResponse<MovimientoPresupuestarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginadoResponse<MovimientoPresupuestarioResponse>>> ConsultarMovimientos(
        Guid programaId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        [FromQuery] string? tipoMovimiento = null,
        [FromQuery] string? periodoAcademicoId = null,
        [FromQuery] Guid? referenciaExternaId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ConsultarMovimientosPresupuestariosQuery(
            programaId,
            pagina,
            tamanoPagina,
            tipoMovimiento,
            periodoAcademicoId,
            referenciaExternaId);

        var resultado = await _consultarMovimientosHandler.HandleAsync(query, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{programaId:guid}/asignaciones-presupuesto")]
    [ProducesResponseType(typeof(ProgramaBecaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramaBecaDetalleDto>> AsignarPresupuesto(
        Guid programaId,
        [FromBody] AsignarPresupuestoDonantesRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new AsignarPresupuestoDonantesCommand(
            programaId,
            request.Asignaciones,
            ResolverActorId(request.AutorizadoPorUsuarioId));

        var resultado = await _asignarPresupuestoHandler.HandleAsync(comando, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { programaId = resultado.Id }, resultado);
    }

    [HttpPost("{programaId:guid}/asignaciones-presupuesto/{asignacionId:guid}/reajustar")]
    [ProducesResponseType(typeof(ProgramaBecaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramaBecaDetalleDto>> ReajustarPresupuesto(
        Guid programaId,
        Guid asignacionId,
        [FromBody] ReajustarPresupuestoDonanteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ReajustarPresupuestoDonanteCommand(
            programaId,
            asignacionId,
            request.MontoAReducir,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _reajustarPresupuestoHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPut("{programaId:guid}/configuracion-elegibilidad")]
    [ProducesResponseType(typeof(ProgramaBecaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProgramaBecaDetalleDto>> ConfigurarElegibilidad(
        Guid programaId,
        [FromBody] ConfigurarCriteriosElegibilidadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ConfigurarCriteriosElegibilidadCommand(
            programaId,
            request.CriteriosElegibilidad,
            request.CondicionesPermanencia,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _configurarElegibilidadHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{programaId:guid}/reservas")]
    [ProducesResponseType(typeof(ReservaPresupuestariaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReservaPresupuestariaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaPresupuestariaResponse>> ReservarPresupuesto(
        Guid programaId,
        [FromBody] ReservarPresupuestoProgramaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new ReservarPresupuestoProgramaCommand(
            programaId,
            request.ReferenciaExternaId,
            request.PeriodoAcademicoId,
            request.Monto,
            request.Descripcion,
            ResolverActorId(request.UsuarioAutorId));

        var resultado = await _reservarPresupuestoHandler.HandleAsync(comando, cancellationToken);

        return resultado.YaExistia
            ? Ok(resultado)
            : CreatedAtAction(nameof(ObtenerBalance), new { programaId }, resultado);
    }

    [HttpPost("{programaId:guid}/reservas/{referenciaExternaId:guid}/confirmar")]
    [ProducesResponseType(typeof(ReservaPresupuestariaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaPresupuestariaResponse>> ConfirmarReserva(
        Guid programaId,
        Guid referenciaExternaId,
        [FromBody] ConfirmarReservaPresupuestoRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var comando = new ConfirmarReservaPresupuestoCommand(
            programaId,
            referenciaExternaId,
            request?.Descripcion,
            ResolverActorId(request?.UsuarioModificacionId));

        var resultado = await _confirmarReservaHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{programaId:guid}/reservas/{referenciaExternaId:guid}/liberar")]
    [ProducesResponseType(typeof(ReservaPresupuestariaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaPresupuestariaResponse>> LiberarReserva(
        Guid programaId,
        Guid referenciaExternaId,
        [FromBody] LiberarReservaPresupuestoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new LiberarReservaPresupuestoCommand(
            programaId,
            referenciaExternaId,
            request.Motivo,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _liberarReservaHandler.HandleAsync(comando, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{programaId:guid}/cerrar-periodo")]
    [ProducesResponseType(typeof(CierrePeriodoProgramaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CierrePeriodoProgramaResponse>> CerrarPeriodo(
        Guid programaId,
        [FromBody] CerrarPeriodoProgramaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var comando = new CerrarPeriodoProgramaCommand(
            programaId,
            request.PeriodoAcademicoId,
            request.Observaciones,
            request.CerrarProgramaDefinitivamente,
            ResolverActorId(request.UsuarioModificacionId));

        var resultado = await _cerrarPeriodoHandler.HandleAsync(comando, cancellationToken);
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

public sealed record CrearProgramaBecaRequest(
    string Codigo,
    string Nombre,
    string Descripcion,
    PoliticaCoberturaDto PoliticaCobertura,
    int CupoTotal,
    int DuracionPeriodos,
    string PeriodoInicialId,
    DateOnly VigenciaDesde,
    DateOnly VigenciaHasta,
    bool PermiteAcumulacion,
    bool RequiereCompromisoFirmado,
    string? TerminosCompromisoEspecial,
    Guid? UsuarioCreacionId);

public sealed record AsignarPresupuestoDonantesRequest(
    IReadOnlyList<ItemAsignacionDonanteCommand> Asignaciones,
    Guid? AutorizadoPorUsuarioId);

public sealed record ReajustarPresupuestoDonanteRequest(
    decimal MontoAReducir,
    Guid? UsuarioModificacionId);

public sealed record ConfigurarCriteriosElegibilidadRequest(
    CriteriosElegibilidadProgramaDto CriteriosElegibilidad,
    CondicionesPermanenciaDto CondicionesPermanencia,
    Guid? UsuarioModificacionId);

public sealed record ReservarPresupuestoProgramaRequest(
    Guid ReferenciaExternaId,
    string PeriodoAcademicoId,
    decimal Monto,
    string Descripcion,
    Guid? UsuarioAutorId = null);

public sealed record ConfirmarReservaPresupuestoRequest(
    string? Descripcion = null,
    Guid? UsuarioModificacionId = null);

public sealed record LiberarReservaPresupuestoRequest(
    string Motivo,
    Guid? UsuarioModificacionId = null);

public sealed record CerrarPeriodoProgramaRequest(
    string PeriodoAcademicoId,
    string Observaciones,
    bool CerrarProgramaDefinitivamente = false,
    Guid? UsuarioModificacionId = null);
