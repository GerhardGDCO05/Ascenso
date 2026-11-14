using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando una reserva presupuestaria se confirma y pasa a presupuesto ejecutado y cupo ocupado.
/// </summary>
public sealed record class PresupuestoProgramaConfirmadoDominioEvent : EventoDominio
{
    public PresupuestoProgramaConfirmadoDominioEvent(
        Guid programaBecaId,
        Guid reservaId,
        Guid referenciaExternaId,
        string periodoAcademicoId,
        decimal montoConfirmado,
        decimal presupuestoEjecutadoResultante,
        int cuposOcupadosResultantes,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        ReservaId = reservaId;
        ReferenciaExternaId = referenciaExternaId;
        PeriodoAcademicoId = periodoAcademicoId;
        MontoConfirmado = montoConfirmado;
        PresupuestoEjecutadoResultante = presupuestoEjecutadoResultante;
        CuposOcupadosResultantes = cuposOcupadosResultantes;
    }

    public Guid ProgramaBecaId { get; }

    public Guid ReservaId { get; }

    public Guid ReferenciaExternaId { get; }

    public string PeriodoAcademicoId { get; }

    public decimal MontoConfirmado { get; }

    public decimal PresupuestoEjecutadoResultante { get; }

    public int CuposOcupadosResultantes { get; }
}
