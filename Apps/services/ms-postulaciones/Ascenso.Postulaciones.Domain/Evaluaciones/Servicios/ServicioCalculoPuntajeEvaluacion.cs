using Ascenso.Postulaciones.Domain.Evaluaciones.Entidades;
using Ascenso.Postulaciones.Domain.Excepciones;
using Ascenso.Postulaciones.Domain.Rubricas.Agregados;

namespace Ascenso.Postulaciones.Domain.Evaluaciones.Servicios;

/// <summary>
/// Servicio de dominio puro que calcula el puntaje total ponderado de una evaluación
/// sobre una rúbrica de evaluación en escala normalizada [0.00, 100.00].
/// </summary>
public static class ServicioCalculoPuntajeEvaluacion
{
    public const int DecimalesResultado = 2;

    public static decimal CalcularPuntajeTotalPonderado(
        RubricaEvaluacion rubrica,
        IReadOnlyCollection<CalificacionCriterio> calificaciones)
    {
        ArgumentNullException.ThrowIfNull(rubrica);
        ArgumentNullException.ThrowIfNull(calificaciones);

        if (rubrica.Criterios.Count == 0)
        {
            throw new EvaluacionInvalidaException(
                "La rúbrica de evaluación no contiene criterios configurados.");
        }

        var criteriosPorId = rubrica.Criterios.ToDictionary(criterio => criterio.Id);
        var calificacionesPorCriterioId = new Dictionary<Guid, CalificacionCriterio>(calificaciones.Count);

        foreach (var calificacion in calificaciones)
        {
            if (calificacion is null)
            {
                throw new EvaluacionInvalidaException(
                    "Las calificaciones de criterios no pueden contener elementos nulos.");
            }

            if (!criteriosPorId.ContainsKey(calificacion.CriterioEvaluacionId))
            {
                throw new CriterioNoPerteneceARubricaException();
            }

            if (!calificacionesPorCriterioId.TryAdd(calificacion.CriterioEvaluacionId, calificacion))
            {
                throw new EvaluacionInvalidaException(
                    "Existen calificaciones duplicadas para un mismo criterio de evaluación.");
            }
        }

        var criteriosFaltantes = criteriosPorId.Count - calificacionesPorCriterioId.Count;
        if (criteriosFaltantes > 0)
        {
            throw new EvaluacionIncompletaException(criteriosFaltantes);
        }

        var sumaPonderada = 0m;

        foreach (var criterio in rubrica.Criterios)
        {
            var calificacion = calificacionesPorCriterioId[criterio.Id];

            if (calificacion.PuntajeAsignado < criterio.PuntajeMinimo
                || calificacion.PuntajeAsignado > criterio.PuntajeMaximo)
            {
                throw new PuntajeFueraDeRangoException(
                    criterio.Nombre,
                    criterio.PuntajeMinimo,
                    criterio.PuntajeMaximo,
                    calificacion.PuntajeAsignado);
            }

            var puntajeNormalizado =
                (calificacion.PuntajeAsignado - criterio.PuntajeMinimo)
                / (criterio.PuntajeMaximo - criterio.PuntajeMinimo)
                * 100m;

            sumaPonderada += puntajeNormalizado * (criterio.PesoPorcentual / 100m);
        }

        return decimal.Round(sumaPonderada, DecimalesResultado, MidpointRounding.AwayFromZero);
    }
}
