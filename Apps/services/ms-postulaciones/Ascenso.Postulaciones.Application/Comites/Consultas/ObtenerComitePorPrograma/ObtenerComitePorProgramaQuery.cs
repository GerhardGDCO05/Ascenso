namespace Ascenso.Postulaciones.Application.Comites.Consultas.ObtenerComitePorPrograma;

public sealed record class ObtenerComitePorProgramaQuery(
    Guid ProgramaBecaId,
    Guid ConvocatoriaId);
