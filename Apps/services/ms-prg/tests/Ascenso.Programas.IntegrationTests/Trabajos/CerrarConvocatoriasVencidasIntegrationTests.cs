using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Convocatorias.Comandos.CerrarConvocatoriasVencidas;
using Ascenso.Programas.Application.Convocatorias.EventosIntegracion;
using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Convocatorias.Agregados;
using Ascenso.Programas.Domain.Convocatorias.ObjetosValor;
using Ascenso.Programas.Domain.Enums;
using Ascenso.Programas.Domain.Programas.Agregados;
using Ascenso.Programas.Domain.Programas.ObjetosValor;
using Ascenso.Programas.Infrastructure.Persistence;
using Ascenso.Programas.Infrastructure.Persistence.Repositorios;
using Ascenso.Programas.Infrastructure.Trabajos;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Ascenso.Programas.IntegrationTests.Trabajos;

public sealed class CerrarConvocatoriasVencidasIntegrationTests : IAsyncLifetime
{
    private static readonly Guid _usuarioId = Guid.Parse("01966000-0000-7000-8000-000000000701");
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _postgres.DisposeAsync();

    [Fact]
    public async Task Debe_CerrarSoloLasConvocatoriasVencidasYNoDuplicarEventosAlRepetirElJob()
    {
        var ahora = new DateTimeOffset(2027, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var programa = ProgramaBeca.Crear(
            "BECA-JOB-TEST",
            "Programa de prueba para cierre",
            "Programa usado por la prueba del cierre automático.",
            new PoliticaCobertura(ModalidadCobertura.Total, 100.00m, 0.00m, "USD"),
            10,
            8,
            "2027-I",
            new DateOnly(2020, 1, 1),
            new DateOnly(2035, 12, 31),
            false,
            true,
            null,
            _usuarioId);
        var vencida = CrearConvocatoriaPublicada(
            programa.Id,
            "JOB-VENCIDA",
            ahora,
            ahora.AddHours(-1));
        var futura = CrearConvocatoriaPublicada(
            programa.Id,
            "JOB-FUTURA",
            ahora,
            ahora.AddDays(5));

        await using (var contextoInicial = CrearContexto())
        {
            await contextoInicial.ProgramasBeca.AddAsync(programa);
            await contextoInicial.Convocatorias.AddRangeAsync(vencida, futura);
            await contextoInicial.GuardarCambiosAsync();
        }

        var publicador = Substitute.For<IIntegrationEventPublisher>();
        publicador
            .PublicarAsync(Arg.Any<ConvocatoriaCerrada>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using (var contextoTrabajo = CrearContexto())
        {
            var manejador = new CerrarConvocatoriasVencidasCommandHandler(
                new ConvocatoriaRepository(contextoTrabajo),
                contextoTrabajo,
                new RelojConvocatoriasFijo(ahora),
                publicador);
            var trabajo = new CerrarConvocatoriasVencidasJob(manejador);

            var cerradasPrimeraEjecucion = await trabajo.EjecutarAsync();
            var cerradasSegundaEjecucion = await trabajo.EjecutarAsync();

            cerradasPrimeraEjecucion.Should().Be(1);
            cerradasSegundaEjecucion.Should().Be(0);
        }

        await using var contextoVerificacion = CrearContexto();
        var convocatoriaVencida = await contextoVerificacion.Convocatorias
            .SingleAsync(convocatoria => convocatoria.Id == vencida.Id);
        var convocatoriaFutura = await contextoVerificacion.Convocatorias
            .SingleAsync(convocatoria => convocatoria.Id == futura.Id);

        convocatoriaVencida.Estado.Should().Be(EstadoConvocatoria.Cerrada);
        convocatoriaFutura.Estado.Should().Be(EstadoConvocatoria.Publicada);
        await publicador.Received(1).PublicarAsync(
            Arg.Is<ConvocatoriaCerrada>(evento => evento.ConvocatoriaId == vencida.Id),
            Arg.Any<CancellationToken>());
    }

    private ProgramasDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ProgramasDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    ProgramasDbContext.Esquema))
            .Options;

        return new ProgramasDbContext(opciones);
    }

    private static Convocatoria CrearConvocatoriaPublicada(
        Guid programaId,
        string codigo,
        DateTimeOffset ahora,
        DateTimeOffset fechaCierre)
    {
        var fechaApertura = fechaCierre.AddDays(-20);
        var fechaSubsanacion = fechaCierre.AddDays(1);
        var fechaInicioEvaluacion = fechaSubsanacion;
        var fechaCierreEvaluacion = fechaSubsanacion.AddDays(10);
        var convocatoria = Convocatoria.CrearBorrador(
            programaId,
            Guid.NewGuid().ToString("D"),
            codigo,
            $"Convocatoria {codigo}",
            fechaApertura,
            fechaCierre,
            fechaSubsanacion,
            fechaInicioEvaluacion,
            fechaCierreEvaluacion,
            true,
            3,
            new CriteriosElegibilidadCiclo(
                15.00m,
                17,
                35,
                1200.00m,
                [Guid.NewGuid()],
                [Guid.NewGuid()]),
            _usuarioId);

        convocatoria.AgregarDocumento(
            "IDENTIDAD",
            "Documento de identidad",
            null,
            true,
            ["application/pdf"],
            5,
            _usuarioId);
        convocatoria.Publicar(ahora.AddDays(-2), _usuarioId);

        return convocatoria;
    }

    private sealed class RelojConvocatoriasFijo(DateTimeOffset ahora) : IRelojConvocatorias
    {
        public DateTimeOffset Ahora { get; } = ahora;
    }
}
