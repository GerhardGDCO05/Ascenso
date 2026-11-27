using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Desembolsos.GenerarCalendarioInicial;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Desembolsos.Datos;
using Ascenso.Becas.Domain.Desembolsos.Eventos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class GenerarCalendarioInicialTests
{
    private readonly PostgreSqlFixture _fixture;

    public GenerarCalendarioInicialTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task ReconciliaBecaActivaUnaSolaVezConPlanOficial()
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var planes = PlanesPara(beca.ProgramaBecaId, beca.PeriodoVigenteActualId);
        var outbox = Substitute.For<IOutboxBeca>();
        var handler = CrearHandler(db, planes, outbox);

        Assert.True(await handler.HandleAsync(new GenerarCalendarioInicialCommand(beca.Id)));
        db.ChangeTracker.Clear();
        Assert.False(await handler.HandleAsync(new GenerarCalendarioInicialCommand(beca.Id)));

        var calendario = await db.CalendariosDesembolso.SingleAsync(item =>
            item.BecaId == beca.Id && item.PeriodoAcademicoId == beca.PeriodoVigenteActualId);
        Assert.Equal(beca.Oferta!.MontoOfertaPorPeriodo,
            calendario.MontoMaximoAprobadoPeriodo);
        Assert.Equal(100m, calendario.SumaCuotasProgramadas);
        await outbox.Received(1).EncolarAsync(
            Arg.Any<DesembolsoProgramadoEvento>(),
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SinPlanOficialConservaLaBecaActivaSinCalendario()
    {
        await using var db = _fixture.CrearContexto();
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        db.Becas.Add(beca);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var planes = Substitute.For<IPlanDesembolsoPrograma>();
        var handler = CrearHandler(db, planes, Substitute.For<IOutboxBeca>());

        await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(() =>
            handler.HandleAsync(new GenerarCalendarioInicialCommand(beca.Id)));

        db.ChangeTracker.Clear();
        Assert.Equal(EstadoBeca.Activa,
            (await db.Becas.SingleAsync(item => item.Id == beca.Id)).Estado);
        Assert.False(await db.CalendariosDesembolso.AnyAsync(item =>
            item.BecaId == beca.Id));
    }

    private static IPlanDesembolsoPrograma PlanesPara(Guid programaId, Guid periodoId)
    {
        var planes = Substitute.For<IPlanDesembolsoPrograma>();
        var fecha = new DateOnly(2026, 9, 1);
        planes.ObtenerAsync(programaId, periodoId, Arg.Any<CancellationToken>())
            .Returns(new PlanDesembolsoPrograma(programaId, periodoId,
                FrecuenciaDesembolso.Mensual, DestinatarioPago.Becario,
                fecha, fecha, [new CuotaProgramada(1, fecha, 100m)]));
        return planes;
    }

    private static GenerarCalendarioInicialHandler CrearHandler(
        Infrastructure.Persistence.BecasDbContext db,
        IPlanDesembolsoPrograma planes, IOutboxBeca outbox)
    {
        var contexto = Substitute.For<IContextoOperacionActual>();
        contexto.UsuarioId.Returns(Guid.CreateVersion7());
        contexto.CorrelationId.Returns(Guid.CreateVersion7().ToString("D"));
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(DatosPrueba.FechaBase);
        return new GenerarCalendarioInicialHandler(new BecaRepository(db),
            new CalendarioDesembolsoRepository(db), planes,
            new UnidadDeTrabajo(db), contexto, reloj, outbox);
    }
}
