using Ascenso.Postulaciones.Domain.Asignaciones.Entidades;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Evaluaciones.Agregados;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.Servicios;

/// <summary>
/// Servicio de dominio puro que ejecuta las fórmulas deterministas de consolidación de puntuaciones,
/// detección de discrepancias y aplicación de excepción de evaluador único (DDD V7 §2.3.B, RN-POS-04).
/// </summary>
public static class ServicioCalculoConsolidacion
{
    public static ResultadoCalculoConsolidacion Calcular(
        Guid postulacionId,
        IReadOnlyCollection<Evaluacion> evaluacionesDefinitivas,
        AutorizacionExcepcionEvaluadorUnico? excepcionEvaluadorUnico,
        ParametrosConsolidacion parametros)
    {
        ArgumentNullException.ThrowIfNull(evaluacionesDefinitivas);
        ArgumentNullException.ThrowIfNull(parametros);

        if (postulacionId == Guid.Empty)
        {
            throw new ConsolidacionInvalidaException(
                "El identificador de la postulación es obligatorio.");
        }

        if (excepcionEvaluadorUnico is not null
            && excepcionEvaluadorUnico.PostulacionId != postulacionId)
        {
            throw new ConsolidacionInvalidaException(
                "La excepción de evaluador único no corresponde a la postulación indicada.");
        }

        if (evaluacionesDefinitivas.Count == 0)
        {
            throw new EvaluadoresInsuficientesException(
                "La postulación no cuenta con evaluaciones definitivas para consolidar.");
        }

        var evaluadoresVistos = new HashSet<Guid>(evaluacionesDefinitivas.Count);
        var notas = new List<decimal>(evaluacionesDefinitivas.Count);
        var convocatoriaId = Guid.Empty;

        foreach (var evaluacion in evaluacionesDefinitivas)
        {
            if (evaluacion is null)
            {
                throw new ConsolidacionInvalidaException(
                    "La colección de evaluaciones no puede contener elementos nulos.");
            }

            if (evaluacion.Estado != EstadoEvaluacion.EnviadaDefinitiva)
            {
                throw new ConsolidacionInvalidaException(
                    "Solo pueden consolidarse evaluaciones en estado ENVIADA_DEFINITIVA.");
            }

            if (evaluacion.PostulacionId != postulacionId)
            {
                throw new ConsolidacionInvalidaException(
                    "Todas las evaluaciones a consolidar deben pertenecer a la postulación indicada.");
            }

            if (evaluacion.ConvocatoriaId == Guid.Empty)
            {
                throw new ConsolidacionInvalidaException(
                    "La convocatoria asociada a la evaluación es obligatoria.");
            }

            if (convocatoriaId == Guid.Empty)
            {
                convocatoriaId = evaluacion.ConvocatoriaId;
            }
            else if (evaluacion.ConvocatoriaId != convocatoriaId)
            {
                throw new ConsolidacionInvalidaException(
                    "Todas las evaluaciones a consolidar deben pertenecer a la misma convocatoria.");
            }

            if (!evaluacion.PuntajeTotalPonderado.HasValue)
            {
                throw new ConsolidacionInvalidaException(
                    "Cada evaluación definitiva debe registrar un puntaje total ponderado.");
            }

            var puntaje = evaluacion.PuntajeTotalPonderado.Value;
            if (puntaje < ResultadoCalculoConsolidacion.PuntajeMinimo
                || puntaje > ResultadoCalculoConsolidacion.PuntajeMaximo
                || decimal.Round(puntaje, 2) != puntaje)
            {
                throw new ConsolidacionInvalidaException(
                    "El puntaje total ponderado de la evaluación está fuera de la escala válida [0.00, 100.00].");
            }

            if (evaluacion.EvaluadorId == Guid.Empty || !evaluadoresVistos.Add(evaluacion.EvaluadorId))
            {
                throw new ConsolidacionInvalidaException(
                    "No se permiten evaluaciones duplicadas del mismo evaluador para la misma postulación.");
            }

            notas.Add(puntaje);
        }

        var cantidad = notas.Count;

        if (cantidad >= parametros.EvaluadoresMinimosRequeridos)
        {
            var suma = 0.00m;
            var minimo = notas[0];
            var maximo = notas[0];

            for (var i = 0; i < cantidad; i++)
            {
                var nota = notas[i];
                suma += nota;

                if (nota < minimo)
                {
                    minimo = nota;
                }

                if (nota > maximo)
                {
                    maximo = nota;
                }
            }

            var puntajeConsolidado = decimal.Round(suma / cantidad, 2, MidpointRounding.AwayFromZero);
            var diferencia = decimal.Round(maximo - minimo, 2, MidpointRounding.AwayFromZero);
            var alertaDiscrepancia = diferencia > parametros.UmbralDiscrepanciaPuntos;

            return new ResultadoCalculoConsolidacion(
                convocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: false,
                justificacionEvaluadorUnico: null,
                autorizadorExcepcionId: null,
                cantidadEvaluacionesValidas: cantidad,
                puntajeConsolidado,
                diferencia,
                parametros.UmbralDiscrepanciaPuntos,
                alertaDiscrepancia);
        }

        if (cantidad == 1 && excepcionEvaluadorUnico is not null)
        {
            var puntajeConsolidado = decimal.Round(notas[0], 2, MidpointRounding.AwayFromZero);

            return new ResultadoCalculoConsolidacion(
                convocatoriaId,
                postulacionId,
                excepcionEvaluadorUnico: true,
                justificacionEvaluadorUnico: excepcionEvaluadorUnico.Justificacion,
                autorizadorExcepcionId: excepcionEvaluadorUnico.UsuarioAutorId,
                cantidadEvaluacionesValidas: 1,
                puntajeConsolidado,
                diferenciaEntreEvaluadores: 0.00m,
                umbralDiscrepanciaAplicado: parametros.UmbralDiscrepanciaPuntos,
                alertaDiscrepancia: false);
        }

        throw new EvaluadoresInsuficientesException(
            $"La postulación requiere al menos {parametros.EvaluadoresMinimosRequeridos} evaluaciones definitivas distintas o una excepción de evaluador único autorizada.");
    }
}
