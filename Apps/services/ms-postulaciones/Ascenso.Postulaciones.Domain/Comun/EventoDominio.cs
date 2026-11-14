namespace Ascenso.Postulaciones.Domain.Comun;

/// <summary>
/// Base inmutable en español para eventos de dominio de MS-POS.
/// </summary>
public abstract record class EventoDominio : DomainEvent
{
    protected EventoDominio(Guid correlacionId, DateTimeOffset ocurridoEn, Guid? eventoId = null)
        : base(correlacionId, ocurridoEn, eventoId)
    {
    }
}

