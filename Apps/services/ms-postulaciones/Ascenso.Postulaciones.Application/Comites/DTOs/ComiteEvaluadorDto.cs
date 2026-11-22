namespace Ascenso.Postulaciones.Application.Comites.DTOs;

public sealed record class ComiteEvaluadorDto(
    Guid Id,
    Guid ProgramaBecaId,
    Guid ConvocatoriaId,
    string Nombre,
    Guid JefaturaUsuarioId,
    DateOnly FechaConformacion,
    Guid ConformadoPorUsuarioId,
    bool Activo,
    int Version,
    IReadOnlyList<MiembroComiteDto> Miembros);
