using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.Servicios;

/// <summary>
/// Servicio de dominio puro que ordena el ranking por convocatoria, traza la línea de corte por cupo y presupuesto,
/// detecta empates reales en frontera (RN-POS-07) y casos límite presupuestarios, aplica decisiones registradas
/// y señala alertas de discrepancia sin revisar en o adyacentes al corte.
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
        var casosLimiteResueltos = new bool[ordenados.Length];
        CasoLimiteRankingDetectado? casoLimiteActivo = null;
        var tieneDecisionInvalida = false;

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
                    if (ordenados[i].Resultado.SeleccionadoEnCasoLimite == true)
                    {
                        tieneDecisionInvalida = true;
                    }
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
                        casosLimiteResueltos[i] = ordenados[i].Resultado.TieneDecisionCasoLimite;
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
                        if (ordenados[i].Resultado.SeleccionadoEnCasoLimite == true)
                        {
                            tieneDecisionInvalida = true;
                        }
                    }

                    if (cuposRestantes >= 1
                        && presupuestoRestante > 0.00m
                        && minimoGrupo > presupuestoRestante
                        && indiceFinGrupo < ordenados.Length)
                    {
                        EvaluarCasoLimitePresupuestarioPosterior(
                            ordenados,
                            indiceFinGrupo,
                            cuposRestantes,
                            presupuestoRestante,
                            clasificaciones,
                            empatadosEnCorte,
                            casosLimiteResueltos,
                            ref casoLimiteActivo,
                            ref tieneDecisionInvalida);

                        break;
                    }
                }
                else
                {
                    corteAlcanzado = true;
                    EvaluarEmpateEnFrontera(
                        ordenados,
                        indiceInicioGrupo,
                        indiceFinGrupo,
                        puntajeGrupo,
                        ref cuposRestantes,
                        ref presupuestoRestante,
                        clasificaciones,
                        empatadosEnCorte,
                        casosLimiteResueltos,
                        ref casoLimiteActivo,
                        ref tieneDecisionInvalida);
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
                fechaConsolidacion: resultado.FechaConsolidacion,
                tipoCasoLimite: resultado.TipoCasoLimite,
                seleccionadoEnCasoLimite: resultado.SeleccionadoEnCasoLimite,
                justificacionDesempateCasoLimite: resultado.JustificacionDesempateCasoLimite,
                decididoPorUsuarioId: resultado.DecididoPorUsuarioId,
                fechaDecisionCasoLimite: resultado.FechaDecisionCasoLimite,
                casoLimiteResuelto: casosLimiteResueltos[i]);
        }

        return new CorteRankingConvocatoria(
            convocatoriaId,
            disponibilidad,
            elementos,
            casoLimiteActivo,
            tieneDecisionInvalida);
    }

    private static void EvaluarEmpateEnFrontera(
        (ResultadoConsolidadoRanking Resultado, decimal Monto)[] ordenados,
        int indiceInicioGrupo,
        int indiceFinGrupo,
        decimal puntajeGrupo,
        ref int cuposRestantes,
        ref decimal presupuestoRestante,
        ClasificacionRanking[] clasificaciones,
        bool[] empatadosEnCorte,
        bool[] casosLimiteResueltos,
        ref CasoLimiteRankingDetectado? casoLimiteActivo,
        ref bool tieneDecisionInvalida)
    {
        var tamanoGrupo = indiceFinGrupo - indiceInicioGrupo;
        var tipoEmpate = tamanoGrupo > cuposRestantes
            ? TipoCasoLimite.EmpatePorCupo
            : TipoCasoLimite.EmpatePorPresupuesto;

        var todosConDecision = true;
        var cantidadSeleccionados = 0;
        var sumaSeleccionados = 0.00m;

        for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
        {
            var resultado = ordenados[i].Resultado;
            if (!resultado.TieneDecisionCasoLimite)
            {
                todosConDecision = false;
            }
            else if (resultado.SeleccionadoEnCasoLimite == true)
            {
                cantidadSeleccionados++;
                sumaSeleccionados += ordenados[i].Monto;
            }
        }

        sumaSeleccionados = decimal.Round(sumaSeleccionados, 2, MidpointRounding.AwayFromZero);

        var resolucionValida = todosConDecision
            && cantidadSeleccionados >= 1
            && cantidadSeleccionados <= cuposRestantes
            && sumaSeleccionados <= presupuestoRestante;

        if (resolucionValida)
        {
            var ganadores = new List<(ResultadoConsolidadoRanking Resultado, decimal Monto)>(cantidadSeleccionados);
            var noSeleccionados = new List<(ResultadoConsolidadoRanking Resultado, decimal Monto)>(
                tamanoGrupo - cantidadSeleccionados);

            for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
            {
                if (ordenados[i].Resultado.SeleccionadoEnCasoLimite == true)
                {
                    ganadores.Add(ordenados[i]);
                }
                else
                {
                    noSeleccionados.Add(ordenados[i]);
                }
            }

            var cursor = indiceInicioGrupo;
            for (var i = 0; i < ganadores.Count; i++)
            {
                ordenados[cursor] = ganadores[i];
                clasificaciones[cursor] = ClasificacionRanking.DentroDeCupo;
                empatadosEnCorte[cursor] = false;
                casosLimiteResueltos[cursor] = true;
                cursor++;
            }

            for (var i = 0; i < noSeleccionados.Count; i++)
            {
                ordenados[cursor] = noSeleccionados[i];
                clasificaciones[cursor] = ClasificacionRanking.ListaDeEspera;
                empatadosEnCorte[cursor] = false;
                casosLimiteResueltos[cursor] = true;
                cursor++;
            }

            cuposRestantes -= cantidadSeleccionados;
            presupuestoRestante = decimal.Round(
                presupuestoRestante - sumaSeleccionados,
                2,
                MidpointRounding.AwayFromZero);
            return;
        }

        if (cantidadSeleccionados > 0
            && (cantidadSeleccionados > cuposRestantes || sumaSeleccionados > presupuestoRestante))
        {
            tieneDecisionInvalida = true;
        }

        var cuposParaCaso = cuposRestantes;
        var presupuestoParaCaso = presupuestoRestante;
        var postulacionesGrupo = new Guid[tamanoGrupo];

        for (var i = indiceInicioGrupo; i < indiceFinGrupo; i++)
        {
            postulacionesGrupo[i - indiceInicioGrupo] = ordenados[i].Resultado.PostulacionId;
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

        casoLimiteActivo = new CasoLimiteRankingDetectado(
            tipoEmpate,
            puntajeGrupo,
            cuposParaCaso,
            presupuestoParaCaso,
            postulacionesGrupo);
    }

    private static void EvaluarCasoLimitePresupuestarioPosterior(
        (ResultadoConsolidadoRanking Resultado, decimal Monto)[] ordenados,
        int indiceInicioPosteriores,
        int cuposRestantes,
        decimal presupuestoRestante,
        ClasificacionRanking[] clasificaciones,
        bool[] empatadosEnCorte,
        bool[] casosLimiteResueltos,
        ref CasoLimiteRankingDetectado? casoLimiteActivo,
        ref bool tieneDecisionInvalida)
    {
        var existenDecisionesPresupuestarias = false;
        var cantidadSeleccionados = 0;
        var sumaSeleccionados = 0.00m;

        for (var j = indiceInicioPosteriores; j < ordenados.Length; j++)
        {
            var resultado = ordenados[j].Resultado;
            if (resultado.TipoCasoLimite == TipoCasoLimite.CasoLimitePresupuestario)
            {
                existenDecisionesPresupuestarias = true;
                if (resultado.SeleccionadoEnCasoLimite == true)
                {
                    cantidadSeleccionados++;
                    sumaSeleccionados += ordenados[j].Monto;
                }
            }
        }

        sumaSeleccionados = decimal.Round(sumaSeleccionados, 2, MidpointRounding.AwayFromZero);

        if (existenDecisionesPresupuestarias)
        {
            var resolucionValida = cantidadSeleccionados >= 1
                && cantidadSeleccionados <= cuposRestantes
                && sumaSeleccionados <= presupuestoRestante;

            if (resolucionValida)
            {
                for (var j = indiceInicioPosteriores; j < ordenados.Length; j++)
                {
                    var resultado = ordenados[j].Resultado;
                    empatadosEnCorte[j] = false;

                    if (resultado.TipoCasoLimite == TipoCasoLimite.CasoLimitePresupuestario)
                    {
                        casosLimiteResueltos[j] = true;
                        clasificaciones[j] = resultado.SeleccionadoEnCasoLimite == true
                            ? ClasificacionRanking.DentroDeCupo
                            : ClasificacionRanking.ListaDeEspera;
                    }
                    else
                    {
                        clasificaciones[j] = ClasificacionRanking.ListaDeEspera;
                        if (resultado.SeleccionadoEnCasoLimite == true)
                        {
                            tieneDecisionInvalida = true;
                        }
                    }
                }
            }
            else
            {
                tieneDecisionInvalida = true;
                for (var j = indiceInicioPosteriores; j < ordenados.Length; j++)
                {
                    clasificaciones[j] = ClasificacionRanking.ListaDeEspera;
                    empatadosEnCorte[j] = false;
                    casosLimiteResueltos[j] = false;
                }
            }

            return;
        }

        var elegibles = new List<Guid>();
        for (var j = indiceInicioPosteriores; j < ordenados.Length; j++)
        {
            clasificaciones[j] = ClasificacionRanking.ListaDeEspera;
            empatadosEnCorte[j] = false;

            if (ordenados[j].Resultado.SeleccionadoEnCasoLimite == true)
            {
                tieneDecisionInvalida = true;
            }

            if (ordenados[j].Monto <= presupuestoRestante)
            {
                elegibles.Add(ordenados[j].Resultado.PostulacionId);
            }
        }

        if (elegibles.Count > 0)
        {
            casoLimiteActivo = new CasoLimiteRankingDetectado(
                TipoCasoLimite.CasoLimitePresupuestario,
                puntajeAfectado: null,
                cuposDisponibles: cuposRestantes,
                presupuestoDisponible: presupuestoRestante,
                postulacionesAfectadas: elegibles);
        }
    }
}
