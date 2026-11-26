using System.Globalization;

namespace Ascenso.Postulaciones.Domain.Excepciones;

/// <summary>
/// Se produce cuando se intenta enviar definitivamente una evaluación con criterios sin calificar.
/// </summary>
public sealed class EvaluacionIncompletaException : ExcepcionReglaNegocio
{
    public const string CodigoError = "EVALUACION_INCOMPLETA";

    public EvaluacionIncompletaException(string? mensaje = null)
        : base(
            CodigoError,
            string.IsNullOrWhiteSpace(mensaje)
                ? "No se puede enviar la evaluación porque faltan criterios por calificar."
                : mensaje,
            422)
    {
    }

    public EvaluacionIncompletaException(int criteriosFaltantes)
        : base(CodigoError, ConstruirMensaje(criteriosFaltantes), 422)
    {
    }

    private static string ConstruirMensaje(int criteriosFaltantes)
        => criteriosFaltantes == 1
            ? "No se puede enviar la evaluación porque falta 1 criterio por calificar."
            : string.Create(
                CultureInfo.InvariantCulture,
                $"No se puede enviar la evaluación porque faltan {criteriosFaltantes} criterios por calificar.");
}
