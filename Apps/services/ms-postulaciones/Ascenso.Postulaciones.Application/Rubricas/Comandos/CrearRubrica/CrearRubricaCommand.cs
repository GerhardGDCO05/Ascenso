namespace Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;

public sealed record class CrearRubricaCommand(
    Guid ProgramaBecaId,
    Guid ConvocatoriaId,
    string Nombre);
