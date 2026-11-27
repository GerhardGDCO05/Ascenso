using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Infrastructure.Dependencias;
using Ascenso.Becas.Infrastructure.Persistence;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ascenso.Becas.IntegrationTests.Mensajeria;

[Collection(ColeccionMensajeria.Nombre)]
public sealed class MensajeriaRabbitMqTests
{
    private readonly MensajeriaFixture _fixture;

    public MensajeriaRabbitMqTests(MensajeriaFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Debe_PersistirEnOutbox_Y_ActualizarLaProyeccionAlConsumir()
    {
        var host = await IniciarHostAsync();

        try
        {
            var programaId = Guid.CreateVersion7();
            var eventoId = Guid.CreateVersion7();
            var mensaje = CrearPrograma(programaId, eventoId);

            await using (var alcance = host.Services.CreateAsyncScope())
            {
                var contexto = alcance.ServiceProvider.GetRequiredService<BecasDbContext>();
                var publicador = alcance.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                await publicador.Publish(mensaje);

                var antesDelGuardado = await ContarOutboxAsync(contexto);
                antesDelGuardado.Should().Be(0);

                await contexto.SaveChangesAsync();

                var despuesDelGuardado = await ContarOutboxAsync(contexto);
                despuesDelGuardado.Should().BeGreaterThan(0);
            }

            var aplicado = await EsperarAsync(async () =>
            {
                await using var contexto = _fixture.CrearContexto();
                return await contexto.ProgramasBecaRef.AnyAsync(programa =>
                    programa.ProgramaBecaId == programaId
                    && programa.UltimoEventoId == eventoId);
            });

            aplicado.Should().BeTrue();
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    [Fact]
    public async Task Debe_IgnorarElMensajeDuplicado_Cuando_ElInboxYaLoProceso()
    {
        var host = await IniciarHostAsync();

        try
        {
            var bus = host.Services.GetRequiredService<IBusControl>();
            var programaId = Guid.CreateVersion7();
            var eventoId = Guid.CreateVersion7();
            var mensaje = CrearPrograma(programaId, eventoId);

            await bus.Publish(mensaje, contexto => contexto.MessageId = eventoId);

            var aplicado = await EsperarAsync(async () =>
            {
                await using var contexto = _fixture.CrearContexto();
                return await contexto.ProgramasBecaRef.AnyAsync(programa =>
                    programa.ProgramaBecaId == programaId);
            });

            aplicado.Should().BeTrue();

            await bus.Publish(mensaje, contexto => contexto.MessageId = eventoId);
            await Task.Delay(TimeSpan.FromSeconds(2));

            await using var verificacion = _fixture.CrearContexto();
            var entradasInbox = await verificacion.Database
                .SqlQuery<int>($"""
                    SELECT COUNT(*)::int AS "Value"
                    FROM becas.inbox_state
                    WHERE "MessageId" = {eventoId}
                    """)
                .SingleAsync();

            entradasInbox.Should().Be(1);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    private async Task<IHost> IniciarHostAsync()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mensajeria:Habilitada"] = "true",
                ["RabbitMq:Host"] = _fixture.RabbitHost,
                ["RabbitMq:Port"] = _fixture.RabbitPort.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                ["RabbitMq:VirtualHost"] = "/",
                ["RabbitMq:Username"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:PrefetchCount"] = "16"
            })
            .Build();

        var constructor = Host.CreateApplicationBuilder();
        constructor.Configuration.AddConfiguration(configuracion);
        constructor.Services.AddLogging();
        constructor.Services.AgregarPersistencia(_fixture.CadenaConexion);
        constructor.Services.AgregarMensajeria(configuracion);
        constructor.Services.AddOptions<MassTransitHostOptions>()
            .Configure(opciones => opciones.WaitUntilStarted = true);

        var host = constructor.Build();
        await host.StartAsync();

        return host;
    }

    private static EventoIntegracion<ProgramaBecaConfigurado> CrearPrograma(
        Guid programaId,
        Guid eventoId)
        => new()
        {
            EventId = eventoId,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = Guid.CreateVersion7(),
            Producer = "ms-prg",
            AggregateId = programaId,
            AggregateVersion = 1,
            Payload = new ProgramaBecaConfigurado(
                programaId,
                $"PRG-{programaId:N}"[..12],
                "Programa de prueba",
                new PoliticaCoberturaIntegracion("TOTAL", 100m, 0m, "USD"),
                new CondicionesPermanenciaIntegracion(14m, 4, 0, 40, "SUSPENSION"),
                false,
                true,
                2,
                "ACTIVO")
        };

    private static async Task<int> ContarOutboxAsync(BecasDbContext contexto)
        => await contexto.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value"
                FROM becas.outbox_message
                """)
            .SingleAsync();

    private static async Task<bool> EsperarAsync(
        Func<Task<bool>> condicion,
        int intentos = 60,
        int esperaMilisegundos = 250)
    {
        for (var intento = 0; intento < intentos; intento++)
        {
            if (await condicion())
            {
                return true;
            }

            await Task.Delay(esperaMilisegundos);
        }

        return false;
    }
}
