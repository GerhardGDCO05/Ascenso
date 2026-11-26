using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;
using Ascenso.Postulaciones.Domain.Rubricas.Entidades;

namespace Ascenso.Postulaciones.Application.Rubricas.DTOs;

public static class RubricaMapeador
{
    public static RubricaDto ADto(this RubricaEvaluacion rubrica)
    {
        ArgumentNullException.ThrowIfNull(rubrica);

        return new RubricaDto(
            rubrica.Id,
            rubrica.ProgramaBecaId,
            rubrica.ConvocatoriaId,
            rubrica.Nombre,
            rubrica.Estado.ACodigo(),
            rubrica.FechaPublicacion,
            rubrica.Criterios
                .OrderBy(criterio => criterio.Orden)
                .Select(criterio => criterio.ADto())
                .ToArray(),
            rubrica.SumaPesos,
            rubrica.DiferenciaPara100,
            rubrica.MotivoBloqueo?.ACodigo(),
            rubrica.FechaBloqueo,
            rubrica.Estado != EstadoRubrica.Borrador);
    }

    public static CriterioEvaluacionDto ADto(this CriterioEvaluacion criterio)
    {
        ArgumentNullException.ThrowIfNull(criterio);

        return new CriterioEvaluacionDto(
            criterio.Id,
            criterio.Nombre,
            criterio.Descripcion,
            criterio.PesoPorcentual,
            criterio.PuntajeMinimo,
            criterio.PuntajeMaximo,
            criterio.Orden);
    }
}
