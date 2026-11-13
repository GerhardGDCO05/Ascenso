namespace Ascenso.Programas.Domain.Excepciones;

public sealed class ExcepcionConflictoConcurrencia : DomainException
{
    public const string CodigoError = "CONFLICTO_CONCURRENCIA";

    public ExcepcionConflictoConcurrencia(string? mensaje = null)
        : base(
            CodigoError,
            409,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El recurso fue modificado concurrentemente por otra operación. Reintente la solicitud."
                : mensaje)
    {
    }
}

