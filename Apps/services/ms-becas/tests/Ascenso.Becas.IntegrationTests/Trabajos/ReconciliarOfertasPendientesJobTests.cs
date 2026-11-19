using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ReconciliarOfertasPendientesJobTests
{
    private readonly PostgreSqlFixture _fixture;

    public ReconciliarOfertasPendientesJobTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ExpirarLasOfertasVencidasSinJob_Cuando_SeEjecutaLaReconciliacion()
    {
        await using var proveedor = EntornoTrabajos.Crear(_fixture);
        var fechaVencida = DateTimeOffset.UtcNow.AddDays(-16);
        var sinJob = await EntornoTrabajos.GuardarOfertaPendienteAsync(
            proveedor,
            fechaVencida,
            "job-no-programado");
        var jobPerdido = await EntornoTrabajos.GuardarOfertaPendienteAsync(
            proveedor,
            fechaVencida,
            "job-eliminado-de-hangfire");
        var vigente = await EntornoTrabajos.GuardarOfertaPendienteAsync(
            proveedor,
            DateTimeOffset.UtcNow);

        await EjecutarAsync(proveedor);
        await EjecutarAsync(proveedor);

        var sinJobGuardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            sinJob.Oferta!.Id);
        var jobPerdidoGuardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            jobPerdido.Oferta!.Id);
        var vigenteGuardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            vigente.Oferta!.Id);

        sinJobGuardada!.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);
        sinJobGuardada.Estado.Should().Be(EstadoBeca.Revocada);
        jobPerdidoGuardada!.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);
        jobPerdidoGuardada.Estado.Should().Be(EstadoBeca.Revocada);
        vigenteGuardada!.Oferta!.Estado.Should().Be(EstadoOferta.PendienteRespuesta);
        vigenteGuardada.Estado.Should().Be(EstadoBeca.Otorgada);

        proveedor.GetRequiredService<OutboxBecaTrabajosFalso>().Eventos
            .OfType<OfertaBecaExpiradaEvento>()
            .Where(evento => evento.OfertaId == sinJob.Oferta!.Id
                || evento.OfertaId == jobPerdido.Oferta!.Id)
            .Should()
            .HaveCount(2);
    }

    private static async Task EjecutarAsync(IServiceProvider proveedor)
    {
        await using var alcance = proveedor.CreateAsyncScope();
        var job = alcance.ServiceProvider
            .GetRequiredService<ReconciliarOfertasPendientesJob>();

        await job.Ejecutar();
    }
}
