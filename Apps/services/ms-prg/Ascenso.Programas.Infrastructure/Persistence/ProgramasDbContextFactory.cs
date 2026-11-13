using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ascenso.Programas.Infrastructure.Persistence;

/// <summary>
/// Fábrica en tiempo de diseño para herramientas y migraciones de EF Core.
/// </summary>
public sealed class ProgramasDbContextFactory : IDesignTimeDbContextFactory<ProgramasDbContext>
{
    public const string CadenaConexionPorDefecto =
        "Host=localhost;Port=5432;Database=ascenso_programas;Username=postgres;Password=postgres";

    public ProgramasDbContext CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Programas");
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            cadenaConexion = CadenaConexionPorDefecto;
        }

        var opciones = new DbContextOptionsBuilder<ProgramasDbContext>()
            .UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ProgramasDbContext.Esquema))
            .Options;

        return new ProgramasDbContext(opciones);
    }
}

