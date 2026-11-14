using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se asignan fondos de uno o más donantes a un programa de becas.
/// </summary>
public sealed record class PresupuestoProgramaAsignadoDominioEvent : EventoDominio
{
    public PresupuestoProgramaAsignadoDominioEvent(
        Guid programaBecaId,
        Guid asignacionId,
        Guid donanteId,
        string periodoAcademicoId,
        decimal montoAsignado,
        decimal presupuestoTotalResultante,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        AsignacionId = asignacionId;
        DonanteId = donanteId;
        PeriodoAcademicoId = periodoAcademicoId;
        MontoAsignado = montoAsignado;
        PresupuestoTotalResultante = presupuestoTotalResultante;
    }

    public Guid ProgramaBecaId { get; }

    public Guid AsignacionId { get; }

    public Guid DonanteId { get; }

    public string PeriodoAcademicoId { get; }

    public decimal MontoAsignado { get; }

    public decimal PresupuestoTotalResultante { get; }
}
