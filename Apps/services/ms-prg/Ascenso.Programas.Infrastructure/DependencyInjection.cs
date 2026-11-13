using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Infrastructure.Mensajeria;
using Ascenso.Programas.Infrastructure.Persistence;
using Ascenso.Programas.Infrastructure.Persistence.Repositorios;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Programas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfraestructuraProgramas(
        this IServiceCollection services,
        string cadenaConexion)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            throw new ArgumentException("La cadena de conexión 'Programas' es obligatoria.", nameof(cadenaConexion));
        }

        services.AddDbContext<ProgramasDbContext>(opciones =>
            opciones.UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    ProgramasDbContext.Esquema)));

        services.AddScoped<IUnitOfWork>(proveedor => proveedor.GetRequiredService<ProgramasDbContext>());
        services.AddScoped<IDonanteRepository, DonanteRepository>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<ProgramasDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingInMemory((contexto, configuracion) =>
            {
                configuracion.ConfigureEndpoints(contexto);
            });
        });

        return services;
    }
}

