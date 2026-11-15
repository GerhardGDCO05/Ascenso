namespace Ascenso.Postulaciones.Domain.Interfaces.Contratos;

/// <summary>
/// Carrera perteneciente a una universidad aliada.
/// </summary>
public sealed record class CarreraCatalogo(
    Guid Id,
    string Nombre,
    bool Activa);
