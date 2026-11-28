namespace Ascenso.Postulaciones.Application.ListaEspera.DTOs;

public sealed record class EntradaListaEsperaDto(
    Guid Id,
    Guid ConvocatoriaId,
    Guid ProgramaBecaId,
    Guid PostulacionId,
    int PosicionOrden,
    decimal MontoEstimadoBecaSnapshot,
    string Estado,
    DateTimeOffset FechaIngreso,
    DateTimeOffset? FechaElegibilidad,
    DateTimeOffset? FechaPromocion,
    Guid? PromovidoPorUsuarioId,
    string? MotivoPromocion,
    int Version);
