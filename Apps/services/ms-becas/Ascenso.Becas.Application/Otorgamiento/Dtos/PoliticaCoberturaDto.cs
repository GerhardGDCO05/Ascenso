using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Fotografía de la política de cobertura devuelta por las consultas.
/// </summary>
public sealed record PoliticaCoberturaDto(
    string Modalidad,
    decimal PorcentajeCobertura,
    decimal MontoFijoPeriodo,
    string Moneda)
{
    public static PoliticaCoberturaDto Desde(PoliticaCobertura politica)
    {
        ArgumentNullException.ThrowIfNull(politica);

        return new PoliticaCoberturaDto(
            politica.Modalidad.ACodigo(),
            politica.PorcentajeCobertura,
            politica.MontoFijoPeriodo,
            politica.Moneda);
    }
}
