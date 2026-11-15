using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se reserva atómicamente un cupo y monto presupuestario en un programa.
/// </summary>
public sealed record class PresupuestoProgramaReservadoDominioEvent : EventoDominio
{
    public PresupuestoProgramaReservadoDominioEvent(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal montoReservado,
        decimal presupuestoDisponibleResultante,
        int cuposDisponiblesResultantes,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        ReservaId = reservaId;
        ReferenciaExternaId = referenciaExternaId;
        PeriodoAcademicoId = periodoAcademicoId;
        MontoReservado = montoReservado;
        PresupuestoDisponibleResultante = presupuestoDisponibleResultante;
        CuposDisponiblesResultantes = cuposDisponiblesResultantes;
    }

    public Guid ProgramaBecaId { get; }

    public Guid ReservaId { get; }

    public Guid ReferenciaExternaId { get; }

    public string PeriodoAcademicoId { get; }

    public decimal MontoReservado { get; }

    public decimal PresupuestoDisponibleResultante { get; }

    public int CuposDisponiblesResultantes { get; }
}
