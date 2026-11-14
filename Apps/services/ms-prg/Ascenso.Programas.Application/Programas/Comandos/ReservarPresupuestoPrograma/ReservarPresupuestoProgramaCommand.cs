namespace Ascenso.Programas.Application.Programas.Comandos.ReservarPresupuestoPrograma;

public sealed record ReservarPresupuestoProgramaCommand(
    Guid ProgramaBecaId,
    Guid ReferenciaExternaId,
    string PeriodoAcademicoId,
    decimal Monto,
    string Descripcion,
    Guid UsuarioAutorId);
