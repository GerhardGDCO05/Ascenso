namespace Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;

public sealed record ItemAsignacionDonanteCommand(
    Guid DonanteId,
    string PeriodoAcademicoId,
    decimal MontoAsignado);

public sealed record AsignarPresupuestoDonantesCommand(
    Guid ProgramaBecaId,
    IReadOnlyList<ItemAsignacionDonanteCommand> Asignaciones,
    Guid AutorizadoPorUsuarioId);
