using System.Diagnostics.Metrics;
using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Infrastructure.Metricas;

/// <summary>
/// Métrica de configuración faltante expuesta con
/// <see cref="System.Diagnostics.Metrics"/>. El contador
/// <c>becas.configuracion_faltante</c> lleva la etiqueta <c>recurso</c> con los
/// valores <c>programa</c>, <c>costo_matricula</c> o <c>convenio</c>.
/// </summary>
public sealed class MetricaConfiguracionFaltante : IMetricaConfiguracionFaltante
{
    public const string NombreMedidor = "Ascenso.Becas";

    public const string NombreContador = "becas.configuracion_faltante";

    public const string EtiquetaRecurso = "recurso";

    public const string RecursoPrograma = IMetricaConfiguracionFaltante.RecursoPrograma;

    public const string RecursoCostoMatricula = IMetricaConfiguracionFaltante.RecursoCostoMatricula;

    public const string RecursoConvenio = IMetricaConfiguracionFaltante.RecursoConvenio;

    private static readonly Meter _medidor = new(NombreMedidor, "1.0.0");

    private static readonly Counter<long> _contador = _medidor.CreateCounter<long>(
        NombreContador,
        unit: "{emision}",
        description:
            "Emisiones bloqueadas porque la configuración local de MS-PRG no está disponible o no es vigente.");

    public void Registrar(string recurso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recurso);

        _contador.Add(
            1,
            new KeyValuePair<string, object?>(
                EtiquetaRecurso,
                Normalizar(recurso)));
    }

    private static string Normalizar(string recurso)
        => recurso.Trim().ToLowerInvariant() switch
        {
            "costo" or "costo-matricula" or "costomatricula" => RecursoCostoMatricula,
            "convenio" => RecursoConvenio,
            _ => RecursoPrograma
        };
}
