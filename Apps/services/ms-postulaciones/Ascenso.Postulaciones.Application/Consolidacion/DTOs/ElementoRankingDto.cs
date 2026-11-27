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
    DateTimeOffset FechaConsolidacion,
    string? TipoCasoLimite = null,
    bool? SeleccionadoEnCasoLimite = null,
    string? JustificacionDesempateCasoLimite = null,
    Guid? DecididoPorUsuarioId = null,
    DateTimeOffset? FechaDecisionCasoLimite = null,
    bool CasoLimiteResuelto = false);
