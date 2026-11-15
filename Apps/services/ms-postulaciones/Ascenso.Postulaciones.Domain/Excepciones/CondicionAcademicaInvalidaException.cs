namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando la condición académica declarada no es admitida por el
/// dominio.
/// </summary>
public sealed class CondicionAcademicaInvalidaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "CONDICION_ACADEMICA_INVALIDA";

    public CondicionAcademicaInvalidaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La condición académica debe ser ADMITIDO o MATRICULADO."
                : mensaje)
    {
    }
}
