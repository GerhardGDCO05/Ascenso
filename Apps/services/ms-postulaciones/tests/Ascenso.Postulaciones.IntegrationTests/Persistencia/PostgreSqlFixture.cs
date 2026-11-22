using Ascenso.Postulaciones.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Ascenso.Postulaciones.IntegrationTests.Persistencia;

/// <summary>
/// Contenedor PostgreSQL compartido por las pruebas de persistencia. Aplica
/// todas las migraciones una sola vez para validar el esquema real.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("ascenso_postulaciones")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
        => await _contenedor.DisposeAsync();

    public PostulacionesDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<PostulacionesDbContext>()
            .UseNpgsql(
                CadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    PostulacionesDbContext.Esquema))
            .Options;

        return new PostulacionesDbContext(opciones);
    }
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgreSql : ICollectionFixture<PostgreSqlFixture>
{
    public const string Nombre = "PostgreSQL";
}
