using Ascenso.Postulaciones.Domain.Enums;

namespace Ascenso.Postulaciones.Domain.Interfaces.Contratos;

/// <summary>
/// Universidad del catálogo académico junto con sus carreras, usada para
/// validar la elegibilidad del registro del postulante.
/// </summary>
public sealed record class UniversidadAliadaCatalogo(
    Guid Id,
    string Nombre,
    EstadoUniversidadAliada Estado,
    bool TieneConvenioVigente,
    IReadOnlyList<CarreraCatalogo> Carreras);
