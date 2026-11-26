using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Evaluaciones.ObjetosValor;

/// <summary>
/// Datos inmutables de entrada para registrar o actualizar la calificación de un criterio.
/// </summary>
public sealed record class DatosCalificacionCriterio : ValueObject
{
    public const int LongitudMaximaComentario = 1000;
    public const int DecimalesMaximos = 2;

    public DatosCalificacionCriterio(
        Guid criterioEvaluacionId,
        decimal puntajeAsignado,
        string? comentario)
    {
        if (criterioEvaluacionId == Guid.Empty)
        {
            throw new EvaluacionInvalidaException(
                "El identificador del criterio de evaluación es obligatorio.");
        }

        if (decimal.Round(puntajeAsignado, DecimalesMaximos) != puntajeAsignado)
        {
            throw new EvaluacionInvalidaException(
                $"El puntaje asignado admite como máximo {DecimalesMaximos} decimales.");
        }

        CriterioEvaluacionId = criterioEvaluacionId;
        PuntajeAsignado = puntajeAsignado;
        Comentario = NormalizarComentario(comentario);
    }

    public Guid CriterioEvaluacionId { get; }

    public decimal PuntajeAsignado { get; }

    public string? Comentario { get; }

    private static string? NormalizarComentario(string? comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return null;
        }

        var normalizado = comentario.Trim();
        if (normalizado.Length > LongitudMaximaComentario)
        {
            throw new EvaluacionInvalidaException(
                $"El comentario por criterio no puede superar los {LongitudMaximaComentario} caracteres.");
        }

        return normalizado;
    }
}
