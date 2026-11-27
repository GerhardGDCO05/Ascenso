using System.Security.Cryptography;
using System.Text;
using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Entidades;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Desembolsos;

public sealed class CasosUsoDesembolso :
    ICommandHandler<ProgramarCalendarioDesembolsoCommand, Guid>,
    ICommandHandler<ActualizarDatosPagoBecarioCommand, Guid>,
    ICommandHandler<RegistrarEjecucionDesembolsoCommand, DesembolsoDto>,
    ICommandHandler<ConfirmarDesembolsoCommand, DesembolsoDto>,
    ICommandHandler<RegistrarIncidenciaDesembolsoCommand, Guid>,
    ICommandHandler<ResolverIncidenciaDesembolsoCommand, DesembolsoDto>,
    ICommandHandler<ReintentarDesembolsoCommand, DesembolsoDto>,
    IQueryHandler<ObtenerCalendarioDesembolsosQuery, IReadOnlyList<CalendarioDesembolsoDto>>,
    IQueryHandler<ObtenerDesembolsosQuery, IReadOnlyList<DesembolsoDto>>
{
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IDatosPagoBecarioRepository _datosPago;
    private readonly IBecaRepository _becas;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly ICifradorCuentaBancaria _cifrador;
    private readonly IOtpClient _otp;
    private readonly IOutboxBeca _outbox;
    private readonly IMovimientoPresupuestoPrograma _movimientos;
    private readonly ICuentaInstitucionalPrograma _cuentaInstitucional;

    public CasosUsoDesembolso(
        ICalendarioDesembolsoRepository calendarios,
        IDatosPagoBecarioRepository datosPago,
        IBecaRepository becas,
        IUnitOfWork unidadDeTrabajo,
        IContextoOperacionActual contextoActual,
        IClock reloj,
        ICifradorCuentaBancaria cifrador,
        IOtpClient otp,
        IOutboxBeca outbox, IMovimientoPresupuestoPrograma movimientos,
        ICuentaInstitucionalPrograma cuentaInstitucional)
    {
        _calendarios = calendarios;
        _datosPago = datosPago;
        _becas = becas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contextoActual = contextoActual;
        _reloj = reloj;
        _cifrador = cifrador;
        _otp = otp;
        _outbox = outbox;
        _movimientos = movimientos ?? throw new ArgumentNullException(nameof(movimientos));
        _cuentaInstitucional = cuentaInstitucional ?? throw new ArgumentNullException(nameof(cuentaInstitucional));
    }

    public async Task<Guid> HandleAsync(ProgramarCalendarioDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        var beca = await ObtenerBecaAsync(command.BecaId, cancellationToken);
        if (beca.Estado != EstadoBeca.Activa || beca.PeriodoVigenteActualId != command.PeriodoAcademicoId)
            throw new ConflictoOperacionException("La beca debe estar activa en el periodo solicitado.");
        if (beca.Oferta is null || command.MontoMaximoAprobadoPeriodo > beca.Oferta.MontoOfertaPorPeriodo)
            throw new ArgumentException("EXCEDE_OFERTA: el techo supera el monto de la oferta.");
        if (await _calendarios.ObtenerPorBecaYPeriodoAsync(command.BecaId,
            command.PeriodoAcademicoId, cancellationToken) is not null)
            throw new ConflictoOperacionException("Ya existe un calendario para esta beca y periodo.");

        var calendario = CalendarioDesembolso.Programar(command.BecaId, command.PeriodoAcademicoId,
            command.Frecuencia, command.Destinatario, command.MontoMaximoAprobadoPeriodo,
            command.FechaInicio, command.FechaFin, command.Cuotas, Contexto());
        await _calendarios.AgregarAsync(calendario, cancellationToken);
        var contexto = Contexto();
        foreach (var cuota in calendario.Desembolsos)
            await _outbox.EncolarAsync(new DesembolsoProgramadoEvento(
                beca.Id, calendario.Id, cuota.Id, contexto.CorrelacionId, contexto.Fecha),
                cancellationToken: cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return calendario.Id;
    }

    public async Task<Guid> HandleAsync(ActualizarDatosPagoBecarioCommand command,
        CancellationToken cancellationToken = default)
    {
        var autorizacionId = ExigirAutorizacionOtp();
        Guid nuevoId = Guid.Empty;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var beca = await _becas.ObtenerBecaParaActualizarAsync(command.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", command.BecaId);
            await ConsumirOtpAsync(autorizacionId, TipoOperacionOTP.ModificarDatosPago, "BECA", command.BecaId,
                $"{command.NombreBanco}|{command.CodigoBanco}|{command.TipoCuenta.ACodigo()}|{command.NumeroCuenta}|{command.DocumentoTitular}",
                command.CodigoOtp, token);
            var anterior = await _datosPago.ObtenerActivoPorBecaAsync(command.BecaId, token);
            anterior?.Reemplazar(Contexto(autorizacionId));
            if (anterior is not null) await _unidadDeTrabajo.GuardarCambiosAsync(token);

            var nuevo = DatosPagoBecario.Crear(beca.PostulanteEstudianteId, command.BecaId,
                command.NombreBanco, command.CodigoBanco, command.TipoCuenta, command.NumeroCuenta,
                command.DocumentoTitular, autorizacionId, _cifrador, Contexto(autorizacionId));
            await _datosPago.AgregarAsync(nuevo, token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            nuevoId = nuevo.Id;
        }, cancellationToken);
        return nuevoId;
    }

    public async Task<DesembolsoDto> HandleAsync(RegistrarEjecucionDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        DesembolsoDto? resultado = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var calendario = await ObtenerCalendarioAsync(command.CalendarioId, token);
            var beca = await _becas.ObtenerBecaParaActualizarAsync(calendario.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", calendario.BecaId);
            beca.ExigirHabilitacionDesembolso();
            var cuentaCifrada = command.CuentaDestinoCifrada;
            if (command.Destinatario == DestinatarioPago.Becario)
            {
                var cuenta = await _datosPago.ObtenerActivoPorBecaAsync(beca.Id, token);
                if (cuenta is null || cuenta.Id != command.DatosPagoBecarioId)
                    throw new ConflictoOperacionException("Los datos de pago no pertenecen a la cuenta activa de la beca.");
                cuentaCifrada = cuenta.NumeroCuentaCifradoAesGcm;
            }
            else
            {
                cuentaCifrada = await _cuentaInstitucional.ObtenerSnapshotCifradoAsync(
                    beca.ProgramaBecaId, beca.PeriodoVigenteActualId, token);
                if (string.IsNullOrWhiteSpace(cuentaCifrada))
                    throw new ConfiguracionProgramaNoDisponibleException(
                        beca.ProgramaBecaId,
                        "No está confirmada la cuenta institucional de destino.");
            }
            calendario.RegistrarEjecucion(command.NumeroCuota, command.Monto, command.FechaEjecucion,
                command.Destinatario, command.DatosPagoBecarioId, cuentaCifrada,
                command.MetodoPago, command.ReferenciaBancaria, Contexto());
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            resultado = Mapear(calendario.ObtenerCuotaVigente(command.NumeroCuota));
        }, cancellationToken);
        return resultado!;
    }

    public async Task<DesembolsoDto> HandleAsync(ConfirmarDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        var autorizacionId = ExigirAutorizacionOtp();
        DesembolsoDto? resultado = null;
        await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async token =>
        {
            var calendario = await ObtenerCalendarioAsync(command.CalendarioId, token);
            var beca = await _becas.ObtenerBecaParaActualizarAsync(calendario.BecaId, token)
                ?? throw new RecursoNoEncontradoException("beca", calendario.BecaId);
            beca.ExigirHabilitacionDesembolso();
            var cuota = calendario.ObtenerCuotaVigente(command.NumeroCuota);
            await _movimientos.AsegurarDisponibleAsync(token);
            await ConsumirOtpAsync(autorizacionId, TipoOperacionOTP.ConfirmarDesembolso, "DESEMBOLSO", cuota.Id,
                FormattableString.Invariant($"{cuota.Id:N}|{cuota.MontoEjecutado}|{cuota.ReferenciaBancaria}"),
                command.CodigoOtp, token);
            await _movimientos.RegistrarAsync(new MovimientoPresupuestoPrograma(
                beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
                cuota.Id, TipoMovimientoPresupuestoPrograma.DesembolsoConfirmado,
                cuota.MontoEjecutado ?? throw new InvalidOperationException("Falta el monto ejecutado."),
                $"desembolso-confirmado-{cuota.Id:N}"), token);
            calendario.Confirmar(command.NumeroCuota, autorizacionId, Contexto(autorizacionId));
            beca.RegistrarDesembolsoConfirmado(
                cuota.MontoEjecutado ?? throw new InvalidOperationException("Falta el monto ejecutado."),
                Contexto(autorizacionId));
            var contexto = Contexto(autorizacionId);
            await _outbox.EncolarAsync(new DesembolsoConfirmadoConOtpEvento(
                beca.Id, calendario.Id, cuota.Id, contexto.CorrelacionId, contexto.Fecha),
                cancellationToken: token);
            await _unidadDeTrabajo.GuardarCambiosAsync(token);
            resultado = Mapear(cuota);
        }, cancellationToken);
        return resultado!;
    }

    public async Task<Guid> HandleAsync(RegistrarIncidenciaDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        var calendario = await ObtenerCalendarioAsync(command.CalendarioId, cancellationToken);
        var incidencia = calendario.ReportarIncidencia(command.NumeroCuota, command.Estado,
            command.Motivo, command.ObservacionBanco, Contexto());
        var cuota = calendario.ObtenerCuotaVigente(command.NumeroCuota);
        var contexto = Contexto();
        if (command.Estado == EstadoDesembolso.Fallido)
            await _outbox.EncolarAsync(new DesembolsoFallidoEvento(calendario.BecaId,
                calendario.Id, cuota.Id, contexto.CorrelacionId, contexto.Fecha),
                cancellationToken: cancellationToken);
        else
            await _outbox.EncolarAsync(new DesembolsoDevueltoEvento(calendario.BecaId,
                calendario.Id, cuota.Id, contexto.CorrelacionId, contexto.Fecha),
                cancellationToken: cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return incidencia.Id;
    }

    public async Task<DesembolsoDto> HandleAsync(ResolverIncidenciaDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        var calendario = await ObtenerCalendarioAsync(command.CalendarioId, cancellationToken);
        calendario.ResolverIncidencia(command.NumeroCuota, command.IncidenciaId, Contexto());
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Mapear(calendario.ObtenerCuotaVigente(command.NumeroCuota));
    }

    public async Task<DesembolsoDto> HandleAsync(ReintentarDesembolsoCommand command,
        CancellationToken cancellationToken = default)
    {
        var calendario = await ObtenerCalendarioAsync(command.CalendarioId, cancellationToken);
        var nuevo = calendario.Reintentar(command.NumeroCuota, Contexto());
        var contexto = Contexto();
        await _outbox.EncolarAsync(new DesembolsoReintentadoEvento(calendario.BecaId,
            calendario.Id, nuevo.Id, contexto.CorrelacionId, contexto.Fecha),
            cancellationToken: cancellationToken);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancellationToken);
        return Mapear(nuevo);
    }

    public async Task<IReadOnlyList<CalendarioDesembolsoDto>> HandleAsync(
        ObtenerCalendarioDesembolsosQuery query, CancellationToken cancellationToken = default)
        => (await _calendarios.ObtenerPorBecaAsync(query.BecaId, query.Pagina,
            query.TamanoPagina, cancellationToken))
            .Select(item => new CalendarioDesembolsoDto(item.Id, item.BecaId,
                item.PeriodoAcademicoId, item.Frecuencia.ACodigo(),
                item.DestinatarioPredeterminado.ACodigo(), item.MontoMaximoAprobadoPeriodo,
                item.SumaCuotasProgramadas, item.NumeroCuotas, item.EstadoPlan.ACodigo()))
            .ToArray();

    public async Task<IReadOnlyList<DesembolsoDto>> HandleAsync(
        ObtenerDesembolsosQuery query, CancellationToken cancellationToken = default)
        => (await _calendarios.ObtenerCuotasAsync(query.CalendarioId, query.Pagina,
            query.TamanoPagina, cancellationToken)).Select(Mapear).ToArray();

    private async Task<CalendarioDesembolso> ObtenerCalendarioAsync(
        Guid id, CancellationToken cancellationToken)
        => await _calendarios.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException("calendario de desembolsos", id);

    private async Task<Domain.Otorgamiento.Agregados.Beca> ObtenerBecaAsync(
        Guid id, CancellationToken cancellationToken)
        => await _becas.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException("beca", id);

    private Domain.Otorgamiento.ValueObjects.ContextoOperacion Contexto(Guid? otpId = null)
        => ContextoAplicacion.Crear(_contextoActual, _reloj, autorizacionOtpId: otpId);

    private Guid ExigirAutorizacionOtp()
        => _contextoActual.AutorizacionOtpId is { } id && id != Guid.Empty
            ? id
            : throw new OperacionNoPermitidaException("Falta la autorización OTP de la operación.");

    private async Task ConsumirOtpAsync(Guid autorizacionOtpId, TipoOperacionOTP operacion,
        string recursoTipo, Guid recursoId,
        string payload, string codigo, CancellationToken cancellationToken)
    {
        var huella = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        var resultado = await _otp.ConsumirAsync(autorizacionOtpId, _contextoActual.UsuarioId ?? Guid.Empty,
            operacion, recursoTipo, recursoId, huella, codigo, cancellationToken);
        if (!resultado.Consumido)
            throw new OperacionNoPermitidaException(resultado.CodigoError ?? "OTP_RECHAZADO");
    }

    private static DesembolsoDto Mapear(Desembolso item)
        => new(item.Id, item.CalendarioDesembolsoId, item.NumeroCuota,
            item.MontoProgramado, item.MontoEjecutado, item.FechaProgramada,
            item.Estado.ACodigo(), item.EsVigente, item.DesembolsoOrigenReintentoId);
}
