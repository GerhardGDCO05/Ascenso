using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.Servicios;

/// <summary>
/// Servicio de dominio puro que ordena el ranking por convocatoria, traza la línea de corte por cupo y presupuesto,
/// detecta empates reales en frontera (RN-POS-07) y señala alertas de discrepancia sin revisar en o adyacentes al corte.
/// </summary>
public static class ServicioCalculoRankingCorte
{
    public static CorteRankingConvocatoria Calcular(
        Guid convocatoriaId,
        IReadOnlyCollection<ResultadoConsolidadoRanking> resultados,
        IReadOnlyDictionary<Guid, decimal> montosEstimadosPorPostulacion,
        DisponibilidadProgramaRanking disponibilidad)
    {
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(montosEstimadosPorPostulacion);
        ArgumentNullException.ThrowIfNull(disponibilidad);

        if (convocatoriaId == Guid.Empty)
        {
            throw new RankingInvalidoException("El identificador de la convocatoria es obligatorio.");
        }

        if (disponibilidad.ConvocatoriaId != convocatoriaId)
        {
            throw new RankingInvalidoException(
                "La disponibilidad de cupos y presupuesto no corresponde a la convocatoria indicada.");
        }

        var postulacionesVistas = new HashSet<Guid>(resultados.Count);
        var entradasValidas = new List<(ResultadoConsolidadoRanking Resultado, decimal Monto)>(resultados.Count);

        foreach (var resultado in resultados)
        {
            if (resultado is null)
            {
                throw new RankingInvalidoException("La colección de resultados consolidados no puede contener nulos.");
            }

            if (resultado.ConvocatoriaId != convocatoriaId)
            {
                throw new RankingInvalidoException(
                    "Todos los resultados consolidados deben pertenecer a la convocatoria indicada.");
            }

            if (!postulacionesVistas.Add(resultado.PostulacionId))
            {
                throw new RankingInvalidoException(
                    "No se permiten resultados consolidados duplicados para la misma postulación.");
            }

            if (!montosEstimadosPorPostulacion.TryGetValue(resultado.PostulacionId, out var monto)
                || monto <= 0.00m
                || decimal.Round(monto, 2) != monto)
            {
                throw new RankingInvalidoException(
                    "Cada postulación del ranking debe contar con un monto estimado de beca mayor a 0.00 con máximo dos decimales.");
            }

            entradasValidas.Add((resultado, monto));
        }

        if (entradasValidas.Count == 0)
        {
            return new CorteRankingConvocatoria(convocatoriaId, disponibilidad, []);
        }

        var ordenados = entradasValidas
            .OrderByDescending(e => e.Resultado.PuntajeConsolidado)
            .ThenBy(e => e.Resultado.FechaConsolidacion)
            .ThenBy(e => e.Resultado.Id)
            .ToArray();

        var clasificaciones = new ClasificacionRanking[ordenados.Length];
        var empatadosEnCorte = new bool[ordenados.Length];

        var cuposRestantes = disponibilidad.CuposDisponiblesEfectivos;
        var presupuestoRestante = disponibilidad.PresupuestoDisponibleEfectivo;
        var corteAlcanzado = false;

        var indiceInicioGrupo = 0;
        while (indiceInicioGrupo < ordenados.Length)
        {
            var puntajeGrupo = ordenados[indiceInicioGrupo].Resultado.PuntajeConsolidado;
            var indiceFinGrupo = indiceInicioGrupo + 1;

            while (indiceFinGrupo < ordenados.Length
                && ordenados[indiceFinGrupo].Resultado.PuntajeConsolidado == puntajeGrupo)
            {
                indiceFinGrupo++;
            }

            var tamanoGrupo = indiceFinGrupo - indiceInicioGrupo;

            if (corteAlcanzado)
            {
                for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
                {
                    clasificaciones[i] = ClasificacionRanking.ListaDeEspera;
                    empatadosEnCorte[i] = false;
                }
            }
            else
            {
                var sumaGrupo = 0.00m;
                var minimoGrupo = ordenados[indiceInicioGrupo].Monto;

                for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
                {
                    var monto = ordenados[i].Monto;
                    sumaGrupo += monto;
                    if (monto < minimoGrupo)
                    {
                        minimoGrupo = monto;
                    }
                }

                sumaGrupo = decimal.Round(sumaGrupo, 2, MidpointRounding.AwayFromZero);

                if (tamanoGrupo <= cuposRestantes && sumaGrupo <= presupuestoRestante)
                {
                    for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
                    {
                        clasificaciones[i] = ClasificacionRanking.DentroDeCupo;
                        empatadosEnCorte[i] = false;
                    }

                    cuposRestantes -= tamanoGrupo;
                    presupuestoRestante = decimal.Round(
                        presupuestoRestante - sumaGrupo,
                        2,
                        MidpointRounding.AwayFromZero);
                }
                else if (cuposRestantes == 0 || minimoGrupo > presupuestoRestante)
                {
                    corteAlcanzado = true;
                    for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
                    {
                        clasificaciones[i] = ClasificacionRanking.ListaDeEspera;
                        empatadosEnCorte[i] = false;
                    }
                }
                else
                {
                    corteAlcanzado = true;
                    for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
                    {
                        empatadosEnCorte[i] = true;
                        var monto = ordenados[i].Monto;

                        if (cuposRestantes > 0 && monto <= presupuestoRestante)
                        {
                            clasificaciones[i] = ClasificacionRanking.DentroDeCupo;
                            cuposRestantes--;
                            presupuestoRestante = decimal.Round(
                                presupuestoRestante - monto,
                                2,
                                MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            clasificaciones[i] = ClasificacionRanking.ListaDeEspera;
                        }
                    }
                }
            }

            indiceInicioGrupo = indiceFinGrupo;
        }

        decimal? puntajePrimerGrupoEspera = null;
        for (var i = 0; i < ordenados.Length; i++)
        {
            if (clasificaciones[i] == ClasificacionRanking.ListaDeEspera)
            {
                puntajePrimerGrupoEspera = ordenados[i].Resultado.PuntajeConsolidado;
                break;
            }
        }

        var elementos = new ElementoRankingEvaluado[ordenados.Length];
        for (var i = 0; i < ordenados.Length; i++)
        {
            var (resultado, monto) = ordenados[i];
            var clasificacion = clasificaciones[i];
            var empatado = empatadosEnCorte[i];

            var afectaAdvertencia = resultado.AlertaDiscrepancia
                && !resultado.DiscrepanciaRevisada
                && (clasificacion == ClasificacionRanking.DentroDeCupo
                    || empatado
                    || (clasificacion == ClasificacionRanking.ListaDeEspera
                        && puntajePrimerGrupoEspera.HasValue
                        && resultado.PuntajeConsolidado == puntajePrimerGrupoEspera.Value));

            elementos[i] = new ElementoRankingEvaluado(
                resultado.Id,
                resultado.PostulacionId,
                posicionRanking: i + 1,
                puntajeConsolidado: resultado.PuntajeConsolidado,
                montoEstimadoBeca: monto,
                clasificacion: clasificacion,
                empatadoEnLineaDeCorte: empatado,
                alertaDiscrepancia: resultado.AlertaDiscrepancia,
                discrepanciaRevisada: resultado.DiscrepanciaRevisada,
                afectaAdvertenciaDiscrepanciaEnCorte: afectaAdvertencia,
                excepcionEvaluadorUnico: resultado.ExcepcionEvaluadorUnico,
                fechaConsolidacion: resultado.FechaConsolidacion);
        }

        return new CorteRankingConvocatoria(convocatoriaId, disponibilidad, elementos);
    }
}
