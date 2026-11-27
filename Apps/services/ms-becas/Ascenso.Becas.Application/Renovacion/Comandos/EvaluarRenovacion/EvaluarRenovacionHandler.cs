using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Dtos;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Agregados;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Datos;
using Ascenso.Becas.Domain.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Ascenso.Becas.Domain.Renovacion.Eventos;

namespace Ascenso.Becas.Application.Renovacion.Comandos.EvaluarRenovacion;

/// <summary>Decide la renovación y crea la sucesora en una transacción local.</summary>
public sealed class EvaluarRenovacionHandler
    : ICommandHandler<EvaluarRenovacionCommand, SolicitudRenovacionDto>
{
    private readonly ISolicitudRenovacionRepository _solicitudes;
    private readonly IBecaRepository _becas;
    private readonly IReporteAcademicoRepository _reportes;
    private readonly ICalendarioDesembolsoRepository _calendarios;
    private readonly IPlanNuevoPeriodoPrograma _planes;
    private readonly IVentanaRenovacionPrograma _ventanas;
    private readonly IProgramasClient _programas;
    private readonly IUnitOfWork _unidad;
    private readonly IContextoOperacionActual _contextoActual;
    private readonly IClock _reloj;
    private readonly IOutboxBeca _outbox;

    public EvaluarRenovacionHandler(ISolicitudRenovacionRepository solicitudes,
        IBecaRepository becas, IReporteAcademicoRepository reportes,
        ICalendarioDesembolsoRepository calendarios,
        IPlanNuevoPeriodoPrograma planes, IVentanaRenovacionPrograma ventanas,
        IProgramasClient programas,
        IUnitOfWork unidad, IContextoOperacionActual contextoActual,
        IClock reloj, IOutboxBeca outbox)
    {
        _solicitudes = solicitudes ?? throw new ArgumentNullException(nameof(solicitudes));
        _becas = becas ?? throw new ArgumentNullException(nameof(becas));
        _reportes = reportes ?? throw new ArgumentNullException(nameof(reportes));
        _calendarios = calendarios ?? throw new ArgumentNullException(nameof(calendarios));
        _planes = planes ?? throw new ArgumentNullException(nameof(planes));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _programas = programas ?? throw new ArgumentNullException(nameof(programas));
        _unidad = unidad ?? throw new ArgumentNullException(nameof(unidad));
        _contextoActual = contextoActual ?? throw new ArgumentNullException(nameof(contextoActual));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<SolicitudRenovacionDto> HandleAsync(
        EvaluarRenovacionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SolicitudRenovacionId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.MotivoDecision))
            throw new SolicitudInvalidaException(
                "La solicitud y el motivo de evaluación son obligatorios.");

        SolicitudRenovacionDto? resultado = null;
        Guid? reservaParaCompensar = null;
        Guid programaReserva = Guid.Empty;
        try
        {
            await _unidad.EjecutarEnTransaccionAsync(async token =>
            {
                var solicitud = await _solicitudes.ObtenerParaActualizarAsync(
                    command.SolicitudRenovacionId, token)
                    ?? throw new RecursoNoEncontradoException(
                        "solicitud de renovación", command.SolicitudRenovacionId);
                if (solicitud.Estado != EstadoRenovacion.SolicitadaEnCurso)
                    throw new ConflictoOperacionException(
                        "La solicitud de renovación ya tiene una decisión.");
                var beca = await _becas.ObtenerBecaParaActualizarAsync(
                    solicitud.BecaId, token)
                    ?? throw new RecursoNoEncontradoException("beca", solicitud.BecaId);
                if (beca.Estado != EstadoBeca.Activa || !beca.TieneRenovacionEnCurso
                    || beca.PeriodoVigenteActualId != solicitud.PeriodoOrigenId)
                    throw new ConflictoOperacionException(
                        "La beca ya no admite esta decisión de renovación.");

                var contexto = ContextoAplicacion.Crear(_contextoActual, _reloj,
                    command.MotivoDecision);
                if (!command.Aprobar)
                {
                    if (solicitud.ReservaPresupuestoId is Guid reservaExistente)
                        await _programas.LiberarReservaAsync(beca.ProgramaBecaId,
                            reservaExistente, solicitud.Id, token);
                    solicitud.Rechazar(command.MotivoDecision, contexto);
                    beca.RechazarRenovacionEnCurso(contexto);
                    await _unidad.GuardarCambiosAsync(token);
                    resultado = SolicitudRenovacionDto.Desde(solicitud);
                    return;
                }

                var ventana = await _ventanas.ObtenerAsync(beca.ProgramaBecaId,
                    solicitud.PeriodoSolicitadoId, token);
                if (ventana is null || !ventana.Activo || ventana.Inicio == default
                    || ventana.Fin == default || ventana.Fin < ventana.Inicio)
                    throw new ConfiguracionProgramaNoDisponibleException(
                        beca.ProgramaBecaId,
                        "Falta la ventana oficial activa para aprobar la renovación.");
                var hoy = DateOnly.FromDateTime(_reloj.UtcNow.UtcDateTime);
                if (hoy < ventana.Inicio || hoy > ventana.Fin
                    || hoy > solicitud.FechaLimiteVentanaRenovacion)
                    throw new ConflictoOperacionException(
                        "La ventana oficial de renovación ya no admite la aprobación.");

                var plan = await _planes.ObtenerAsync(beca.ProgramaBecaId,
                    solicitud.PeriodoSolicitadoId, token);
                ValidarPlan(solicitud, beca.ProgramaBecaId, plan);
                var reporte = await _reportes.ObtenerPorBecaYPeriodoParaConsultaAsync(
                    beca.Id, solicitud.PeriodoOrigenId, token);
                if (reporte?.Estado != EstadoReporteAcademico.Aprobado
                    || reporte.Evaluacion?.EstadoConfirmacion != EstadoConsecuencia.Confirmada
                    || !reporte.Evaluacion.CumpleCondiciones)
                    throw new ConflictoOperacionException(
                        "La evaluación académica vigente no habilita la renovación.");

                var reserva = await _programas.ReservarAsync(beca.ProgramaBecaId,
                    new SolicitudReservaPresupuesto("RENOVACION", solicitud.Id,
                        solicitud.PeriodoSolicitadoId, solicitud.MontoSolicitado, 1), token);
                if (reserva.ReservaId != Guid.Empty)
                {
                    reservaParaCompensar = reserva.ReservaId;
                    programaReserva = beca.ProgramaBecaId;
                }
                if (reserva.ReservaId == Guid.Empty
                    || reserva.Monto != solicitud.MontoSolicitado || reserva.Cupos != 1
                    || reserva.Estado is not ("PENDIENTE" or "CONFIRMADA")
                    || reserva.ExpiraEn <= _reloj.UtcNow)
                    throw new ConflictoOperacionException(
                        "MS-PRG no confirmó una reserva válida para la renovación.");
                var sucesora = beca.CrearSucesoraPorRenovacion(new DatosBecaSucesora(
                    solicitud.PeriodoSolicitadoId, plan!.CostoMatriculaReferenciaId,
                    reserva.Monto, reserva.ReservaId, plan.Cobertura, plan.Condiciones,
                    plan.RequiereCompromisoFirmado, plan.RequiereNuevaFirma,
                    string.Concat("B", solicitud.Id.ToString("N"))[..20],
                    plan.UrlPlantillaCompromiso), contexto);
                var calendario = CalendarioDesembolso.Programar(sucesora.Id,
                    solicitud.PeriodoSolicitadoId, plan.Frecuencia, plan.Destinatario,
                    reserva.Monto, plan.FechaInicio, plan.FechaFin, plan.Cuotas, contexto);
                await _becas.AdquirirBloqueoEstudianteAsync(
                    beca.PostulanteEstudianteId, token);
                var activas = await _becas.ObtenerActivasPorEstudianteAsync(
                    beca.PostulanteEstudianteId, token);
                PoliticaNoDobleBeneficio.Validar(sucesora,
                    activas.Where(item => item.Id != beca.Id));
                await _programas.ConfirmarReservaAsync(beca.ProgramaBecaId,
                    reserva.ReservaId, solicitud.Id, token);
                await _programas.ActivarReservaAsync(beca.ProgramaBecaId,
                    reserva.ReservaId, solicitud.Id, token);
                solicitud.ConfirmarReservaPresupuestaria(reserva.ReservaId,
                    reserva.Monto, contexto);
                solicitud.Aprobar(command.MotivoDecision, contexto);
                beca.Renovar(contexto);
                await _becas.AgregarAsync(sucesora, token);
                await _calendarios.AgregarAsync(calendario, token);
                await _outbox.EncolarAsync(new RenovacionBecaAprobadaEvento(
                    solicitud.Id, beca.Id, sucesora.Id, beca.CadenaBecaId,
                    solicitud.PeriodoOrigenId, solicitud.PeriodoSolicitadoId,
                    contexto.CorrelacionId, contexto.Fecha), solicitud.Version, token);
                await _outbox.EncolarAsync(new BecaActivadaNuevoPeriodoEvento(
                    sucesora.Id, beca.Id, beca.CadenaBecaId,
                    solicitud.PeriodoSolicitadoId, contexto.CorrelacionId,
                    contexto.Fecha), sucesora.Version, token);
                foreach (var cuota in calendario.Desembolsos)
                    await _outbox.EncolarAsync(new DesembolsoProgramadoEvento(
                        sucesora.Id, calendario.Id, cuota.Id,
                        contexto.CorrelacionId, contexto.Fecha),
                        calendario.Version, token);
                await _unidad.GuardarCambiosAsync(token);
                resultado = SolicitudRenovacionDto.Desde(solicitud);
            }, NivelAislamientoTransaccion.Serializable, cancellationToken);
            reservaParaCompensar = null;
        }
        catch
        {
            if (reservaParaCompensar is Guid reservaId)
            {
                try
                {
                    await _programas.LiberarReservaAsync(programaReserva,
                        reservaId, command.SolicitudRenovacionId,
                        CancellationToken.None);
                }
                catch
                {
                    // La reconciliación externa debe resolver una liberación fallida.
                }
            }
            throw;
        }

        return resultado ?? throw new InvalidOperationException(
            "La evaluación de renovación no produjo un resultado.");
    }

    private static void ValidarPlan(SolicitudRenovacion solicitud,
        Guid programaId, PlanNuevoPeriodo? plan)
    {
        if (plan is null || plan.ProgramaBecaId != programaId
            || plan.PeriodoAcademicoId != solicitud.PeriodoSolicitadoId
            || plan.CostoMatriculaReferenciaId == Guid.Empty
            || plan.MontoReferenciaMatricula <= 0m
            || decimal.Round(plan.MontoReferenciaMatricula, 2)
                != plan.MontoReferenciaMatricula
            || plan.Cobertura is null || plan.Condiciones is null
            || plan.Cuotas is null || plan.Cuotas.Count == 0
            || plan.Cuotas.Sum(cuota => cuota.Monto) != solicitud.MontoSolicitado
            || solicitud.MontoSolicitado <= 0m)
            throw new ConflictoOperacionException(
                "Faltan datos oficiales válidos del nuevo periodo para aprobar la renovación.");
        if (CalculadoraMontoOferta.CalcularMontoPorPeriodo(plan.Cobertura,
                plan.MontoReferenciaMatricula) != solicitud.MontoSolicitado)
            throw new ConflictoOperacionException(
                "El monto solicitado no coincide con la cobertura y el costo del nuevo periodo.");
    }
}
