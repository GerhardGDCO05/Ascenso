namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>
/// Proyección local del estado del convenio con una universidad aliada.
/// </summary>
public sealed class ConvenioRef
{
    public Guid UniversidadAliadaId { get; set; }

    public Guid ConvenioId { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateOnly FechaFin { get; set; }

    public Guid UltimoEventoId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset FechaUltimaActualizacion { get; set; }
}
