using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfraestructuraPostulaciones(
        this IServiceCollection services,
        string cadenaConexion)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException(
                "La cadena de conexión 'Postulaciones' es obligatoria.",
                nameof(cadenaConexion));
        }

        services.AddDbContext<PostulacionesDbContext>(opciones =>
            opciones.UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    PostulacionesDbContext.Esquema)));

        services.AddScoped<IUnitOfWork>(proveedor =>
            proveedor.GetRequiredService<PostulacionesDbContext>());

        return services;
    }
}
