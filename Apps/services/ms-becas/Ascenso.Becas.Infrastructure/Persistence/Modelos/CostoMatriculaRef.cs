namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>
/// Proyección local de los costos de matrícula versionados por carrera y periodo.
/// </summary>
public sealed class CostoMatriculaRef
{
    public Guid CostoMatriculaReferenciaId { get; set; }

    public Guid CarreraId { get; set; }

    public Guid PeriodoAcademicoId { get; set; }

    public decimal MontoReferenciaMatricula { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public int NumeroVersion { get; set; }

    public DateOnly FechaVigenciaDesde { get; set; }

    public DateOnly? FechaVigenciaHasta { get; set; }

    public string Estado { get; set; } = string.Empty;

    public Guid UltimoEventoId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset FechaUltimaActualizacion { get; set; }
}
