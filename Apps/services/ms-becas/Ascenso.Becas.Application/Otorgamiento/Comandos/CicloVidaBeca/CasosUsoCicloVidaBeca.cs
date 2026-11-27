using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.Application.Otorgamiento.Comandos.CicloVidaBeca;

/// <summary>
/// Coordina beca y calendario bajo el mismo bloqueo de beca y la misma transacción local.
/// Los movimientos contables de MS-PRG se incorporan en la tarea 17.6.
/// </summary>
public sealed class CasosUsoCicloVidaBeca :
    ICommandHandler<SuspenderBecaCommand, Guid>,
    ICommandHandler<ReanudarBecaCommand, Guid>,
    ICommandHandler<RevocarBecaDefinitivamenteCommand>
{
    private readonly IBecaRepository _becas;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOtpClient _otp;
    private readonly IOutboxBeca _outbox;
    private readonly IProgramadorExpiracionOfertas _programador;
    private readonly ILogger<CasosUsoCicloVidaBeca> _logger;
    private readonly IMovimientoPresupuestoPrograma _movimientos;
    private readonly IIntencionAceptacionOfertaStore _aceptaciones;

    public CasosUsoCicloVidaBeca(
        IBecaRepository becas,
        ICalendarioDesembolsoRepository calendarios,
        IUnitOfWork unidadDeTrabajo,
        IContextoOperacionActual contextoActual,
        IClock reloj,
        IOtpClient otp,
        IOutboxBeca outbox,
        IProgramadorExpiracionOfertas programador,
        ILogger<CasosUsoCicloVidaBeca> logger,
        IMovimientoPresupuestoPrograma movimientos,
        IIntencionAceptacionOfertaStore aceptaciones)
    {
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _calendarios = calendarios ?? throw new ArgumentNullException(nameof(calendarios));
        _unidadDeTrabajo = unidadDeTrabajo ?? throw new ArgumentNullException(nameof(unidadDeTrabajo));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _otp = otp ?? throw new ArgumentNullException(nameof(otp));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _programador = programador ?? throw new ArgumentNullException(nameof(programador));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
        _aceptaciones = aceptaciones ?? throw new ArgumentNullException(nameof(aceptaciones));
    }

    public async Task<Guid> HandleAsync(
        SuspenderBecaCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Tipo != TipoSuspensionBeca.Manual)
            throw new OperacionNoPermitidaException(
                "La suspensión académica o por pausa exige su caso de uso propietario.");

        Guid suspensionId = Guid.Empty;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await ObtenerBecaBloqueadaAsync(command.BecaId, token);
            if (beca.Estado != EstadoBeca.Activa)
                throw new ConflictoOperacionException("La beca debe estar activa para suspenderse.");
            await _movimientos.AsegurarDisponibleAsync(token);
            var version = beca.Version;
            var monto = beca.MontoComprometidoPeriodoActual;
            var calendario = await ObtenerCalendarioActualAsync(beca, token);
            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj, command.Justificacion);
            var suspension = beca.Suspender(command.Tipo, command.Motivo,
                command.RequisitosParaReanudar, command.FechaInicio,
                command.FechaRevision, command.EsIndefinida, contexto);
            calendario?.SuspenderCuotasProgramadas(beca.Id, beca.Estado,
                suspension.Id, null, contexto);

            await _movimientos.RegistrarAsync(CrearMovimiento(beca,
                TipoMovimientoPresupuestoPrograma.Suspension, monto, version), token);

            await EncolarEventosAsync(beca, calendario, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            suspensionId = suspension.Id;
        }, cancellationToken);
        return suspensionId;
    }

    public async Task<Guid> HandleAsync(
        ReanudarBecaCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        Guid suspensionId = Guid.Empty;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await ObtenerBecaBloqueadaAsync(command.BecaId, token);
            if (beca.Estado != EstadoBeca.Suspendida)
                throw new ConflictoOperacionException("La beca debe estar suspendida para reanudarse.");
            await _movimientos.AsegurarDisponibleAsync(token);
            var version = beca.Version;
            var monto = beca.MontoComprometidoPeriodoActual;
            var calendario = await ObtenerCalendarioActualAsync(beca, token);
            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj, command.Justificacion);
            var suspension = beca.Reactivar(contexto);
            calendario?.ReanudarCuotasSuspendidas(beca.Id, beca.Estado,
                suspension.Id, contexto);

            await _movimientos.RegistrarAsync(CrearMovimiento(beca,
                TipoMovimientoPresupuestoPrograma.Reanudacion, monto, version), token);

            await EncolarEventosAsync(beca, calendario, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            suspensionId = suspension.Id;
        }, cancellationToken);
        return suspensionId;
    }

    public async Task HandleAsync(
        RevocarBecaDefinitivamenteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.CodigoOtp))
            throw new OperacionNoPermitidaException("Falta el código OTP de revocación.");
        var autorizacionId = _contextoActual.AutorizacionOtpId is { } id && id != Guid.Empty
            ? id
            : throw new OperacionNoPermitidaException("Falta la autorización OTP de revocación.");

        Guid? ofertaPendienteId = null;
        string? referenciaJob = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await ObtenerBecaBloqueadaAsync(command.BecaId, token);
            if (beca.Estado is not (EstadoBeca.Otorgada or EstadoBeca.Activa or EstadoBeca.Suspendida))
                throw new ConflictoOperacionException("La beca no admite revocación definitiva en su estado actual.");
            if (beca.Oferta?.Estado == EstadoOferta.PendienteRespuesta
                && await _aceptaciones.ObtenerAsync(beca.Oferta.Id, token) is not null)
                throw new ConflictoOperacionException(
                    "La oferta tiene una aceptación pendiente de coordinación.");
            if (string.IsNullOrWhiteSpace(command.Justificacion)
                || command.Justificacion.Trim().Length < 10)
                throw new SolicitudInvalidaException("La revocación requiere una justificación suficiente.");

            await _movimientos.AsegurarDisponibleAsync(token);
            var version = beca.Version;
            var montoPendiente = beca.MontoComprometidoPeriodoActual;

            var calendario = await ObtenerCalendarioActualAsync(beca, token);
            var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                command.Justificacion, autorizacionOtpId: autorizacionId);
            var huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{beca.Id:N}|{command.Justificacion.Trim()}")));
            var resultadoOtp = await _otp.ConsumirAsync(autorizacionId,
                contexto.UsuarioAutorId, TipoOperacionOTP.RevocarBeca,
                "BECA", beca.Id, huella, command.CodigoOtp, token);
            if (!resultadoOtp.Consumido)
                throw new OperacionNoPermitidaException(resultadoOtp.CodigoError ?? "OTP_RECHAZADO");

            if (beca.Oferta?.Estado == EstadoOferta.PendienteRespuesta)
            {
                ofertaPendienteId = beca.Oferta.Id;
                referenciaJob = beca.Oferta.ReferenciaJobExpiracionHangfireId;
            }
            beca.Revocar(contexto);
            calendario?.CancelarCuotasFuturasPorRevocacion(beca.Id, beca.Estado, contexto);

            await _movimientos.RegistrarAsync(CrearMovimiento(beca,
                TipoMovimientoPresupuestoPrograma.Revocacion,
                montoPendiente, version), token);

            await EncolarEventosAsync(beca, calendario, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
        }, cancellationToken);

        if (ofertaPendienteId.HasValue)
            await CanceladorExpiracionOferta.CancelarAsync(_programador, _logger,
                ofertaPendienteId.Value, referenciaJob);
    }

    private async Task<Beca> ObtenerBecaBloqueadaAsync(Guid becaId, CancellationToken token)
        => await _becas.ObtenerBecaParaActualizarAsync(becaId, token)
            ?? throw new RecursoNoEncontradoException("beca", becaId);

    private static MovimientoPresupuestoPrograma CrearMovimiento(Beca beca,
        TipoMovimientoPresupuestoPrograma tipo, decimal monto, int version)
        => new(beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            beca.Id, tipo, monto,
            $"{tipo.ToString().ToLowerInvariant()}-{beca.Id:N}-{version}");

    private Task<CalendarioDesembolso?> ObtenerCalendarioActualAsync(Beca beca, CancellationToken token)
        => _calendarios.ObtenerPorBecaYPeriodoAsync(beca.Id, beca.PeriodoVigenteActualId, token);

    private async Task EncolarEventosAsync(
        Beca beca, CalendarioDesembolso? calendario, CancellationToken token)
    {
        foreach (var evento in beca.EventosDominio)
            await _outbox.EncolarAsync(evento, beca.Version, token);
        if (calendario is not null)
            foreach (var evento in calendario.EventosDominio)
                await _outbox.EncolarAsync(evento, calendario.Version, token);
    }
}
