namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;

public sealed record class ActualizarPerfilSocioeconomicoCommand(
    string EstadoCivil,
    decimal? PromedioActual,
    bool? Trabaja,
    string Ocupacion,
    decimal? IngresoMensualPostulante,
    IReadOnlyList<DatosMiembroFamiliarCommand> MiembrosFamiliares);
