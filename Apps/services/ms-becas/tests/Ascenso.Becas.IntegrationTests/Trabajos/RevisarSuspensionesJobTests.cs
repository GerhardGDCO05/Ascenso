using Ascenso.Becas.Domain.Enums;
using Ascenso.Becas.Domain.Otorgamiento.ValueObjects;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Trabajos;
using Ascenso.Becas.IntegrationTests.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ascenso.Becas.IntegrationTests.Trabajos;

[Collection(ColeccionPostgreSql.Nombre)]
public sealed class RevisarSuspensionesJobTests
{
    private static readonly DateTimeOffset _fecha =
        new(2026, 11, 25, 13, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlFixture _fixture;

    public RevisarSuspensionesJobTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task AlertaVencidasEIndefinidasSinReactivarBecas()
    {
        await using var db = _fixture.CrearContexto();
        var vencida = CrearSuspendida(new DateOnly(2026, 11, 20), false);
        var indefinida = CrearSuspendida(null, true);
        var futura = CrearSuspendida(new DateOnly(2026, 12, 1), false);
        db.Becas.AddRange(vencida, indefinida, futura);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var alertas = new LoggerAlertas();
        var job = new RevisarSuspensionesJob(db, new RelojFijo(),
            Options.Create(new OpcionesTrabajosHangfire
            {
                LimiteSuspensionesPorPagina = 1
            }), alertas);

        await job.Ejecutar();

        Assert.Contains(alertas.Mensajes, mensaje => mensaje.Contains(vencida.Id.ToString(),
            StringComparison.Ordinal));
        Assert.Contains(alertas.Mensajes, mensaje => mensaje.Contains(indefinida.Id.ToString(),
            StringComparison.Ordinal));
        Assert.DoesNotContain(alertas.Mensajes, mensaje => mensaje.Contains(futura.Id.ToString(),
            StringComparison.Ordinal));
        Assert.All(await db.Becas.Where(item => new[] { vencida.Id, indefinida.Id, futura.Id }
            .Contains(item.Id)).ToArrayAsync(),
            beca => Assert.Equal(EstadoBeca.Suspendida, beca.Estado));
    }

    private static Ascenso.Becas.Domain.Otorgamiento.Agregados.Beca CrearSuspendida(
        DateOnly? revision, bool indefinida)
    {
        var beca = DatosPrueba.CrearBecaActiva(DatosPrueba.CrearDatosBeca(),
            DatosPrueba.CrearContextoOperacion(_fecha));
        beca.Suspender(TipoSuspensionBeca.Manual,
            "Suspensión documentada", "Revisión por coordinación",
            new DateOnly(2026, 11, 1), revision, indefinida,
            new ContextoOperacion(Guid.CreateVersion7(), Guid.CreateVersion7(),
                _fecha, "Suspensión autorizada por coordinación"));
        return beca;
    }

    private sealed class RelojFijo : IClock
    {
        public DateTimeOffset UtcNow => _fecha;
    }

    private sealed class LoggerAlertas : ILogger<RevisarSuspensionesJob>
    {
        public List<string> Mensajes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Mensajes.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
