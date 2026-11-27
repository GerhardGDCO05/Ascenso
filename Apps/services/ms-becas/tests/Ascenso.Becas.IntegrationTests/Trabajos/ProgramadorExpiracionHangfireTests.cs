using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ProgramadorExpiracionHangfireTests
{
    private readonly PostgreSqlFixture _fixture;

    public ProgramadorExpiracionHangfireTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ProgramarElJobEnLaColaCritica_Cuando_LaFechaEsFutura()
    {
        await using var proveedor = EntornoTrabajos.Crear(
            _fixture,
            conHangfire: true);
        var programador = proveedor.GetRequiredService<IProgramadorExpiracionOfertas>();
        var ofertaId = Guid.CreateVersion7();

        var referencia = await programador.ProgramarAsync(
            ofertaId,
            DateTimeOffset.UtcNow.AddDays(1));

        referencia.Should().NotBeNullOrWhiteSpace();

        var almacenamiento = proveedor.GetRequiredService<JobStorage>();
        var detalles = almacenamiento.GetMonitoringApi().JobDetails(referencia);

        detalles.Should().NotBeNull();
        detalles!.Job.Type.Should().Be<ExpirarOfertaBecaJob>();
        detalles.History.First().StateName.Should().Be("Scheduled");

        var metodo = typeof(ExpirarOfertaBecaJob)
            .GetMethod(nameof(ExpirarOfertaBecaJob.Ejecutar))!;
        metodo.GetCustomAttributes(typeof(QueueAttribute), inherit: false)
            .Cast<QueueAttribute>()
            .Should().ContainSingle()
            .Which.Queue.Should().Be(RegistroHangfire.ColaCritica);

        var cancelado = await programador.CancelarAsync(referencia);

        cancelado.Should().BeTrue();
        almacenamiento.GetMonitoringApi().JobDetails(referencia)!
            .History.First().StateName.Should().Be("Deleted");
    }

    [Fact]
    public async Task Debe_EncolarDeInmediato_Cuando_LaFechaYaVencio()
    {
        await using var proveedor = EntornoTrabajos.Crear(
            _fixture,
            conHangfire: true);
        var programador = proveedor.GetRequiredService<IProgramadorExpiracionOfertas>();
        var ofertaId = Guid.CreateVersion7();

        var referencia = await programador.ProgramarAsync(
            ofertaId,
            DateTimeOffset.UtcNow.AddMinutes(-1));

        referencia.Should().NotBeNullOrWhiteSpace();

        var almacenamiento = proveedor.GetRequiredService<JobStorage>();
        var detalles = almacenamiento.GetMonitoringApi().JobDetails(referencia);

        detalles.Should().NotBeNull();
        detalles!.History.First().StateName.Should().Be("Enqueued");
    }
}
