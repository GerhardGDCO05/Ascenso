using System.Diagnostics.Metrics;
using Ascenso.Becas.Api.Observabilidad;
using Ascenso.Becas.Infrastructure.Metricas;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ascenso.Becas.IntegrationTests.Observabilidad;

[Collection(ColeccionMetricas.Nombre)]
public sealed class MetricasOtorgamientoTests
{
    [Fact]
    public void Debe_IncrementarLosContadores_DeCadaOperacion()
    {
        var mediciones = new List<(string Nombre, long Valor)>();
        using var escucha = CrearEscucha(mediciones);

        var metricas = new MetricasOtorgamiento();
        metricas.OfertaEmitida();
        metricas.OfertaAceptada();
        metricas.OfertaRechazada();
        metricas.OfertaExpirada();

        lock (mediciones)
        {
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreOfertasEmitidas, 1L));
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreOfertasAceptadas, 1L));
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreOfertasRechazadas, 1L));
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreOfertasExpiradas, 1L));
        }
    }

    [Fact]
    public void Debe_PublicarLosGauges_DeCompromisosYOutbox()
    {
        var mediciones = new List<(string Nombre, long Valor)>();
        using var escucha = CrearEscucha(mediciones);

        MetricasOtorgamiento.ActualizarPendientes(37, 12);
        escucha.RecordObservableInstruments();

        lock (mediciones)
        {
            mediciones
                .Where(medicion => medicion.Nombre
                    == MetricasOtorgamiento.NombreCompromisosPendientes)
                .Should().Contain((MetricasOtorgamiento.NombreCompromisosPendientes, 37L));
            mediciones
                .Where(medicion => medicion.Nombre
                    == MetricasOtorgamiento.NombreOutboxPendientes)
                .Should().Contain((MetricasOtorgamiento.NombreOutboxPendientes, 12L));
        }
    }

    [Fact]
    public async Task Debe_ActualizarLosGauges_ConElPublicadorPeriodico()
    {
        var opciones = new OpcionesTelemetria
        {
            MetricasPendientesHabilitadas = true,
            IntervaloMetricasPendientes = TimeSpan.FromMilliseconds(20)
        };
        using var publicador = new PublicadorMetricasPendientes(
            new ContadorPendientesFalso(9, 4),
            opciones,
            NullLogger<PublicadorMetricasPendientes>.Instance);

        var mediciones = new List<(string Nombre, long Valor)>();
        using var escucha = CrearEscucha(mediciones);
        var limite = DateTime.UtcNow.AddSeconds(5);

        await publicador.StartAsync(CancellationToken.None);

        try
        {
            while (DateTime.UtcNow < limite && !Publicados(mediciones))
            {
                escucha.RecordObservableInstruments();
                await Task.Delay(10);
            }
        }
        finally
        {
            await publicador.StopAsync(CancellationToken.None);
        }

        lock (mediciones)
        {
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreCompromisosPendientes, 9L));
            mediciones.Should().Contain(
                (MetricasOtorgamiento.NombreOutboxPendientes, 4L));
        }
    }

    private static bool Publicados(List<(string Nombre, long Valor)> mediciones)
    {
        lock (mediciones)
        {
            return mediciones.Contains(
                    (MetricasOtorgamiento.NombreCompromisosPendientes, 9L))
                && mediciones.Contains(
                    (MetricasOtorgamiento.NombreOutboxPendientes, 4L));
        }
    }

    private static MeterListener CrearEscucha(
        List<(string Nombre, long Valor)> mediciones)
    {
        var escucha = new MeterListener
        {
            InstrumentPublished = (instrumento, listener) =>
            {
                if (instrumento.Meter.Name == MetricasOtorgamiento.NombreMedidor)
                {
                    listener.EnableMeasurementEvents(instrumento);
                }
            }
        };

        escucha.SetMeasurementEventCallback<long>(
            (instrumento, medicion, _, _) =>
            {
                lock (mediciones)
                {
                    mediciones.Add((instrumento.Name, medicion));
                }
            });

        escucha.Start();
        return escucha;
    }

    private sealed class ContadorPendientesFalso
        : IContadorPendientesOtorgamiento
    {
        private readonly long _compromisos;
        private readonly long _outbox;

        public ContadorPendientesFalso(long compromisos, long outbox)
        {
            _compromisos = compromisos;
            _outbox = outbox;
        }

        public Task<ResultadoPendientesOtorgamiento> ContarAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(
                new ResultadoPendientesOtorgamiento(_compromisos, _outbox));
        }
    }
}

/// <summary>
/// Serializa las pruebas que leen o escriben los instrumentos estáticos de
/// <see cref="MetricasOtorgamiento"/>, porque comparten estado en todo el
/// proceso de pruebas.
/// </summary>
[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionMetricas
{
    public const string Nombre = "MetricasOtorgamiento";
}
