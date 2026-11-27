namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta consolidar una postulación que aún tiene asignaciones vigentes sin completar.
/// </summary>
public sealed class EvaluacionesPendientesParaConsolidarException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACIONES_PENDIENTES";

    public EvaluacionesPendientesParaConsolidarException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "La postulación aún tiene asignaciones vigentes pendientes de completar su evaluación definitiva."
                : mensaje,
            409)
    {
    }
}
