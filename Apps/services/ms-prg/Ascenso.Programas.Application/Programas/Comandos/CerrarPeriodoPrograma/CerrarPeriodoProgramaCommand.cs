namespace Ascenso.Programas.Application.Programas.Comandos.CerrarPeriodoPrograma;

public sealed record CerrarPeriodoProgramaCommand(
    Guid ProgramaBecaId,
    string PeriodoAcademicoId,
    string Observaciones,
    bool CerrarProgramaDefinitivamente,
    Guid UsuarioModificacionId);
