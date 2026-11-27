using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Otorgamiento.Servicios;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Ascenso.Becas.Infrastructure.Trabajos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class IntencionEmisionOfertaTests
{
    private readonly PostgreSqlFixture _fixture;

    public IntencionEmisionOfertaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task RecuperaMismaOfertaTrasRollbackDeTransaccionAmbiental()
    {
        var datos = DatosPrueba.CrearDatosBeca();
        var contexto = DatosPrueba.CrearContextoOperacion();
        var intencion = new IntencionEmisionOferta(
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            datos.PostulacionOrigenId, datos,
            DatosPrueba.CrearDatosOferta(datos), contexto);

        await using (var fallida = _fixture.CrearContexto())
        {
            await using var transaccion = await fallida.Database.BeginTransactionAsync();
            await new IntencionEmisionOfertaStore(fallida).GuardarAsync(intencion);
            await transaccion.RollbackAsync();
        }

        await using var recuperacion = _fixture.CrearContexto();
        var store = new IntencionEmisionOfertaStore(recuperacion);
        var guardada = await store.ObtenerAsync(intencion.OfertaId);
        Assert.NotNull(guardada);
        Assert.Equal(intencion.PostulacionId, guardada.PostulacionId);
        Assert.Equal(intencion.DatosOferta.UrlCartaPdfFirebase,
            guardada.DatosOferta.UrlCartaPdfFirebase);

        var postulaciones = Substitute.For<IPostulacionesClient>();
        var programas = Substitute.For<IProgramasClient>();
        var outbox = Substitute.For<IOutboxBeca>();
        var finalizador = new FinalizadorEmisionOferta(postulaciones, programas,
            store,
            new BecaRepository(recuperacion), new UnidadDeTrabajo(recuperacion),
            outbox);

        var job = new ReconciliarEmisionesOfertaJob(store, finalizador,
            Options.Create(new OpcionesTrabajosHangfire()),
            NullLogger<ReconciliarEmisionesOfertaJob>.Instance);
        await job.Ejecutar();
        await job.Ejecutar();

        Assert.NotNull(await recuperacion.Becas.Include(beca => beca.Oferta)
            .SingleOrDefaultAsync(beca => beca.Id == intencion.BecaId));
        Assert.Null(await store.ObtenerAsync(intencion.OfertaId));
        await postulaciones.Received(1).OtorgarAsync(intencion.PostulacionId,
            intencion.OfertaId, Arg.Any<CancellationToken>());
        await programas.Received(1).ConfirmarReservaAsync(
            intencion.DatosBeca.ProgramaBecaId,
            intencion.DatosOferta.ReservaPresupuestoId,
            intencion.OfertaId, Arg.Any<CancellationToken>());
        await outbox.Received(1).EncolarAsync(Arg.Any<Ascenso.Becas.Domain.Comun.IDomainEvent>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
