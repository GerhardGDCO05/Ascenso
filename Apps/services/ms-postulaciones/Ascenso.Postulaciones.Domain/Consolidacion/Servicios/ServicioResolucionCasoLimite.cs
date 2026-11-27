using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;
using Ascenso.Postulaciones.Domain.Excepciones;

namespace Ascenso.Postulaciones.Domain.Consolidacion.Servicios;

/// <summary>
/// Servicio de dominio puro que valida y aplica atómicamente una resolución manual
/// de empate en frontera o caso límite presupuestario sobre el grupo afectado.
/// </summary>
public static class ServicioResolucionCasoLimite
{
    public static IReadOnlyList<ResultadoConsolidadoRanking> AplicarResolucion(
        CorteRankingConvocatoria corte,
        IReadOnlyCollection<ResultadoConsolidadoRanking> resultados,
        IReadOnlyCollection<Guid> postulacionesSeleccionadasIds,
        string justificacion,
        DateTimeOffset fechaDecision,
        Guid usuarioDecisorId)
    {
        ArgumentNullException.ThrowIfNull(corte);
        ArgumentNullException.ThrowIfNull(resultados);
        ArgumentNullException.ThrowIfNull(postulacionesSeleccionadasIds);

        var casoLimite = corte.CasoLimiteActivo
            ?? throw new CasoLimiteNoDetectadoException();

        if (postulacionesSeleccionadasIds.Count == 0)
        {
            throw new SeleccionCasoLimiteInvalidaException(
                "Debe seleccionar al menos una postulación del grupo afectado por el caso límite.");
        }

        var seleccionadas = new HashSet<Guid>(postulacionesSeleccionadasIds.Count);
        foreach (var postulacionId in postulacionesSeleccionadasIds)
        {
            if (postulacionId == Guid.Empty)
            {
                throw new SeleccionCasoLimiteInvalidaException(
                    "Las postulaciones seleccionadas no pueden contener identificadores vacíos.");
            }

            if (!seleccionadas.Add(postulacionId))
            {
                throw new SeleccionCasoLimiteInvalidaException(
                    "Las postulaciones seleccionadas no pueden contener duplicados.");
            }
        }

        var afectadasSet = new HashSet<Guid>(casoLimite.PostulacionesAfectadas);
        foreach (var postulacionId in seleccionadas)
        {
            if (!afectadasSet.Contains(postulacionId))
            {
                throw new SeleccionCasoLimiteInvalidaException(
                    "Todas las postulaciones seleccionadas deben pertenecer al grupo afectado por el caso límite activo.");
            }
        }

        var resultadosPorPostulacion = new Dictionary<Guid, ResultadoConsolidadoRanking>(resultados.Count);
        foreach (var resultado in resultados)
        {
            if (resultado is null)
            {
                throw new CasoLimiteInvalidoException(
                    "La colección de resultados consolidados no puede contener elementos nulos.");
            }

            resultadosPorPostulacion[resultado.PostulacionId] = resultado;
        }

        var resultadosAfectados = new List<ResultadoConsolidadoRanking>(casoLimite.PostulacionesAfectadas.Count);
        foreach (var postulacionId in casoLimite.PostulacionesAfectadas)
        {
            if (!resultadosPorPostulacion.TryGetValue(postulacionId, out var resultadoAfectado))
            {
                throw new CasoLimiteInvalidoException(
                    "No se encontraron todos los resultados consolidados correspondientes al grupo afectado.");
            }

            if (resultadoAfectado.EstadoConfirmacionRanking == EstadoRanking.Confirmado)
            {
                throw new RankingYaConfirmadoException();
            }

            if (resultadoAfectado.TieneDecisionCasoLimite)
            {
                throw new CasoLimiteYaResueltoException();
            }

            resultadosAfectados.Add(resultadoAfectado);
        }

        if (seleccionadas.Count > casoLimite.CuposDisponibles)
        {
            throw new SeleccionCasoLimiteExcedeDisponibilidadException(
                "La cantidad de postulaciones seleccionadas excede los cupos efectivos disponibles para el caso límite.");
        }

        var elementosPorPostulacion = corte.Elementos.ToDictionary(e => e.PostulacionId);
        var montoSeleccionadas = 0.00m;
        foreach (var postulacionId in seleccionadas)
        {
            if (!elementosPorPostulacion.TryGetValue(postulacionId, out var elemento))
            {
                throw new CasoLimiteInvalidoException(
                    "No se encontró el monto estimado de una de las postulaciones seleccionadas en el corte.");
            }

            montoSeleccionadas += elemento.MontoEstimadoBeca;
        }

        montoSeleccionadas = decimal.Round(montoSeleccionadas, 2, MidpointRounding.AwayFromZero);
        if (montoSeleccionadas > casoLimite.PresupuestoDisponible)
        {
            throw new SeleccionCasoLimiteExcedeDisponibilidadException(
                "El monto total de las postulaciones seleccionadas excede el presupuesto efectivo disponible para el caso límite.");
        }

        if (fechaDecision == default)
        {
            throw new ArgumentException("La fecha de operación es obligatoria.", nameof(fechaDecision));
        }

        if (usuarioDecisorId == Guid.Empty)
        {
            throw new ArgumentException("El usuario actor es obligatorio.", nameof(usuarioDecisorId));
        }

        if (string.IsNullOrWhiteSpace(justificacion))
        {
            throw new JustificacionCasoLimiteInvalidaException();
        }

        var justificacionNormalizada = justificacion.Trim();
        if (justificacionNormalizada.Length < ResultadoConsolidadoRanking.LongitudMinimaJustificacionCasoLimite
            || justificacionNormalizada.Length > ResultadoConsolidadoRanking.LongitudMaximaJustificacionCasoLimite)
        {
            throw new JustificacionCasoLimiteInvalidaException();
        }

        foreach (var resultadoAfectado in resultadosAfectados)
        {
            resultadoAfectado.RegistrarDecisionCasoLimite(
                casoLimite.Tipo,
                seleccionado: seleccionadas.Contains(resultadoAfectado.PostulacionId),
                justificacionNormalizada,
                fechaDecision,
                usuarioDecisorId);
        }

        return Array.AsReadOnly(resultadosAfectados.ToArray());
    }
}
