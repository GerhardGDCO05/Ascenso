namespace Ascenso.Postulaciones.Application.Rubricas.DTOs;

public sealed record class RubricaDto(
    Guid Id,
    Guid ProgramaBecaId,
    Guid ConvocatoriaId,
    string Nombre,
    string Estado,
    DateTimeOffset? FechaPublicacion,
    IReadOnlyList<CriterioEvaluacionDto> Criterios,
    decimal SumaPesos,
    decimal DiferenciaPara100,
    string? MotivoBloqueo,
    DateTimeOffset? FechaBloqueo,
    bool SoloLectura);
