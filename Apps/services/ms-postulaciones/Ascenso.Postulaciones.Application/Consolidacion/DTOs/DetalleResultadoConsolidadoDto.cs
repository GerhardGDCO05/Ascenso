namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class DetalleResultadoConsolidadoDto(
    Guid Id,
    Guid ConvocatoriaId,
    Guid PostulacionId,
    bool ExcepcionEvaluadorUnico,
    string? JustificacionEvaluadorUnico,
    Guid? AutorizadorExcepcionId,
    int CantidadEvaluacionesValidas,
    decimal PuntajeConsolidado,
    decimal DiferenciaEntreEvaluadores,
    decimal UmbralDiscrepanciaAplicado,
    bool AlertaDiscrepancia,
    bool DiscrepanciaRevisada,
    string? ComentarioRevisionDiscrepancia,
    Guid? RevisadoPorUsuarioId,
    DateTimeOffset? FechaRevisionDiscrepancia,
    string EstadoConfirmacionRanking,
    DateTimeOffset FechaConsolidacion,
    int Version,
    IReadOnlyList<NotaEvaluacionIndividualDto> Evaluaciones);
