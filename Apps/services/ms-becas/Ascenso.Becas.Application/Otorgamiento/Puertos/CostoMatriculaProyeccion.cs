namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Proyección local del costo de matrícula vigente para una carrera y periodo.
/// </summary>
public sealed record CostoMatriculaProyeccion(
    Guid CostoMatriculaReferenciaId,
    decimal MontoReferenciaMatricula,
    string Moneda,
    string Estado,
    DateOnly FechaVigenciaDesde,
    DateOnly? FechaVigenciaHasta,
    int NumeroVersion);
