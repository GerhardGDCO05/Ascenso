namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un valor no pertenece al catálogo cerrado esperado.
/// </summary>
public sealed class CatalogoInvalidoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CATALOGO_INVALIDO";

    public CatalogoInvalidoException(string campo, string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? $"El valor del catálogo '{campo}' no es válido."
                : mensaje)
    {
    }
}
