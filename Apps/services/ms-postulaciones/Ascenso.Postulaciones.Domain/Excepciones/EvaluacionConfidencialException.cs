namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando un evaluador intenta acceder o operar sobre una asignación o evaluación ajena
/// en violación de la evaluación ciega (RN-PLT-02).
/// </summary>
public sealed class EvaluacionConfidencialException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_CONFIDENCIAL";

    public EvaluacionConfidencialException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No tiene permiso para acceder ni modificar la evaluación asignada a otro evaluador."
                : mensaje,
            403)
    {
    }
}
