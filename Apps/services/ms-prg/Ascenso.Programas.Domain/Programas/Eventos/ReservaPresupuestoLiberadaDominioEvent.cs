using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se libera una reserva presupuestaria restituyendo el saldo y cupo disponible.
/// </summary>
public sealed record class ReservaPresupuestoLiberadaDominioEvent : EventoDominio
{
    public ReservaPresupuestoLiberadaDominioEvent(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal montoLiberado,
        string motivo,
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
        MontoLiberado = montoLiberado;
        Motivo = motivo;
        PresupuestoDisponibleResultante = presupuestoDisponibleResultante;
        CuposDisponiblesResultantes = cuposDisponiblesResultantes;
    }

    public Guid ProgramaBecaId { get; }

    public Guid ReservaId { get; }

    public Guid ReferenciaExternaId { get; }

    public string PeriodoAcademicoId { get; }

    public decimal MontoLiberado { get; }

    public string Motivo { get; }

    public decimal PresupuestoDisponibleResultante { get; }

    public int CuposDisponiblesResultantes { get; }
}
