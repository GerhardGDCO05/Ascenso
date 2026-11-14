using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ascenso.Postulaciones.Infrastructure.Persistence;

/// <summary>
/// Fábrica en tiempo de diseño para herramientas y migraciones de EF Core.
/// </summary>
public sealed class PostulacionesDbContextFactory : IDesignTimeDbContextFactory<PostulacionesDbContext>
{
    public const string CadenaConexionPorDefecto =
        "Host=localhost;Port=5432;Database=ascenso_postulaciones;Username=postgres;Password=postgres";

    public PostulacionesDbContext CreateDbContext(string[] args)
    {
        var cadenaConexion = Environment.GetEnvironmentVariable("ConnectionStrings__Postulaciones");
        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            cadenaConexion = CadenaConexionPorDefecto;
        }

        var opciones = new DbContextOptionsBuilder<PostulacionesDbContext>()
            .UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    PostulacionesDbContext.Esquema))
            .Options;

        return new PostulacionesDbContext(opciones);
    }
}
