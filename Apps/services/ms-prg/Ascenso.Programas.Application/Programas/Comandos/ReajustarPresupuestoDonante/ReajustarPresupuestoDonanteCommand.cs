namespace Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;

public sealed record ReajustarPresupuestoDonanteCommand(
    Guid ProgramaBecaId,
    Guid AsignacionId,
    decimal MontoAReducir,
    Guid UsuarioModificacionId);
