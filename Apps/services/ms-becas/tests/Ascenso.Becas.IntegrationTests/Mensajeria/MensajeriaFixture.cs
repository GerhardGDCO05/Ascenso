using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Ascenso.Becas.IntegrationTests.Mensajeria;

/// <summary>
/// Contenedores PostgreSQL y RabbitMQ para validar el Outbox, el Inbox y los
/// consumidores de proyecciones contra infraestructura real.
/// </summary>
public sealed class MensajeriaFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("ascenso_becas")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3.13-alpine")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public string CadenaConexion => _postgres.GetConnectionString();

    public string RabbitHost => _rabbit.Hostname;

    public ushort RabbitPort => _rabbit.GetMappedPublicPort(5672);

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbit.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public BecasDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<BecasDbContext>()
            .UseNpgsql(
                CadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    BecasDbContext.Esquema))
            .Options;

        return new BecasDbContext(opciones);
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionMensajeria : ICollectionFixture<MensajeriaFixture>
{
    public const string Nombre = "Mensajeria";
}
