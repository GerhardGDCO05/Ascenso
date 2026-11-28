namespace Ascenso.Postulaciones.Application.Consolidacion.DTOs;

public sealed record class AdjudicabilidadPostulacionDto(
    Guid PostulacionId,
    Guid ConvocatoriaOrigenId,
    bool Adjudicable,
    string? MotivoNoAdjudicable,
    string EstadoConfirmacionRanking,
    string? Clasificacion,
    int? PosicionRanking,
    decimal? MontoEstimadoBeca,
    DateTimeOffset? FechaConfirmacionRanking,
    bool PromovidaDeListaEspera = false,
    string? EstadoListaEspera = null,
    int? PosicionListaEspera = null,
    DateTimeOffset? FechaPromocionListaEspera = null,
    Guid? ProgramaBecaId = null);
