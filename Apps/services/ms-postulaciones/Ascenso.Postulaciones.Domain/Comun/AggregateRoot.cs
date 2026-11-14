namespace Ascenso.Postulaciones.Domain.Comun;

/// <summary>
/// Raíz de agregado. Acumula los eventos de dominio que serán publicados
/// después de confirmar la transacción local.
/// </summary>
public abstract class AggregateRoot<TId> : EntidadAuditable<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _eventosDominio = [];

    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
    }

    public IReadOnlyCollection<IDomainEvent> EventosDominio => _eventosDominio.AsReadOnly();

    protected void RegistrarEventoDominio(IDomainEvent eventoDominio)
    {
        ArgumentNullException.ThrowIfNull(eventoDominio);
        _eventosDominio.Add(eventoDominio);
    }

    protected void LimpiarEventosDominio()
        => _eventosDominio.Clear();
}

/// <summary>
/// Variante no genérica para agregados identificados por UUIDv7.
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
    }
}

