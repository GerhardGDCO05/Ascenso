namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Proyección local del convenio con una universidad aliada.
/// </summary>
public sealed record ConvenioProyeccion(
    Guid ConvenioId,
    string Estado,
    DateOnly FechaFin);
