namespace Ascenso.Becas.Domain.Comun;

/// <summary>
/// Registro base append-only. No expone operaciones de modificación ni borrado.
/// </summary>
public abstract class EntidadInmutable : IEquatable<EntidadInmutable>
{
    public Guid Id { get; }

    public DateTimeOffset FechaCreacion { get; }

    public Guid UsuarioAutorId { get; }

    public Guid CorrelacionId { get; }

    // Constructor requerido por los adaptadores de persistencia.
    protected EntidadInmutable()
    {
    }

    protected EntidadInmutable(
        Guid id,
        DateTimeOffset fechaCreacion,
        Guid usuarioAutorId,
        Guid correlacionId)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("El identificador del registro es obligatorio.", nameof(id));
        }

        if (fechaCreacion == default)
        {
            throw new ArgumentException("La fecha de creación es obligatoria.", nameof(fechaCreacion));
        }

        if (usuarioAutorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario autor es obligatorio.", nameof(usuarioAutorId));
        }

        if (correlacionId == Guid.Empty)
        {
            throw new ArgumentException("La correlación es obligatoria.", nameof(correlacionId));
        }

        Id = id;
        FechaCreacion = fechaCreacion.ToUniversalTime();
        UsuarioAutorId = usuarioAutorId;
        CorrelacionId = correlacionId;
    }

    public bool Equals(EntidadInmutable? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other)
            || (GetType() == other.GetType() && Id == other.Id);
    }

    public override bool Equals(object? obj)
        => obj is EntidadInmutable other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator ==(EntidadInmutable? izquierda, EntidadInmutable? derecha)
        => Equals(izquierda, derecha);

    public static bool operator !=(EntidadInmutable? izquierda, EntidadInmutable? derecha)
        => !Equals(izquierda, derecha);
}
