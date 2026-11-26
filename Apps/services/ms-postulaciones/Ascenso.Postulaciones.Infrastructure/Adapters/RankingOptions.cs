namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Opciones de configuración para los adaptadores HTTP de disponibilidad y estimación de monto de beca del ranking.
/// </summary>
public sealed class RankingOptions
{
    public const string Seccion = "Ranking";

    public const string RutaDisponibilidadProgramaPorDefecto = "api/v1/programas/{programaId}/balance";

    public const string RutaDetalleProgramaPorDefecto = "api/v1/programas/{programaId}";

    public const decimal MontoEstimadoBecaReferenciaPorDefectoConstante = 1000.00m;

    public string RutaDisponibilidadPrograma { get; set; } = RutaDisponibilidadProgramaPorDefecto;

    public string RutaDetallePrograma { get; set; } = RutaDetalleProgramaPorDefecto;

    public decimal MontoEstimadoBecaReferenciaPorDefecto { get; set; } = MontoEstimadoBecaReferenciaPorDefectoConstante;
}
