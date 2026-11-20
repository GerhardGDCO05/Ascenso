namespace Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;

public sealed record class DatosMiembroFamiliarCommand(
    string Parentesco,
    int? Edad,
    string Ocupacion,
    decimal? IngresoMensual);
