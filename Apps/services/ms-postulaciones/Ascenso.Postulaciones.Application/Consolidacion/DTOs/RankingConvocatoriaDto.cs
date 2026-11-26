using Ascenso.Postulaciones.Application.Asignaciones.DTOs;

namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class RankingConvocatoriaDto(
    Guid ConvocatoriaId,
    string EstadoRanking,
    DateTimeOffset? FechaConfirmacionRanking,
    Guid? ConfirmadoPorUsuarioId,
    DisponibilidadRankingDto? Disponibilidad,
    int TotalPostulantes,
    int TotalDentroDeCupo,
    int TotalListaDeEspera,
    decimal MontoTotalDentroDeCupo,
    bool TieneEmpateEnLineaDeCorte,
    decimal? PuntajeEmpateEnLineaDeCorte,
    IReadOnlyList<Guid> PostulacionesEmpatadasEnCorte,
    bool TieneAdvertenciaDiscrepanciasEnCorte,
    int CantidadDiscrepanciasSinRevisarEnCorte,
    IReadOnlyList<Guid> PostulacionesConDiscrepanciaSinRevisarEnCorte,
    PaginaDto<ElementoRankingDto> Elementos);
