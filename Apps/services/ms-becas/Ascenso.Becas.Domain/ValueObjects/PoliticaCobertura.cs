using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Enums;

namespace Ascenso.Becas.Domain.ValueObjects;

/// <summary>
/// Estrategia inmutable de cobertura de un programa de becas.
/// </summary>
public sealed record class PoliticaCobertura : ValueObject
{
    public PoliticaCobertura(
        TipoCobertura modalidad,
        decimal porcentajeCobertura,
        decimal montoFijoPeriodo,
        string moneda)
    {
        if (!Enum.IsDefined(modalidad))
        {
            throw new ArgumentOutOfRangeException(nameof(modalidad), "La modalidad de cobertura no es válida.");
        }

        if (porcentajeCobertura is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(porcentajeCobertura),
                "El porcentaje de cobertura debe estar entre 0 y 100.");
        }

        if (decimal.Round(porcentajeCobertura, 2) != porcentajeCobertura)
        {
            throw new ArgumentException(
                "El porcentaje de cobertura admite como máximo dos decimales.",
                nameof(porcentajeCobertura));
        }

        if (montoFijoPeriodo < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(montoFijoPeriodo),
                "El monto fijo por periodo no puede ser negativo.");
        }

        if (decimal.Round(montoFijoPeriodo, 2) != montoFijoPeriodo)
        {
            throw new ArgumentException(
                "El monto fijo por periodo admite como máximo dos decimales.",
                nameof(montoFijoPeriodo));
        }

        if (!string.Equals(moneda?.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("La moneda de la política de cobertura debe ser USD.", nameof(moneda));
        }

        Modalidad = modalidad;
        PorcentajeCobertura = porcentajeCobertura;
        MontoFijoPeriodo = montoFijoPeriodo;
        Moneda = "USD";
    }

    public TipoCobertura Modalidad { get; }

    public decimal PorcentajeCobertura { get; }

    public decimal MontoFijoPeriodo { get; }

    public string Moneda { get; }
}
