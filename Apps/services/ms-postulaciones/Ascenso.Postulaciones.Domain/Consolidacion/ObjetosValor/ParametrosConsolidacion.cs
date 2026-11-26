using Ascenso.Postulaciones.Domain.Comun;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que encapsula los parámetros de negocio aplicados al cálculo de consolidación.
/// </summary>
public sealed record class ParametrosConsolidacion : ValueObject
{
    public const int EvaluadoresMinimosPorDefecto = 2;

    public const decimal UmbralDiscrepanciaPorDefecto = 20.00m;

    public const decimal UmbralMinimoExclusivo = 0.00m;

    public const decimal UmbralMaximo = 100.00m;

    public static ParametrosConsolidacion PorDefecto { get; } =
        new(EvaluadoresMinimosPorDefecto, UmbralDiscrepanciaPorDefecto);

    public ParametrosConsolidacion(
        int evaluadoresMinimosRequeridos = EvaluadoresMinimosPorDefecto,
        decimal umbralDiscrepanciaPuntos = UmbralDiscrepanciaPorDefecto)
    {
        if (evaluadoresMinimosRequeridos < EvaluadoresMinimosPorDefecto)
        {
            throw new ConsolidacionInvalidaException(
                $"El mínimo de evaluadores requeridos debe ser mayor o igual a {EvaluadoresMinimosPorDefecto}.");
        }

        if (umbralDiscrepanciaPuntos <= UmbralMinimoExclusivo
            || umbralDiscrepanciaPuntos > UmbralMaximo)
        {
            throw new ConsolidacionInvalidaException(
                "El umbral de discrepancia debe ser estrictamente mayor a 0.00 y menor o igual a 100.00.");
        }

        if (decimal.Round(umbralDiscrepanciaPuntos, 2) != umbralDiscrepanciaPuntos)
        {
            throw new ConsolidacionInvalidaException(
                "El umbral de discrepancia no puede tener más de dos decimales.");
        }

        EvaluadoresMinimosRequeridos = evaluadoresMinimosRequeridos;
        UmbralDiscrepanciaPuntos = umbralDiscrepanciaPuntos;
    }

    public int EvaluadoresMinimosRequeridos { get; }

    public decimal UmbralDiscrepanciaPuntos { get; }
}
