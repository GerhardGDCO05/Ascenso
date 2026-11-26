using Ascenso.Postulaciones.Application.Asignaciones.DTOs;
using Ascenso.Postulaciones.Domain.Consolidacion.Agregados;
using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;
using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public static class RankingMapeador
{
    public const string MotivoRankingNoConfirmado = "RANKING_NO_CONFIRMADO";

    public const string MotivoFueraDeCupoEnListaDeEspera = "FUERA_DE_CUPO_EN_LISTA_DE_ESPERA";

    public static DisponibilidadRankingDto ADto(this DisponibilidadProgramaRanking disponibilidad)
    {
        ArgumentNullException.ThrowIfNull(disponibilidad);

        return new DisponibilidadRankingDto(
            disponibilidad.ProgramaBecaId,
            disponibilidad.CuposAsignadosConvocatoria,
            disponibilidad.CuposDisponiblesPrograma,
            disponibilidad.CuposDisponiblesEfectivos,
            disponibilidad.PresupuestoDisponibleBruto,
            disponibilidad.PresupuestoComprometidoRenovaciones,
            disponibilidad.PresupuestoDisponibleEfectivo,
            disponibilidad.Moneda);
    }

    public static ElementoRankingDto ADto(this ElementoRankingEvaluado elemento)
    {
        ArgumentNullException.ThrowIfNull(elemento);

        return new ElementoRankingDto(
            elemento.ResultadoConsolidadoId,
            elemento.PostulacionId,
            elemento.PosicionRanking,
            elemento.PuntajeConsolidado,
            elemento.MontoEstimadoBeca,
            elemento.Clasificacion.ACodigo(),
            elemento.EmpatadoEnLineaDeCorte,
            elemento.AlertaDiscrepancia,
            elemento.DiscrepanciaRevisada,
            elemento.AfectaAdvertenciaDiscrepanciaEnCorte,
            elemento.ExcepcionEvaluadorUnico,
            elemento.FechaConsolidacion);
    }

    public static RankingConvocatoriaDto ARankingProvisionalDto(
        this CorteRankingConvocatoria corte,
        ClasificacionRanking? filtroClasificacion = null,
        int pagina = 1,
        int tamano = 20)
    {
        ArgumentNullException.ThrowIfNull(corte);

        var filtrados = filtroClasificacion.HasValue
            ? corte.Elementos.Where(e => e.Clasificacion == filtroClasificacion.Value).ToArray()
            : corte.Elementos;

        var itemsPagina = filtrados
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(e => e.ADto())
            .ToArray();

        return new RankingConvocatoriaDto(
            corte.ConvocatoriaId,
            EstadoRanking.Provisional.ACodigo(),
            FechaConfirmacionRanking: null,
            ConfirmadoPorUsuarioId: null,
            corte.Disponibilidad.ADto(),
            corte.TotalPostulantes,
            corte.TotalDentroDeCupo,
            corte.TotalListaDeEspera,
            corte.MontoTotalDentroDeCupo,
            corte.TieneEmpateEnLineaDeCorte,
            corte.PuntajeEmpateEnLineaDeCorte,
            corte.PostulacionesEmpatadasEnCorte,
            corte.TieneAdvertenciaDiscrepanciasEnCorte,
            corte.CantidadDiscrepanciasSinRevisarEnCorte,
            corte.PostulacionesConDiscrepanciaSinRevisarEnCorte,
            new PaginaDto<ElementoRankingDto>(itemsPagina, pagina, tamano, filtrados.Count));
    }

    public static RankingConvocatoriaDto ARankingConfirmadoDto(
        Guid convocatoriaId,
        IReadOnlyList<ResultadoConsolidadoRanking> resultadosConfirmados,
        DisponibilidadProgramaRanking? disponibilidad = null,
        ClasificacionRanking? filtroClasificacion = null,
        int pagina = 1,
        int tamano = 20)
    {
        ArgumentNullException.ThrowIfNull(resultadosConfirmados);

        var ordenados = resultadosConfirmados
            .OrderBy(r => r.PosicionRanking ?? int.MaxValue)
            .ThenBy(r => r.Id)
            .ToArray();

        decimal? puntajePrimerGrupoEspera = null;
        for (var i = 0; i < ordenados.Length; i++)
        {
            if (ordenados[i].Clasificacion == ClasificacionRanking.ListaDeEspera)
            {
                puntajePrimerGrupoEspera = ordenados[i].PuntajeConsolidado;
                break;
            }
        }

        var totalDentroDeCupo = 0;
        var totalListaDeEspera = 0;
        var montoTotalDentroDeCupo = 0.00m;
        var postulacionesConDiscrepanciaEnCorte = new List<Guid>();
        var elementosConfirmados = new List<(ElementoRankingDto Dto, ClasificacionRanking Clasificacion)>(ordenados.Length);

        foreach (var resultado in ordenados)
        {
            if (resultado.EstadoConfirmacionRanking != EstadoRanking.Confirmado
                || !resultado.PosicionRanking.HasValue
                || !resultado.MontoEstimadoBeca.HasValue
                || !resultado.Clasificacion.HasValue)
            {
                throw new InvalidOperationException(
                    "Solo pueden mapearse como ranking confirmado los resultados en estado CONFIRMADO con posición, monto y clasificación.");
            }

            var clasificacion = resultado.Clasificacion.Value;
            if (clasificacion == ClasificacionRanking.DentroDeCupo)
            {
                totalDentroDeCupo++;
                montoTotalDentroDeCupo += resultado.MontoEstimadoBeca.Value;
            }
            else
            {
                totalListaDeEspera++;
            }

            var afectaAdvertencia = resultado.AlertaDiscrepancia
                && !resultado.DiscrepanciaRevisada
                && (clasificacion == ClasificacionRanking.DentroDeCupo
                    || (clasificacion == ClasificacionRanking.ListaDeEspera
                        && puntajePrimerGrupoEspera.HasValue
                        && resultado.PuntajeConsolidado == puntajePrimerGrupoEspera.Value));

            if (afectaAdvertencia)
            {
                postulacionesConDiscrepanciaEnCorte.Add(resultado.PostulacionId);
            }

            var dto = new ElementoRankingDto(
                resultado.Id,
                resultado.PostulacionId,
                resultado.PosicionRanking.Value,
                resultado.PuntajeConsolidado,
                resultado.MontoEstimadoBeca.Value,
                clasificacion.ACodigo(),
                EmpatadoEnLineaDeCorte: false,
                resultado.AlertaDiscrepancia,
                resultado.DiscrepanciaRevisada,
                AfectaAdvertenciaDiscrepanciaEnCorte: afectaAdvertencia,
                resultado.ExcepcionEvaluadorUnico,
                resultado.FechaConsolidacion);

            elementosConfirmados.Add((dto, clasificacion));
        }

        var filtrados = filtroClasificacion.HasValue
            ? elementosConfirmados.Where(e => e.Clasificacion == filtroClasificacion.Value).ToArray()
            : elementosConfirmados.ToArray();

        var itemsPagina = filtrados
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(e => e.Dto)
            .ToArray();

        var fechaConfirmacion = ordenados.Length > 0 ? ordenados[0].FechaConfirmacionRanking : null;
        var confirmadoPor = ordenados.Length > 0 ? ordenados[0].ConfirmadoPorUsuarioId : null;

        return new RankingConvocatoriaDto(
            convocatoriaId,
            EstadoRanking.Confirmado.ACodigo(),
            fechaConfirmacion,
            confirmadoPor,
            disponibilidad?.ADto(),
            ordenados.Length,
            totalDentroDeCupo,
            totalListaDeEspera,
            decimal.Round(montoTotalDentroDeCupo, 2, MidpointRounding.AwayFromZero),
            TieneEmpateEnLineaDeCorte: false,
            PuntajeEmpateEnLineaDeCorte: null,
            PostulacionesEmpatadasEnCorte: [],
            TieneAdvertenciaDiscrepanciasEnCorte: postulacionesConDiscrepanciaEnCorte.Count > 0,
            CantidadDiscrepanciasSinRevisarEnCorte: postulacionesConDiscrepanciaEnCorte.Count,
            PostulacionesConDiscrepanciaSinRevisarEnCorte: postulacionesConDiscrepanciaEnCorte.ToArray(),
            new PaginaDto<ElementoRankingDto>(itemsPagina, pagina, tamano, filtrados.Length));
    }

    public static AdjudicabilidadPostulacionDto AAdjudicabilidadDto(this ResultadoConsolidadoRanking resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        var adjudicable = resultado.EsAdjudicable;
        string? motivoNoAdjudicable = null;

        if (!adjudicable)
        {
            motivoNoAdjudicable = resultado.EstadoConfirmacionRanking == EstadoRanking.Provisional
                ? MotivoRankingNoConfirmado
                : MotivoFueraDeCupoEnListaDeEspera;
        }

        return new AdjudicabilidadPostulacionDto(
            resultado.PostulacionId,
            resultado.ConvocatoriaId,
            adjudicable,
            motivoNoAdjudicable,
            resultado.EstadoConfirmacionRanking.ACodigo(),
            resultado.Clasificacion?.ACodigo(),
            resultado.PosicionRanking,
            resultado.MontoEstimadoBeca,
            resultado.FechaConfirmacionRanking);
    }
}
