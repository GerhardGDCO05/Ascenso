namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una asignación de evaluación no se encuentra en un estado que permita
/// la operación solicitada.
/// </summary>
public sealed class AsignacionNoEvaluableException : ExcepcionReglaNegocio
{
    public const string CodigoError = "ASIGNACION_NO_EVALUABLE";

    public AsignacionNoEvaluableException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La asignación de evaluación no se encuentra en un estado evaluable."
                : mensaje,
            409)
    {
    }
}
