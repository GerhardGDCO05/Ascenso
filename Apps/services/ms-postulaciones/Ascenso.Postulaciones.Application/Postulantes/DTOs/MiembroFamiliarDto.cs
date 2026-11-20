namespace Ascenso.Postulaciones.Application.Postulantes.DTOs;

public sealed record class MiembroFamiliarDto(
    Guid Id,
    string Parentesco,
    int Edad,
    string Ocupacion,
    decimal IngresoMensual);
