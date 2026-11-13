namespace Ascenso.Programas.Domain.Comun;

/// <summary>
/// Entidad base mutable con auditoría local y token de concurrencia optimista.
/// </summary>
public abstract class EntidadAuditable<TId> : IEquatable<EntidadAuditable<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public DateTimeOffset FechaCreacion { get; protected set; }

    public Guid UsuarioCreacionId { get; protected set; }

    public DateTimeOffset FechaUltimaModificacion { get; protected set; }

    public Guid UsuarioModificacionId { get; protected set; }

    public int Version { get; protected set; }

    // Constructor requerido por los adaptadores de persistencia.
    protected EntidadAuditable()
    {
    }

    protected EntidadAuditable(TId id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException("El identificador de la entidad es obligatorio.", nameof(id));
        }

        if (usuarioCreacionId == Guid.Empty)
        {
            throw new ArgumentException("El usuario de creación es obligatorio.", nameof(usuarioCreacionId));
        }

        if (fechaCreacion == default)
        {
            throw new ArgumentException("La fecha de creación es obligatoria.", nameof(fechaCreacion));
        }

        Id = id;
        FechaCreacion = fechaCreacion.ToUniversalTime();
        UsuarioCreacionId = usuarioCreacionId;
        FechaUltimaModificacion = FechaCreacion;
        UsuarioModificacionId = usuarioCreacionId;
        Version = 1;
    }

    protected void ActualizarAuditoria(Guid usuarioModificacionId, DateTimeOffset fechaModificacion)
    {
        if (usuarioModificacionId == Guid.Empty)
        {
            throw new ArgumentException("El usuario de modificación es obligatorio.", nameof(usuarioModificacionId));
        }

        if (fechaModificacion == default)
        {
            throw new ArgumentException("La fecha de modificación es obligatoria.", nameof(fechaModificacion));
        }

        FechaUltimaModificacion = fechaModificacion.ToUniversalTime();
        UsuarioModificacionId = usuarioModificacionId;
        Version++;
    }

    public bool Equals(EntidadAuditable<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other)
            || (GetType() == other.GetType()
                && EqualityComparer<TId>.Default.Equals(Id, other.Id));
    }

    public override bool Equals(object? obj)
        => obj is EntidadAuditable<TId> other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator ==(EntidadAuditable<TId>? izquierda, EntidadAuditable<TId>? derecha)
        => Equals(izquierda, derecha);

    public static bool operator !=(EntidadAuditable<TId>? izquierda, EntidadAuditable<TId>? derecha)
        => !Equals(izquierda, derecha);
}

/// <summary>
/// Variante no genérica para entidades cuyo identificador es un UUIDv7.
/// </summary>
public abstract class EntidadAuditable : EntidadAuditable<Guid>
{
    protected EntidadAuditable()
    {
    }

    protected EntidadAuditable(Guid id, DateTimeOffset fechaCreacion, Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
    }
}

