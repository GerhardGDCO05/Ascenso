namespace Ascenso.Postulaciones.Application.Comites.Comandos.DesvincularMiembroComite;

public sealed record class DesvincularMiembroComiteCommand(
    Guid ComiteId,
    Guid MiembroId);
