namespace Ascenso.Postulaciones.Application.Comites.Comandos.CrearComiteEvaluador;

public sealed record class CrearComiteEvaluadorCommand(
    Guid ProgramaBecaId,
    Guid ConvocatoriaId,
    string Nombre,
    Guid JefaturaUsuarioId);
