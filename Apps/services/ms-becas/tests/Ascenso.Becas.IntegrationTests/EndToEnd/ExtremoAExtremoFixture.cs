using Ascenso.Becas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

/// <summary>
/// Infraestructura compartida por las pruebas de extremo a extremo: PostgreSQL
/// real para la persistencia y WireMock para simular MS-POS y MS-PRG.
/// </summary>
public sealed class ExtremoAExtremoFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("ascenso_becas")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string CadenaConexion => _postgres.GetConnectionString();

    public WireMockServer WireMock { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        WireMock = WireMockServer.Start();

        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        WireMock.Dispose();
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

[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionExtremoAExtremo : ICollectionFixture<ExtremoAExtremoFixture>
{
    public const string Nombre = "ExtremoAExtremo";
}
