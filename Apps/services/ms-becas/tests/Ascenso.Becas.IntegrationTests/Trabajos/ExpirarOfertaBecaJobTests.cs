using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ExpirarOfertaBecaJobTests
{
    private readonly PostgreSqlFixture _fixture;

    public ExpirarOfertaBecaJobTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_ExpirarLaOfertaPendienteYNoLaAceptada_Cuando_SeEjecutaElJob()
    {
        await using var proveedor = EntornoTrabajos.Crear(_fixture);
        var fechaOperacion = DateTimeOffset.UtcNow.AddDays(-16);
        var pendiente = await EntornoTrabajos.GuardarOfertaPendienteAsync(
            proveedor,
            fechaOperacion);
        var aceptada = await EntornoTrabajos.GuardarBecaActivaAsync(
            proveedor,
            fechaOperacion);

        await EjecutarAsync(proveedor, pendiente.Oferta!.Id);
        await EjecutarAsync(proveedor, aceptada.Oferta!.Id);

        var pendienteGuardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            pendiente.Oferta.Id);
        var aceptadaGuardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            aceptada.Oferta.Id);

        pendienteGuardada.Should().NotBeNull();
        pendienteGuardada!.Estado.Should().Be(EstadoBeca.Revocada);
        pendienteGuardada.TipoCierre.Should().Be(TipoCierreBeca.OfertaExpirada);
        pendienteGuardada.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);

        aceptadaGuardada.Should().NotBeNull();
        aceptadaGuardada!.Estado.Should().Be(EstadoBeca.Activa);
        aceptadaGuardada.Oferta!.Estado.Should().Be(EstadoOferta.Aceptada);

        var outbox = proveedor.GetRequiredService<OutboxBecaTrabajosFalso>();
        outbox.Eventos.OfType<OfertaBecaExpiradaEvento>()
            .Should().ContainSingle(evento => evento.OfertaId == pendiente.Oferta.Id);
    }

    [Fact]
    public async Task Debe_SerIdempotente_Cuando_ElJobSeEjecutaDosVeces()
    {
        await using var proveedor = EntornoTrabajos.Crear(_fixture);
        var pendiente = await EntornoTrabajos.GuardarOfertaPendienteAsync(
            proveedor,
            DateTimeOffset.UtcNow.AddDays(-16));
        var ofertaId = pendiente.Oferta!.Id;

        await EjecutarAsync(proveedor, ofertaId);
        await EjecutarAsync(proveedor, ofertaId);

        var guardada = await EntornoTrabajos.ObtenerPorOfertaAsync(
            proveedor,
            ofertaId);

        guardada!.Oferta!.Estado.Should().Be(EstadoOferta.Expirada);
        proveedor.GetRequiredService<OutboxBecaTrabajosFalso>().Eventos
            .OfType<OfertaBecaExpiradaEvento>()
            .Should().ContainSingle();
    }

    private static async Task EjecutarAsync(IServiceProvider proveedor, Guid ofertaId)
    {
        await using var alcance = proveedor.CreateAsyncScope();
        var job = alcance.ServiceProvider.GetRequiredService<ExpirarOfertaBecaJob>();

        await job.Ejecutar(ofertaId);
    }
}
