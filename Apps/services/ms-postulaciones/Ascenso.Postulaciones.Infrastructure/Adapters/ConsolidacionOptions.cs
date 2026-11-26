using Ascenso.Postulaciones.Domain.Consolidacion.ObjetosValor;

namespace Ascenso.Postulaciones.Infrastructure.Adapters;

/// <summary>
/// Opciones de configuración para los parámetros de consolidación de postulaciones.
/// </summary>
public sealed class ConsolidacionOptions
{
    public const string Seccion = "Consolidacion";

    public int EvaluadoresMinimosRequeridos { get; set; } =
        ParametrosConsolidacion.EvaluadoresMinimosPorDefecto;

    public decimal UmbralDiscrepanciaPuntos { get; set; } =
        ParametrosConsolidacion.UmbralDiscrepanciaPorDefecto;
}
