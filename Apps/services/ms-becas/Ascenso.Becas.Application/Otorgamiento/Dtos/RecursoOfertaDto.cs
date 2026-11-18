namespace Ascenso.Becas.Application.Otorgamiento.Dtos;

/// <summary>
/// Identificadores mínimos de una oferta necesarios para evaluar las políticas
/// de autorización de alcance de programa y propiedad del postulante. No
/// expone datos personales ni información económica.
/// </summary>
public sealed record RecursoOfertaDto(
    Guid OfertaId,
    Guid BecaId,
    Guid ProgramaBecaId,
    Guid PostulanteEstudianteId);
