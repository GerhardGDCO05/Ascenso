namespace Ascenso.Programas.Application.Programas.Comandos.LiberarReservaPresupuesto;

public sealed record LiberarReservaPresupuestoCommand(
    Guid ProgramaBecaId,
    Guid ReferenciaExternaId,
    string Motivo,
    Guid UsuarioModificacionId);
