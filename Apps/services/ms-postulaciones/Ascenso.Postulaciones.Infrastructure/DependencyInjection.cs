using Ascenso.Postulaciones.Domain.Interfaces;
using Ascenso.Postulaciones.Infrastructure.Adapters;
using Ascenso.Postulaciones.Infrastructure.Persistence;
using Ascenso.Postulaciones.Infrastructure.Persistence.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ascenso.Postulaciones.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfraestructuraPostulaciones(
        this IServiceCollection services,
        string cadenaConexion,
        IConfiguration? configuration = null)
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

        services.AddScoped<IPostulanteRepository, PostulanteRepository>();
        services.AddScoped<IRubricaRepository, RubricaRepository>();

        if (configuration is not null)
        {
            services.Configure<ServiciosExternosOptions>(
                configuration.GetSection(ServiciosExternosOptions.Seccion));
            services.Configure<PeriodoActualOptions>(
                configuration.GetSection(PeriodoActualOptions.Seccion));
        }
        else
        {
            services.AddOptions<ServiciosExternosOptions>();
            services.AddOptions<PeriodoActualOptions>();
        }

        services.AddHttpClient<IPeriodoAcademicoClient, PeriodoAcademicoClient>((proveedor, cliente) =>
        {
            var opciones = proveedor.GetRequiredService<IOptions<ServiciosExternosOptions>>().Value;
            cliente.Timeout = ResolverTimeout(opciones.TimeoutSegundos);
        });

        services.AddHttpClient<ICatalogoAcademicoClient, CatalogoAcademicoClient>((proveedor, cliente) =>
        {
            var opciones = proveedor.GetRequiredService<IOptions<ServiciosExternosOptions>>().Value;
            cliente.Timeout = ResolverTimeout(opciones.TimeoutSegundos);
        });

        return services;
    }

    private static TimeSpan ResolverTimeout(int timeoutSegundos)
        => TimeSpan.FromSeconds(timeoutSegundos > 0 ? timeoutSegundos : 3);
}
