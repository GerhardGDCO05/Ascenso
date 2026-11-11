using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Excepciones;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;

namespace Ascenso.Becas.Domain.Otorgamiento.Servicios;

/// <summary>
/// Aplica la invariante de no doble beneficio antes de activar una beca.
/// </summary>
public static class PoliticaNoDobleBeneficio
{
    public static void Validar(Beca candidata, IEnumerable<Beca> becasDelEstudiante)
    {
        ArgumentNullException.ThrowIfNull(candidata);
        ArgumentNullException.ThrowIfNull(becasDelEstudiante);

        var otrasActivas = becasDelEstudiante
            .Where(beca => beca.Id != candidata.Id)
            .Where(beca => beca.PostulanteEstudianteId == candidata.PostulanteEstudianteId)
            .Where(beca => beca.Estado == EstadoBeca.Activa)
            .ToArray();

        if (otrasActivas.Any(beca => beca.CoberturaAplicada.Modalidad == TipoCobertura.Total))
        {
            throw new DobleBeneficioException(
                candidata.PostulanteEstudianteId,
                "ya posee otra beca ACTIVA con cobertura TOTAL.");
        }

        var conflictoEnPeriodo = otrasActivas.Any(beca =>
            beca.PeriodoVigenteActualId == candidata.PeriodoVigenteActualId
            && !(beca.PermiteAcumulacion && candidata.PermiteAcumulacion));

        if (conflictoEnPeriodo)
        {
            throw new DobleBeneficioException(
                candidata.PostulanteEstudianteId,
                "ya posee otra beca ACTIVA en el mismo periodo y la acumulación no está permitida en ambos programas.");
        }
    }
}
