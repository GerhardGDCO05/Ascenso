using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ascenso.Becas.Infrastructure.Persistence;

/// <summary>
/// Fábrica usada por las herramientas de EF Core en tiempo de diseño y por los
/// comandos de migración. La cadena puede sustituirse con
/// <c>ConnectionStrings__Becas</c>.
/// </summary>
public sealed class BecasDbContextFactory : IDesignTimeDbContextFactory<BecasDbContext>
{
    public const string CadenaConexionPorDefecto =
        "Host=localhost;Port=5432;Database=ascenso_becas;Username=postgres;Password=postgres";

    public BecasDbContext CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Becas");
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            cadenaConexion = CadenaConexionPorDefecto;
        }

        var opciones = new DbContextOptionsBuilder<BecasDbContext>()
            .UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", BecasDbContext.Esquema))
            .Options;

        return new BecasDbContext(opciones);
    }
}
