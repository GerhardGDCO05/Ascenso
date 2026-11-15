namespace Ascenso.Postulaciones.Domain.Excepciones;

public sealed class ExcepcionConflictoConcurrencia : DomainException
{
    public const string CodigoError = "CONFLICTO_CONCURRENCIA";
    public const string CodigoRestriccionUnicidad = "RESTRICCION_UNICIDAD";

    public ExcepcionConflictoConcurrencia(string? mensaje = null, string codigo = CodigoError)
        : base(
            codigo,
            409,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El recurso fue modificado concurrentemente por otra operación. Reintente la solicitud."
                : mensaje)
    {
    }
}


