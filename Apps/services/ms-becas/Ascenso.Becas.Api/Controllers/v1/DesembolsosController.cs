using System.Text.Json.Serialization;
using Ascenso.Becas.Api.Contexto;
using Ascenso.Becas.Api.Seguridad;
using Ascenso.Becas.Application.Desembolsos;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ascenso.Becas.Api.Controllers.v1;

/// <summary>Operaciones de desembolsos autorizadas por programa.</summary>
[ApiController]
[Authorize(Roles = PoliticasSeguridad.RolAdministradorFinanciero + "," + PoliticasSeguridad.RolCoordinadorPrograma)]
[Authorize(Policy = PoliticasSeguridad.Requiere2FA)]
[Route("api/v1/becas")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status412PreconditionFailed)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class DesembolsosController : ControllerBase
{
    private readonly ICommandDispatcher _comandos;
    private readonly IQueryDispatcher _consultas;
    private readonly IConsultasBeca _becas;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IAuthorizationService _autorizacion;

    public DesembolsosController(ICommandDispatcher comandos, IQueryDispatcher consultas,
        IConsultasBeca becas, ICalendarioDesembolsoRepository calendarios,
        IAuthorizationService autorizacion)
    {
        _comandos = comandos;
        _consultas = consultas;
        _becas = becas;
        _calendarios = calendarios;
        _autorizacion = autorizacion;
    }

    [HttpPost("{becaId:guid}/calendarios-desembolso")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Programar(Guid becaId,
        [FromBody] ProgramarCalendarioRequest solicitud, CancellationToken cancellationToken)
    {
        await ExigirAlcanceBecaAsync(becaId, cancellationToken);
        var id = await _comandos.EnviarAsync<ProgramarCalendarioDesembolsoCommand, Guid>(
            new(becaId, solicitud.PeriodoAcademicoId, solicitud.Frecuencia,
                solicitud.Destinatario, solicitud.MontoMaximoAprobadoPeriodo,
                solicitud.FechaInicio, solicitud.FechaFin, solicitud.Cuotas), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpGet("{becaId:guid}/calendarios-desembolso")]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarioDesembolsoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CalendarioDesembolsoDto>>> ConsultarCalendarios(
        Guid becaId, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 50,
        CancellationToken cancellationToken = default)
    {
        await ExigirAlcanceBecaAsync(becaId, cancellationToken);
        return Ok(await _consultas.ConsultarAsync<ObtenerCalendarioDesembolsosQuery,
            IReadOnlyList<CalendarioDesembolsoDto>>(
            new(becaId, pagina, tamanoPagina), cancellationToken));
    }

    [HttpPost("{becaId:guid}/datos-pago")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> ActualizarDatosPago(Guid becaId,
        [FromBody] ActualizarDatosPagoRequest solicitud,
        [FromHeader(Name = ContextoOperacionHttp.EncabezadoAutorizacionOtp)] Guid? autorizacionOtpId,
        CancellationToken cancellationToken)
    {
        ExigirAutorizacionOtp(autorizacionOtpId);
        await ExigirAlcanceBecaAsync(becaId, cancellationToken);
        var id = await _comandos.EnviarAsync<ActualizarDatosPagoBecarioCommand, Guid>(
            new(becaId, solicitud.NombreBanco, solicitud.CodigoBanco, solicitud.TipoCuenta,
                solicitud.NumeroCuenta, solicitud.DocumentoTitular, solicitud.CodigoOtp),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpGet("calendarios-desembolso/{calendarioId:guid}/cuotas")]
    [ProducesResponseType(typeof(IReadOnlyList<DesembolsoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DesembolsoDto>>> ConsultarCuotas(
        Guid calendarioId, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 50,
        CancellationToken cancellationToken = default)
    {
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        return Ok(await _consultas.ConsultarAsync<ObtenerDesembolsosQuery,
            IReadOnlyList<DesembolsoDto>>(
            new(calendarioId, pagina, tamanoPagina), cancellationToken));
    }

    [HttpPost("calendarios-desembolso/{calendarioId:guid}/cuotas/{numeroCuota:int}/ejecucion")]
    [ProducesResponseType(typeof(DesembolsoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DesembolsoDto>> RegistrarEjecucion(Guid calendarioId,
        int numeroCuota, [FromBody] RegistrarEjecucionRequest solicitud,
        CancellationToken cancellationToken)
    {
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        var resultado = await _comandos.EnviarAsync<RegistrarEjecucionDesembolsoCommand,
            DesembolsoDto>(new(calendarioId, numeroCuota, solicitud.Monto,
            solicitud.FechaEjecucion, solicitud.Destinatario, solicitud.DatosPagoBecarioId,
            solicitud.CuentaDestinoCifrada, solicitud.MetodoPago, solicitud.ReferenciaBancaria),
            cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("calendarios-desembolso/{calendarioId:guid}/cuotas/{numeroCuota:int}/incidencias")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> RegistrarIncidencia(Guid calendarioId,
        int numeroCuota, [FromBody] RegistrarIncidenciaRequest solicitud,
        CancellationToken cancellationToken)
    {
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        var id = await _comandos.EnviarAsync<RegistrarIncidenciaDesembolsoCommand,
            Guid>(new(calendarioId, numeroCuota, solicitud.Estado,
                solicitud.Motivo, solicitud.ObservacionBanco), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    [HttpPost("calendarios-desembolso/{calendarioId:guid}/cuotas/{numeroCuota:int}/incidencias/{incidenciaId:guid}/resolver")]
    [ProducesResponseType(typeof(DesembolsoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DesembolsoDto>> ResolverIncidencia(Guid calendarioId,
        int numeroCuota, Guid incidenciaId, CancellationToken cancellationToken)
    {
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        return Ok(await _comandos.EnviarAsync<ResolverIncidenciaDesembolsoCommand,
            DesembolsoDto>(new(calendarioId, numeroCuota, incidenciaId), cancellationToken));
    }

    [HttpPost("calendarios-desembolso/{calendarioId:guid}/cuotas/{numeroCuota:int}/reintentar")]
    [ProducesResponseType(typeof(DesembolsoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DesembolsoDto>> Reintentar(Guid calendarioId,
        int numeroCuota, CancellationToken cancellationToken)
    {
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        return Ok(await _comandos.EnviarAsync<ReintentarDesembolsoCommand,
            DesembolsoDto>(new(calendarioId, numeroCuota), cancellationToken));
    }

    [HttpPost("calendarios-desembolso/{calendarioId:guid}/cuotas/{numeroCuota:int}/confirmar")]
    [ProducesResponseType(typeof(DesembolsoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DesembolsoDto>> Confirmar(Guid calendarioId,
        int numeroCuota, [FromBody] ConfirmarDesembolsoRequest solicitud,
        [FromHeader(Name = ContextoOperacionHttp.EncabezadoAutorizacionOtp)] Guid? autorizacionOtpId,
        CancellationToken cancellationToken)
    {
        if (numeroCuota <= 0 || solicitud is null
            || string.IsNullOrWhiteSpace(solicitud.CodigoOtp))
            throw new ValidacionAplicacionException(new Dictionary<string, string[]>
            {
                [nameof(solicitud)] = ["La cuota y el código OTP son obligatorios."]
            });
        ExigirAutorizacionOtp(autorizacionOtpId);
        await ExigirAlcanceCalendarioAsync(calendarioId, cancellationToken);
        // La confirmación no se despacha hasta que 16.10 garantice el
        // movimiento contable idempotente en MS-PRG.
        throw new DependenciaNoDisponibleException("MS-PRG: movimientos-presupuesto");
    }

    private static void ExigirAutorizacionOtp(Guid? autorizacionOtpId)
    {
        if (!autorizacionOtpId.HasValue || autorizacionOtpId.Value == Guid.Empty)
            throw new ValidacionAplicacionException(new Dictionary<string, string[]>
            {
                [ContextoOperacionHttp.EncabezadoAutorizacionOtp] =
                    ["Se requiere un identificador de autorización OTP válido."]
            });
    }

    private async Task ExigirAlcanceCalendarioAsync(Guid calendarioId, CancellationToken token)
    {
        var calendario = await _calendarios.ObtenerPorIdAsync(calendarioId, token)
            ?? throw new RecursoNoEncontradoException("calendario de desembolsos", calendarioId);
        await ExigirAlcanceBecaAsync(calendario.BecaId, token);
    }

    private async Task ExigirAlcanceBecaAsync(Guid becaId, CancellationToken token)
    {
        var recurso = await _becas.ObtenerRecursoBecaAsync(becaId, token)
            ?? throw new RecursoNoEncontradoException("beca", becaId);
        var resultado = await _autorizacion.AuthorizeAsync(User,
            new RecursoAutorizable(ProgramaId: recurso.ProgramaBecaId),
            PoliticasSeguridad.AlcancePrograma);
        if (!resultado.Succeeded)
            throw new OperacionNoPermitidaException("Sin alcance sobre el programa de la beca.");
    }

    public sealed record ProgramarCalendarioRequest(Guid PeriodoAcademicoId,
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<FrecuenciaDesembolso>))]
        FrecuenciaDesembolso Frecuencia,
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<DestinatarioPago>))]
        DestinatarioPago Destinatario,
        decimal MontoMaximoAprobadoPeriodo, DateOnly FechaInicio, DateOnly FechaFin,
        IReadOnlyList<CuotaProgramada> Cuotas);

    public sealed record ActualizarDatosPagoRequest(string NombreBanco, string CodigoBanco,
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<TipoCuentaBancaria>))]
        TipoCuentaBancaria TipoCuenta, string NumeroCuenta, string DocumentoTitular,
        string CodigoOtp);

    public sealed record RegistrarEjecucionRequest(decimal Monto, DateOnly FechaEjecucion,
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<DestinatarioPago>))]
        DestinatarioPago Destinatario, Guid? DatosPagoBecarioId,
        string CuentaDestinoCifrada,
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<MetodoPago>))]
        MetodoPago MetodoPago, string ReferenciaBancaria);

    public sealed record RegistrarIncidenciaRequest(
        [property: JsonConverter(typeof(CodigoEnumJsonConverter<EstadoDesembolso>))]
        EstadoDesembolso Estado,
        string Motivo, string ObservacionBanco);

    public sealed record ConfirmarDesembolsoRequest(string CodigoOtp);
}
