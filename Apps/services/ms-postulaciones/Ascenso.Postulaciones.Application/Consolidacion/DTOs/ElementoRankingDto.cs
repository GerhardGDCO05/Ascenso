namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class ElementoRankingDto(
    Guid ResultadoConsolidadoId,
    Guid PostulacionId,
    int PosicionRanking,
    decimal PuntajeConsolidado,
    decimal MontoEstimadoBeca,
    string Clasificacion,
    bool EmpatadoEnLineaDeCorte,
    bool AlertaDiscrepancia,
    bool DiscrepanciaRevisada,
    bool AfectaAdvertenciaDiscrepanciaEnCorte,
    bool ExcepcionEvaluadorUnico,
    DateTimeOffset FechaConsolidacion);
