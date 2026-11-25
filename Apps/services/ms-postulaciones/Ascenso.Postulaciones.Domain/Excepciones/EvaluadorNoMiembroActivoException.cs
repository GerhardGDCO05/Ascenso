namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta asignar una postulación a un evaluador que no es miembro activo del comité.
/// </summary>
public sealed class EvaluadorNoMiembroActivoException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUADOR_NO_MIEMBRO_ACTIVO";

    public EvaluadorNoMiembroActivoException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "El evaluador indicado no pertenece como miembro activo al comité evaluador."
                : mensaje,
            422)
    {
    }
}
