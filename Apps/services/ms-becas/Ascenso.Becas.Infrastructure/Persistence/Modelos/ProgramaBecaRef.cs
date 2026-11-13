using Ascenso.Becas.Domain.ValueObjects;

namespace Ascenso.Becas.Infrastructure.Persistence.Modelos;

/// <summary>
/// Proyección local de la configuración vigente de un programa de becas.
/// </summary>
public sealed class ProgramaBecaRef
{
    public Guid ProgramaBecaId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public PoliticaCobertura PoliticaCobertura { get; set; } = null!;

    public CondicionesPermanencia CondicionesPermanencia { get; set; } = null!;

    public bool PermiteAcumulacion { get; set; }

    public bool RequiereCompromisoFirmado { get; set; }

    public int DuracionPeriodos { get; set; }

    public string Estado { get; set; } = string.Empty;

    public Guid UltimoEventoId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset FechaUltimaActualizacion { get; set; }
}
