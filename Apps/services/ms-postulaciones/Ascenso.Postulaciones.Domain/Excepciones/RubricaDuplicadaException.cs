namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando ya existe una rúbrica de evaluación para la combinación de programa y convocatoria.
/// </summary>
public sealed class RubricaDuplicadaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "RUBRICA_DUPLICADA";

    public RubricaDuplicadaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Ya existe una rúbrica de evaluación para el programa y la convocatoria indicados."
                : mensaje,
            409)
    {
    }
}
