using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Agregados;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class IntencionAceptacionOfertaTests
{
    private readonly PostgreSqlFixture _fixture;

    public IntencionAceptacionOfertaTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RecuperaAceptacionTrasFalloLocalPosteriorAActivacionRemota()
    {
        var datos = DatosPrueba.CrearDatosBeca();
        var contexto = DatosPrueba.CrearContextoOperacion();
        var beca = Beca.CrearOtorgada(datos, contexto);
        var oferta = beca.EmitirOferta(DatosPrueba.CrearDatosOferta(datos), contexto);
        await using (var inicial = _fixture.CrearContexto())
        {
            inicial.Becas.Add(beca);
            await inicial.SaveChangesAsync();
        }

        var fecha = DateOnly.FromDateTime(contexto.Fecha.UtcDateTime);
        var plan = new PlanDesembolsoPrograma(datos.ProgramaBecaId,
            datos.PeriodoOtorgamientoId, FrecuenciaDesembolso.Mensual,
            DestinatarioPago.Becario, fecha, fecha,
            [new CuotaProgramada(1, fecha, 100m)]);
        var intencion = new IntencionAceptacionOferta(oferta.Id, beca.Id,
            datos.ProgramaBecaId, oferta.ReservaPresupuestoId, plan, contexto);
        var programas = Substitute.For<IProgramasClient>();
        var programador = Substitute.For<IProgramadorExpiracionOfertas>();
        var metricas = Substitute.For<IMetricasOtorgamiento>();

        await using (var fallida = _fixture.CrearContexto())
        {
            var store = new IntencionAceptacionOfertaStore(fallida);
            await store.GuardarAsync(intencion);
            var outbox = Substitute.For<IOutboxBeca>();
            outbox.EncolarAsync(Arg.Any<Ascenso.Becas.Domain.Comun.IDomainEvent>(),
                    Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Fallo local")));
            var finalizador = CrearFinalizador(fallida, store, programas,
                outbox, programador, metricas);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                finalizador.ConfirmarAsync(intencion));
        }

        await using var recuperacion = _fixture.CrearContexto();
        var intenciones = new IntencionAceptacionOfertaStore(recuperacion);
        Assert.NotNull(await intenciones.ObtenerAsync(oferta.Id));
        var pendiente = await recuperacion.Becas.Include(item => item.Oferta)
            .SingleAsync(item => item.Id == beca.Id);
        Assert.Equal(EstadoBeca.Otorgada, pendiente.Estado);
        var final = CrearFinalizador(recuperacion, intenciones, programas,
            Substitute.For<IOutboxBeca>(), programador, metricas);
        await final.ConfirmarAsync(intencion);
        await final.ConfirmarAsync(intencion);

        Assert.Null(await intenciones.ObtenerAsync(oferta.Id));
        Assert.Equal(EstadoBeca.Activa, (await recuperacion.Becas
            .SingleAsync(item => item.Id == beca.Id)).Estado);
        Assert.Single(await recuperacion.CalendariosDesembolso
            .Where(item => item.BecaId == beca.Id).ToListAsync());
        await programas.Received(2).ActivarReservaAsync(
            datos.ProgramaBecaId, oferta.ReservaPresupuestoId,
            oferta.Id, Arg.Any<CancellationToken>());
    }

    private static FinalizadorAceptacionOferta CrearFinalizador(
        Ascenso.Becas.Infrastructure.Persistence.BecasDbContext db,
        IIntencionAceptacionOfertaStore intenciones,
        IProgramasClient programas, IOutboxBeca outbox,
        IProgramadorExpiracionOfertas programador,
        IMetricasOtorgamiento metricas)
        => new(new BecaRepository(db), new CalendarioDesembolsoRepository(db),
            intenciones, programas, new UnidadDeTrabajo(db), outbox,
            programador, metricas,
            NullLogger<FinalizadorAceptacionOferta>.Instance);
}
