using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class PersistenciaTrabajosTests
{
    private readonly PostgreSqlFixture _fixture;

    public PersistenciaTrabajosTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ConservarElJobYExpirarLaOferta_Cuando_SeReiniciaElHost()
    {
        Guid ofertaId;

        await using (var proveedorAnterior = EntornoTrabajos.Crear(
            _fixture,
            conHangfire: true))
        {
            var beca = await EntornoTrabajos.GuardarOfertaPendienteAsync(
                proveedorAnterior,
                DateTimeOffset.UtcNow.AddDays(-16),
                "job-sobrevive-al-reinicio");
            ofertaId = beca.Oferta!.Id;

            await using var alcance = proveedorAnterior.CreateAsyncScope();
            var programador = alcance.ServiceProvider
                .GetRequiredService<IProgramadorExpiracionOfertas>();
            var referencia = await programador.ProgramarAsync(
                ofertaId,
                beca.Oferta.FechaVencimientoAceptacion);

            referencia.Should().NotBeNullOrWhiteSpace();
        }

        var constructor = Host.CreateApplicationBuilder();
        constructor.Logging.ClearProviders();
        EntornoTrabajos.ConfigurarServicios(
            constructor.Services,
            _fixture.CadenaConexion,
            conHangfire: true,
            conServidorHangfire: true);

        using var host = constructor.Build();
        await host.StartAsync();

        try
        {
            await EsperarExpiracionAsync(host.Services, ofertaId);

            var beca = await EntornoTrabajos.ObtenerPorOfertaAsync(
                host.Services,
                ofertaId);

            beca.Should().NotBeNull();
            beca!.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);
            beca.Estado.Should().Be(EstadoBeca.Revocada);
            beca.TipoCierre.Should().Be(TipoCierreBeca.OfertaExpirada);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static async Task EsperarExpiracionAsync(
        IServiceProvider servicios,
        Guid ofertaId)
    {
        var limite = DateTimeOffset.UtcNow.AddSeconds(20);

        while (DateTimeOffset.UtcNow < limite)
        {
            var beca = await EntornoTrabajos.ObtenerPorOfertaAsync(
                servicios,
                ofertaId);

            if (beca?.Oferta?.Estado == EstadoOferta.Expirada)
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException(
            "La oferta no expiró después de reiniciar el host con Hangfire.");
    }
}
