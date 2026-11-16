using System.Diagnostics.Metrics;
using Ascenso.Becas.Infrastructure.Metricas;
using FluentAssertions;

namespace Ascenso.Becas.IntegrationTests.Metricas;

public sealed class MetricaConfiguracionFaltanteTests
{
    [Fact]
    public void Debe_RegistrarLaMetricaConLaEtiquetaRecurso()
    {
        var mediciones = new List<(string Recurso, long Valor)>();

        using var escucha = new MeterListener
        {
            InstrumentPublished = (instrumento, listener) =>
            {
                if (instrumento.Meter.Name == MetricaConfiguracionFaltante.NombreMedidor
                    && instrumento.Name == MetricaConfiguracionFaltante.NombreContador)
                {
                    listener.EnableMeasurementEvents(instrumento);
                }
            }
        };

        escucha.SetMeasurementEventCallback<long>((_, medicion, etiquetas, _) =>
        {
            string? recurso = null;

            foreach (var etiqueta in etiquetas)
            {
                if (etiqueta.Key == MetricaConfiguracionFaltante.EtiquetaRecurso)
                {
                    recurso = etiqueta.Value?.ToString();
                }
            }

            lock (mediciones)
            {
                mediciones.Add((recurso ?? string.Empty, medicion));
            }
        });

        escucha.Start();

        var metrica = new MetricaConfiguracionFaltante();
        metrica.Registrar("programa");
        metrica.Registrar("costo-matricula");
        metrica.Registrar("convenio");

        lock (mediciones)
        {
            mediciones.Should().BeEquivalentTo(
            [
                (MetricaConfiguracionFaltante.RecursoPrograma, 1L),
                (MetricaConfiguracionFaltante.RecursoCostoMatricula, 1L),
                (MetricaConfiguracionFaltante.RecursoConvenio, 1L)
            ]);
        }
    }
}
