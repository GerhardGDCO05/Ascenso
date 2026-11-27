namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la selección propuesta para resolver un caso límite es vacía, duplicada o incluye postulaciones ajenas al grupo afectado.
/// </summary>
public sealed class SeleccionCasoLimiteInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "SELECCION_CASO_LIMITE_INVALIDA";

    public SeleccionCasoLimiteInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Las postulaciones seleccionadas no son válidas o no pertenecen al grupo afectado por el caso límite."
                : mensaje,
            422)
    {
    }
}
