namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando el agregado no admite la operación en su estado actual
/// por una razón distinta a una transición de estado explícita.
/// </summary>
public sealed class ConflictoOperacionException : AplicacionException
{
    public const string CodigoError = "CONFLICTO_OPERACION";

    public ConflictoOperacionException(string detalle)
        : base(CodigoError, 409, detalle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detalle);
    }
}
