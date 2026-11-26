using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Renovacion.Datos;
using Ascenso.Becas.Domain.Renovacion.Eventos;

namespace Ascenso.Becas.Domain.Renovacion.Agregados;

/// <summary>Decisión de continuidad de una beca para el periodo siguiente.</summary>
public sealed class SolicitudRenovacion : AggregateRoot
{
    private SolicitudRenovacion()
    {
    }

    private SolicitudRenovacion(DatosSolicitudRenovacion datos,
        ContextoOperacion contexto)
        : base(Guid.CreateVersion7(), contexto.Fecha, contexto.UsuarioAutorId)
    {
        BecaId = datos.BecaId;
        PeriodoOrigenId = datos.PeriodoOrigenId;
        PeriodoSolicitadoId = datos.PeriodoSolicitadoId;
        FechaSolicitud = contexto.Fecha.ToUniversalTime();
        FechaLimiteVentanaRenovacion = datos.FechaLimiteVentanaRenovacion;
        CumpleRendimientoAcademico = datos.CumpleRendimientoAcademico;
        TienePresupuestoDisponible = datos.TienePresupuestoDisponible;
        Estado = EstadoRenovacion.SolicitadaEnCurso;
    }

    public Guid BecaId { get; private set; }
    public Guid PeriodoOrigenId { get; private set; }
    public Guid PeriodoSolicitadoId { get; private set; }
    public DateTimeOffset FechaSolicitud { get; private set; }
    public DateOnly FechaLimiteVentanaRenovacion { get; private set; }
    public bool CumpleRendimientoAcademico { get; private set; }
    public bool TienePresupuestoDisponible { get; private set; }
    public decimal MontoReservadoRenovacion { get; private set; }
    public Guid? ReservaPresupuestoId { get; private set; }
    public EstadoRenovacion Estado { get; private set; }
    public string? MotivoDecision { get; private set; }
    public Guid? EvaluadoPorCoordinadorId { get; private set; }
    public DateTimeOffset? FechaDecision { get; private set; }
    public string? ReferenciaJobVencimientoId { get; private set; }

    public static SolicitudRenovacion Solicitar(DatosSolicitudRenovacion datos,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentNullException.ThrowIfNull(contexto);
        if (datos.BecaId == Guid.Empty || datos.PeriodoOrigenId == Guid.Empty
            || datos.PeriodoSolicitadoId == Guid.Empty
            || datos.PeriodoOrigenId == datos.PeriodoSolicitadoId
            || datos.FechaLimiteVentanaRenovacion == default)
            throw new ArgumentException("Los datos de la solicitud de renovación son inválidos.", nameof(datos));
        if (DateOnly.FromDateTime(contexto.Fecha.UtcDateTime)
            > datos.FechaLimiteVentanaRenovacion)
            throw new InvalidOperationException("La ventana de renovación ya venció.");
        if (!datos.CumpleRendimientoAcademico || !datos.TienePresupuestoDisponible)
            throw new InvalidOperationException(
                "La renovación exige rendimiento académico y presupuesto disponible.");

        var solicitud = new SolicitudRenovacion(datos, contexto);
        solicitud.RegistrarEventoDominio(new RenovacionBecaSolicitadaEvento(
            solicitud.Id, datos.BecaId, datos.PeriodoOrigenId,
            datos.PeriodoSolicitadoId, contexto.CorrelacionId, contexto.Fecha));
        return solicitud;
    }

    public void RegistrarReferenciaJobVencimiento(string referenciaJob,
        ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ExigirEnCurso();
        if (string.IsNullOrWhiteSpace(referenciaJob))
            throw new ArgumentException("La referencia del job es obligatoria.", nameof(referenciaJob));
        if (ReferenciaJobVencimientoId is not null)
            throw new InvalidOperationException("La solicitud ya tiene un job de vencimiento.");

        ReferenciaJobVencimientoId = referenciaJob.Trim();
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public void ConfirmarReservaPresupuestaria(Guid reservaPresupuestoId,
        decimal montoReservado, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ExigirEnCurso();
        if (!TienePresupuestoDisponible)
            throw new InvalidOperationException("No hay presupuesto disponible para la renovación.");
        if (reservaPresupuestoId == Guid.Empty || montoReservado <= 0m)
            throw new ArgumentException("La reserva y su monto deben ser válidos.");
        if (ReservaPresupuestoId is not null)
            throw new InvalidOperationException("La solicitud ya tiene una reserva confirmada.");

        ReservaPresupuestoId = reservaPresupuestoId;
        MontoReservadoRenovacion = montoReservado;
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }

    public void Aprobar(string motivoDecision, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ExigirEnCurso();
        ValidarMotivo(motivoDecision);
        if (DateOnly.FromDateTime(contexto.Fecha.UtcDateTime)
            > FechaLimiteVentanaRenovacion)
            throw new InvalidOperationException("No se puede aprobar una renovación vencida.");
        if (!CumpleRendimientoAcademico || !TienePresupuestoDisponible
            || ReservaPresupuestoId is null || MontoReservadoRenovacion <= 0m)
            throw new InvalidOperationException(
                "La aprobación exige rendimiento académico y reserva presupuestaria confirmada.");

        Estado = EstadoRenovacion.Aprobada;
        RegistrarDecision(motivoDecision, contexto, contexto.UsuarioAutorId);
    }

    public void Rechazar(string motivoDecision, ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ExigirEnCurso();
        ValidarMotivo(motivoDecision);
        Estado = EstadoRenovacion.Rechazada;
        RegistrarDecision(motivoDecision, contexto, contexto.UsuarioAutorId);
    }

    public void Expirar(ContextoOperacion contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ExigirEnCurso();
        if (DateOnly.FromDateTime(contexto.Fecha.UtcDateTime)
            <= FechaLimiteVentanaRenovacion)
            throw new InvalidOperationException("La ventana de renovación todavía está abierta.");

        Estado = EstadoRenovacion.ExpiradaConcluida;
        RegistrarDecision("Vencimiento de la ventana de renovación.", contexto, null);
    }

    private void ExigirEnCurso()
    {
        if (Estado != EstadoRenovacion.SolicitadaEnCurso)
            throw new InvalidOperationException("La solicitud de renovación ya terminó.");
    }

    private static void ValidarMotivo(string motivoDecision)
    {
        if (string.IsNullOrWhiteSpace(motivoDecision))
            throw new ArgumentException("El motivo de la decisión es obligatorio.", nameof(motivoDecision));
    }

    private void RegistrarDecision(string motivoDecision,
        ContextoOperacion contexto, Guid? coordinadorId)
    {
        MotivoDecision = motivoDecision.Trim();
        EvaluadoPorCoordinadorId = coordinadorId;
        FechaDecision = contexto.Fecha.ToUniversalTime();
        ActualizarAuditoria(contexto.UsuarioAutorId, contexto.Fecha);
    }
}
