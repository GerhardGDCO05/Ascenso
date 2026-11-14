using Ascenso.Programas.Domain.Comun;

namespace Ascenso.Programas.Domain.Programas.Eventos;

/// <summary>
/// Evento de dominio emitido cuando un programa completa su elegibilidad y transiciona a estado Configurado.
/// </summary>
public sealed record class ProgramaBecaConfiguradoDominioEvent : EventoDominio
{
    public ProgramaBecaConfiguradoDominioEvent(
        Guid programaBecaId,
        string codigo,
        decimal presupuestoTotalAsignado,
        int cupoTotal,
        IReadOnlyList<Guid> universidadesElegiblesIds,
        IReadOnlyList<Guid> carrerasElegiblesIds,
        Guid correlacionId,
        DateTimeOffset ocurridoEn)
        : base(correlacionId, ocurridoEn)
    {
        ProgramaBecaId = programaBecaId;
        Codigo = codigo;
        PresupuestoTotalAsignado = presupuestoTotalAsignado;
        CupoTotal = cupoTotal;
        UniversidadesElegiblesIds = universidadesElegiblesIds;
        CarrerasElegiblesIds = carrerasElegiblesIds;
    }

    public Guid ProgramaBecaId { get; }

    public string Codigo { get; }

    public decimal PresupuestoTotalAsignado { get; }

    public int CupoTotal { get; }

    public IReadOnlyList<Guid> UniversidadesElegiblesIds { get; }

    public IReadOnlyList<Guid> CarrerasElegiblesIds { get; }
}
