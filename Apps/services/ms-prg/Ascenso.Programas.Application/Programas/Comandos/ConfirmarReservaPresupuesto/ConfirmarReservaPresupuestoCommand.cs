namespace Ascenso.Programas.Application.Programas.Comandos.ConfirmarReservaPresupuesto;

public sealed record ConfirmarReservaPresupuestoCommand(
    Guid ProgramaBecaId,
    Guid ReferenciaExternaId,
    string? Descripcion,
    Guid UsuarioModificacionId);
