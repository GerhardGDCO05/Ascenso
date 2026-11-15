using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Enums;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando se cierra contablemente un período académico de un programa de becas.
/// </summary>
public sealed record class PeriodoProgramaCerradoDominioEvent : EventoDominio
{
    public PeriodoProgramaCerradoDominioEvent(
        Guid programaBecaId,
        string periodoAcademicoId,
        decimal saldoNoEjecutadoConsolidado,
        decimal presupuestoEjecutadoTotal,
        EstadoProgramaBeca estadoResultante,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        PeriodoAcademicoId = periodoAcademicoId;
        SaldoNoEjecutadoConsolidado = saldoNoEjecutadoConsolidado;
        PresupuestoEjecutadoTotal = presupuestoEjecutadoTotal;
        EstadoResultante = estadoResultante;
    }

    public Guid ProgramaBecaId { get; }

    public string PeriodoAcademicoId { get; }

    public decimal SaldoNoEjecutadoConsolidado { get; }

    public decimal PresupuestoEjecutadoTotal { get; }

    public EstadoProgramaBeca EstadoResultante { get; }
}
