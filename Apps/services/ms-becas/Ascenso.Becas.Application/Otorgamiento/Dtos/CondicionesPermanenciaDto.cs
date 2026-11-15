using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Condiciones de permanencia capturadas al emitir la oferta.
/// </summary>
public sealed record CondicionesPermanenciaDto(
    decimal PromedioMinimoRenovacion,
    int MateriasMinimasAprobadas,
    int MaximoMateriasReprobadas,
    int HorasServicioPorPeriodo,
    string ConsecuenciaIncumplimiento)
{
    public static CondicionesPermanenciaDto Desde(CondicionesPermanencia condiciones)
    {
        ArgumentNullException.ThrowIfNull(condiciones);

        return new CondicionesPermanenciaDto(
            condiciones.PromedioMinimoRenovacion,
            condiciones.MateriasMinimasAprobadas,
            condiciones.MaximoMateriasReprobadas,
            condiciones.HorasServicioPorPeriodo,
            condiciones.ConsecuenciaIncumplimiento.ACodigo());
    }
}
