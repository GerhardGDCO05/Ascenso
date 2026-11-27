using Ascenso.Becas.Application.Despacho;
using Ascenso.Becas.Application.Renovacion.Comandos.ConcluirBecaPorVencimiento;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

public sealed class DescubrirConclusionesRenovacionJobTests
{
    [Fact]
    public async Task RecorrePaginasYUsaSoloElPeriodoSiguienteOficial()
    {
        var programa = Guid.CreateVersion7();
        var origen = Guid.CreateVersion7();
        var siguienteId = Guid.CreateVersion7();
        var becas = Enumerable.Range(0, 3).Select(_ =>
            new BecaCandidataConclusion(Guid.CreateVersion7(), programa, origen))
            .ToArray();
        var repositorio = Substitute.For<IBecaRepository>();
        repositorio.ObtenerActivasSinRenovacionAsync(2, null,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BecaCandidataConclusion>>(
                becas[..2]));
        repositorio.ObtenerActivasSinRenovacionAsync(2, becas[1].BecaId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BecaCandidataConclusion>>(
                becas[2..]));
        var periodos = Substitute.For<IPeriodoSiguienteRenovacionPrograma>();
        periodos.ObtenerAsync(programa, origen, Arg.Any<CancellationToken>())
            .Returns(new PeriodoSiguienteRenovacionPrograma(programa, origen,
                siguienteId, new VentanaRenovacionPrograma(
                    new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15),
                    true, false)));
        var despachador = Substitute.For<ICommandDispatcher>();
        despachador.EnviarAsync<ConcluirBecaPorVencimientoCommand, bool>(
                Arg.Any<ConcluirBecaPorVencimientoCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        await CrearJob(repositorio, periodos, despachador, 2).Ejecutar();

        await periodos.Received(1).ObtenerAsync(programa, origen,
            Arg.Any<CancellationToken>());
        foreach (var beca in becas)
            await despachador.Received(1)
                .EnviarAsync<ConcluirBecaPorVencimientoCommand, bool>(
                    Arg.Is<ConcluirBecaPorVencimientoCommand>(valor =>
                        valor.BecaId == beca.BecaId
                        && valor.PeriodoSolicitadoId == siguienteId),
                    Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VentanaInactivaOIntegracionCaidaNoDespachaConclusiones()
    {
        var becaInactiva = new BecaCandidataConclusion(Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7());
        var becaSinServicio = new BecaCandidataConclusion(Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7());
        var repositorio = Substitute.For<IBecaRepository>();
        repositorio.ObtenerActivasSinRenovacionAsync(10, null,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BecaCandidataConclusion>>(
                [becaInactiva, becaSinServicio]));
        var periodos = Substitute.For<IPeriodoSiguienteRenovacionPrograma>();
        periodos.ObtenerAsync(becaInactiva.ProgramaBecaId,
                becaInactiva.PeriodoOrigenId, Arg.Any<CancellationToken>())
            .Returns(new PeriodoSiguienteRenovacionPrograma(
                becaInactiva.ProgramaBecaId, becaInactiva.PeriodoOrigenId,
                Guid.CreateVersion7(), new VentanaRenovacionPrograma(
                    new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15),
                    false, false)));
        periodos.ObtenerAsync(becaSinServicio.ProgramaBecaId,
                becaSinServicio.PeriodoOrigenId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PeriodoSiguienteRenovacionPrograma?>(
                new HttpRequestException("Calendario no disponible")));
        var despachador = Substitute.For<ICommandDispatcher>();

        await CrearJob(repositorio, periodos, despachador, 10).Ejecutar();

        await despachador.DidNotReceiveWithAnyArgs()
            .EnviarAsync<ConcluirBecaPorVencimientoCommand, bool>(default!, default);
    }

    private static DescubrirConclusionesRenovacionJob CrearJob(
        IBecaRepository becas, IPeriodoSiguienteRenovacionPrograma periodos,
        ICommandDispatcher despachador, int limite)
    {
        var reloj = Substitute.For<IClock>();
        reloj.UtcNow.Returns(new DateTimeOffset(2026, 3, 16, 12, 0, 0,
            TimeSpan.Zero));
        return new DescubrirConclusionesRenovacionJob(becas, periodos,
            despachador, reloj,
            Options.Create(new OpcionesTrabajosHangfire
            {
                LimiteRenovacionesPorReconciliacion = limite
            }), NullLogger<DescubrirConclusionesRenovacionJob>.Instance);
    }
}

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class BecasCandidatasConclusionPersistenciaTests
{
    private readonly PostgreSqlFixture _fixture;

    public BecasCandidatasConclusionPersistenciaTests(PostgreSqlFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task ConsultaActivasSinRenovacionConCursorEstable()
    {
        var activa = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        var enRenovacion = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion());
        enRenovacion.IniciarRenovacion(DatosPrueba.CrearContextoOperacion());
        await using (var escritura = _fixture.CrearContexto())
        {
            escritura.Becas.AddRange(activa, enRenovacion);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _fixture.CrearContexto();
        var repositorio = new BecaRepository(lectura);
        var primeraPagina = await repositorio.ObtenerActivasSinRenovacionAsync(1000);
        var despuesDeActiva = await repositorio.ObtenerActivasSinRenovacionAsync(
            1000, activa.Id);

        Assert.Contains(primeraPagina, valor => valor.BecaId == activa.Id
            && valor.ProgramaBecaId == activa.ProgramaBecaId
            && valor.PeriodoOrigenId == activa.PeriodoVigenteActualId);
        Assert.DoesNotContain(primeraPagina, valor =>
            valor.BecaId == enRenovacion.Id);
        Assert.DoesNotContain(despuesDeActiva, valor => valor.BecaId == activa.Id);
        Assert.All(despuesDeActiva, valor =>
            Assert.True(valor.BecaId.CompareTo(activa.Id) > 0));
    }
}
