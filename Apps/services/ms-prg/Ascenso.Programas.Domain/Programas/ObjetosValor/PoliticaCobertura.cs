using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Programas.ObjetosValor;

/// <summary>
/// Objeto de valor inmutable que define la modalidad y parámetros financieros de cobertura de un programa.
/// </summary>
public sealed record PoliticaCobertura
{
    private PoliticaCobertura()
    {
        Moneda = "USD";
    }

    public PoliticaCobertura(
        ModalidadCobertura modalidad,
        decimal porcentajeCobertura,
        decimal montoFijoPeriodo,
        string moneda = "USD")
    {
        if (!Enum.IsDefined(modalidad))
        {
            throw new PoliticaCoberturaInvalidaException("La modalidad de cobertura especificada no es válida.");
        }

        if (string.IsNullOrWhiteSpace(moneda) || !string.Equals(moneda.Trim(), "USD", StringComparison.OrdinalIgnoreCase))
        {
            throw new PoliticaCoberturaInvalidaException("La moneda de la política de cobertura debe ser USD.");
        }

        var porcentajeRedondeado = decimal.Round(porcentajeCobertura, 2, MidpointRounding.AwayFromZero);
        var montoRedondeado = decimal.Round(montoFijoPeriodo, 2, MidpointRounding.AwayFromZero);

        switch (modalidad)
        {
            case ModalidadCobertura.Total:
                if (porcentajeRedondeado != 100.00m || montoRedondeado < 0.00m)
                {
                    throw new PoliticaCoberturaInvalidaException(
                        "La modalidad TOTAL exige porcentajeCobertura igual a 100.00 y montoFijoPeriodo mayor o igual a 0.");
                }

                break;

            case ModalidadCobertura.Parcial:
                if (porcentajeRedondeado <= 0.00m || porcentajeRedondeado >= 100.00m || montoRedondeado < 0.00m)
                {
                    throw new PoliticaCoberturaInvalidaException(
                        "La modalidad PARCIAL exige porcentajeCobertura mayor que 0.00 y menor que 100.00.");
                }

                break;

            case ModalidadCobertura.MontoFijo:
                if (montoRedondeado <= 0.00m || porcentajeRedondeado is < 0.00m or > 100.00m)
                {
                    throw new PoliticaCoberturaInvalidaException(
                        "La modalidad MONTO_FIJO exige montoFijoPeriodo mayor que 0.00.");
                }

                break;
        }

        Modalidad = modalidad;
        PorcentajeCobertura = porcentajeRedondeado;
        MontoFijoPeriodo = montoRedondeado;
        Moneda = "USD";
    }

    public ModalidadCobertura Modalidad { get; }

    public decimal PorcentajeCobertura { get; }

    public decimal MontoFijoPeriodo { get; }

    public string Moneda { get; }
}
