namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando una decisión de caso límite ya registrada excede los cupos o el presupuesto efectivos vigentes al confirmar el ranking.
/// </summary>
public sealed class DecisionCasoLimiteInvalidaPorDisponibilidadException : ExcepcionReglaNegocio
{
    public const string CodigoError = "DECISION_CASO_LIMITE_EXCEDE_DISPONIBILIDAD";

    public DecisionCasoLimiteInvalidaPorDisponibilidadException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "Una decisión de caso límite registrada previamente excede los cupos o el presupuesto efectivos vigentes."
                : mensaje,
            422)
    {
    }
}
