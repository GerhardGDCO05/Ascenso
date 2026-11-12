using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Domain.Otorgamiento.Servicios;

/// <summary>
/// Calcula los montos de la oferta a partir de la política de cobertura y
/// del costo de matrícula de referencia vigente al momento de la emisión.
/// </summary>
public static class CalculadoraMontoOferta
{
    public static decimal CalcularMontoPorPeriodo(
        PoliticaCobertura politicaCobertura,
        decimal costoMatriculaReferencia)
    {
        ArgumentNullException.ThrowIfNull(politicaCobertura);

        if (costoMatriculaReferencia < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(costoMatriculaReferencia),
                "El costo de matrícula no puede ser negativo.");
        }

        if (Redondear(costoMatriculaReferencia) != costoMatriculaReferencia)
        {
            throw new ArgumentException(
                "El costo de matrícula admite como máximo dos decimales.",
                nameof(costoMatriculaReferencia));
        }

        var monto = politicaCobertura.Modalidad switch
        {
            TipoCobertura.Total => costoMatriculaReferencia,
            TipoCobertura.Parcial => costoMatriculaReferencia * politicaCobertura.PorcentajeCobertura / 100m,
            TipoCobertura.MontoFijo => politicaCobertura.MontoFijoPeriodo,
            _ => throw new ArgumentOutOfRangeException(
                nameof(politicaCobertura),
                "La modalidad de cobertura no es válida.")
        };

        return Redondear(monto);
    }

    public static decimal CalcularMontoTotalEstimado(decimal montoPorPeriodo, int duracionPeriodos)
    {
        if (montoPorPeriodo < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(montoPorPeriodo),
                "El monto por periodo no puede ser negativo.");
        }

        if (duracionPeriodos <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duracionPeriodos),
                "La duración en periodos debe ser mayor que cero.");
        }

        return Redondear(montoPorPeriodo * duracionPeriodos);
    }

    public static decimal Redondear(decimal monto)
        => decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
}
