namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Identificadores mínimos de un compromiso y su beca para evaluar las
/// políticas de autorización de alcance de programa y propiedad del
/// postulante. No expone datos personales ni información económica.
/// </summary>
public sealed record RecursoCompromisoDto(
    Guid CompromisoId,
    Guid BecaId,
    Guid ProgramaBecaId,
    Guid PostulanteEstudianteId);
