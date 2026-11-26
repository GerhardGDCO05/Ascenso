using Ascenso.Becas.Application.Comun;
using Ascenso.Becas.Application.Excepciones;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.Eventos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Domain.Renovacion.Agregados;
using Ascenso.Becas.Domain.Renovacion.Datos;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Persistencia;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class ConcluirBecaPorVencimientoTests
{
    private readonly PostgreSqlFixture _fixture;

    public ConcluirBecaPorVencimientoTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task ConcluyeSoloDespuesDelPlazoSinSolicitudYLiberacionConfirmada()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(beca);
            await inicio.SaveChangesAsync();
        }

        var periodoNuevo = Guid.CreateVersion7();
        var ventanas = Ventanas(beca.ProgramaBecaId, periodoNuevo, false);
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        cupos.LiberarPorConclusionAsync(beca.ProgramaBecaId, beca.Id,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(true));
        await using var contexto = _fixture.CrearContexto();
        var reloj = Reloj(new DateTimeOffset(2026, 3, 16, 12, 0, 0,
            TimeSpan.Zero));
        var outbox = Substitute.For<IOutboxBeca>();
        var handler = CrearHandler(contexto, ventanas, cupos, reloj, outbox);

        Assert.True(await handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
            beca.Id, periodoNuevo)));
        Assert.False(await handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
            beca.Id, periodoNuevo)));

        await using var lectura = _fixture.CrearContexto();
        var reconstruida = await lectura.Becas.SingleAsync(valor => valor.Id == beca.Id);
        Assert.Equal(EstadoBeca.Concluida, reconstruida.Estado);
        Assert.False(reconstruida.HabilitadaParaDesembolso);
        Assert.False(await lectura.SolicitudesRenovacion.AnyAsync(valor =>
            valor.BecaId == beca.Id));
        Assert.False(await lectura.Becas.AnyAsync(valor =>
            valor.BecaAnteriorId == beca.Id));
        await cupos.Received(1).LiberarPorConclusionAsync(
            beca.ProgramaBecaId, beca.Id, beca.PeriodoVigenteActualId,
            Arg.Any<CancellationToken>());
        await outbox.Received(1).EncolarAsync(
            Arg.Is<BecaConcluidaPorVencimientoRenovacionEvento>(evento =>
                evento.BecaId == beca.Id), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 16)]
    public async Task ConservaBecaSiNoVencioOElProgramaPermiteRenovacionTardia(
        bool permiteTardia, int diaActual)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(beca);
            await inicio.SaveChangesAsync();
        }

        var periodoNuevo = Guid.CreateVersion7();
        var ventanas = Ventanas(beca.ProgramaBecaId, periodoNuevo, permiteTardia);
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        await using var contexto = _fixture.CrearContexto();
        var handler = CrearHandler(contexto, ventanas, cupos,
            Reloj(new DateTimeOffset(2026, 3, diaActual, 12, 0, 0,
                TimeSpan.Zero)));

        Assert.False(await handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
            beca.Id, periodoNuevo)));
        Assert.Equal(EstadoBeca.Activa, (await contexto.Becas.SingleAsync(valor =>
            valor.Id == beca.Id)).Estado);
        await cupos.DidNotReceiveWithAnyArgs().LiberarPorConclusionAsync(
            default, default, default);
    }

    [Fact]
    public async Task FallaCerradoSiMsPrgNoConfirmaLiberacion()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(beca);
            await inicio.SaveChangesAsync();
        }

        var periodoNuevo = Guid.CreateVersion7();
        var ventanas = Ventanas(beca.ProgramaBecaId, periodoNuevo, false);
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        cupos.LiberarPorConclusionAsync(beca.ProgramaBecaId, beca.Id,
            beca.PeriodoVigenteActualId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<bool?>(null));
        await using var contexto = _fixture.CrearContexto();
        var handler = CrearHandler(contexto, ventanas, cupos,
            Reloj(new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<DependenciaNoDisponibleException>(() =>
            handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
                beca.Id, periodoNuevo)));
        await using var lectura = _fixture.CrearContexto();
        Assert.Equal(EstadoBeca.Activa, (await lectura.Becas.SingleAsync(valor =>
            valor.Id == beca.Id)).Estado);
    }

    [Fact]
    public async Task FallaCerradoSiNoSeConoceLaExcepcionDeRenovacionTardia()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(beca);
            await inicio.SaveChangesAsync();
        }

        var periodoNuevo = Guid.CreateVersion7();
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        await using var contexto = _fixture.CrearContexto();
        var handler = CrearHandler(contexto,
            Ventanas(beca.ProgramaBecaId, periodoNuevo, null), cupos,
            Reloj(new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<ConfiguracionProgramaNoDisponibleException>(() =>
            handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
                beca.Id, periodoNuevo)));
        await cupos.DidNotReceiveWithAnyArgs().LiberarPorConclusionAsync(
            default, default, default);
        Assert.Equal(EstadoBeca.Activa, (await contexto.Becas.SingleAsync(valor =>
            valor.Id == beca.Id)).Estado);
    }

    [Fact]
    public async Task NoConcluyeSiExisteUnaSolicitud()
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var periodoNuevo = Guid.CreateVersion7();
        var solicitud = SolicitudRenovacion.Solicitar(new DatosSolicitudRenovacion(
            beca.Id, beca.PeriodoVigenteActualId, periodoNuevo,
            new DateOnly(2026, 3, 15), true, true),
            DatosPrueba.CrearContextoOperacion());
        await using (var inicio = _fixture.CrearContexto())
        {
            inicio.Becas.Add(beca);
            inicio.SolicitudesRenovacion.Add(solicitud);
            await inicio.SaveChangesAsync();
        }

        var ventanas = Ventanas(beca.ProgramaBecaId, periodoNuevo, false);
        var cupos = Substitute.For<ILiberacionCupoPrograma>();
        await using var contexto = _fixture.CrearContexto();
        var handler = CrearHandler(contexto, ventanas, cupos,
            Reloj(new DateTimeOffset(2026, 3, 16, 12, 0, 0, TimeSpan.Zero)));

        Assert.False(await handler.HandleAsync(new ConcluirBecaPorVencimientoCommand(
            beca.Id, periodoNuevo)));
        Assert.Equal(EstadoBeca.Activa, (await contexto.Becas.SingleAsync(valor =>
            valor.Id == beca.Id)).Estado);
        await cupos.DidNotReceiveWithAnyArgs().LiberarPorConclusionAsync(
            default, default, default);
    }

    private static IVentanaRenovacionPrograma Ventanas(Guid programaId,
        Guid periodoId, bool? permiteTardia)
    {
        var ventanas = Substitute.For<IVentanaRenovacionPrograma>();
        ventanas.ObtenerAsync(programaId, periodoId, Arg.Any<CancellationToken>())
            .Returns(new VentanaRenovacionPrograma(new DateOnly(2026, 3, 1),
                new DateOnly(2026, 3, 15), false, permiteTardia));
        return ventanas;
    }

    private static IClock Reloj(DateTimeOffset fecha)
    {
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(fecha);
        return reloj;
    }

    private static ConcluirBecaPorVencimientoHandler CrearHandler(
        BecasDbContext contexto,
        IVentanaRenovacionPrograma ventanas,
        ILiberacionCupoPrograma cupos, IClock reloj,
        IOutboxBeca? outbox = null)
    {
        var contextoActual = Substitute.For<IContextoOperacionActual>();
        contextoActual.CorrelationId.Returns(Guid.CreateVersion7().ToString());
        return new ConcluirBecaPorVencimientoHandler(
            new BecaRepository(contexto), new SolicitudRenovacionRepository(contexto),
            ventanas, cupos, new UnidadDeTrabajo(contexto), contextoActual, reloj,
            outbox ?? Substitute.For<IOutboxBeca>());
    }
}
