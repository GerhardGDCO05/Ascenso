using Ascenso.Programas.Application.Convocatorias.Abstracciones;
using Ascenso.Programas.Application.Donantes.Abstracciones;
using Ascenso.Programas.Domain.Interfaces;
using Ascenso.Programas.Infrastructure.Consultas;
using Ascenso.Programas.Infrastructure.Integraciones;
using Ascenso.Programas.Infrastructure.Mensajeria;
using Ascenso.Programas.Infrastructure.Persistence;
using Ascenso.Programas.Infrastructure.Persistence.Repositorios;
using Ascenso.Programas.Infrastructure.Trabajos;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ascenso.Programas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AgregarInfraestructuraProgramas(
        this IServiceCollection services,
        string cadenaConexion,
        IConfiguration? configuration = null)
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
        services.AddScoped<IConvocatoriaTransactionExecutor, ConvocatoriaTransactionExecutor>();
        services.AddScoped<IDonanteRepository, DonanteRepository>();
        services.AddScoped<IUniversidadAliadaRepository, UniversidadAliadaRepository>();
        services.AddScoped<IProgramaBecaRepository, ProgramaBecaRepository>();
        services.AddScoped<IConvocatoriaRepository, ConvocatoriaRepository>();
        services.AddScoped<ICatalogoConvocatoriaReader, CatalogoConvocatoriaReader>();
        services.AddScoped<CerrarConvocatoriasVencidasJob>();
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        if (configuration is not null)
        {
            services.Configure<PeriodoAcademicoOptions>(
                configuration.GetSection(PeriodoAcademicoOptions.Seccion));
        }
        else
        {
            services.AddOptions<PeriodoAcademicoOptions>();
        }

        services.AddHttpClient<IPeriodoAcademicoClient, PeriodoAcademicoClient>((proveedor, cliente) =>
        {
            var opciones = proveedor.GetRequiredService<IOptions<PeriodoAcademicoOptions>>().Value;
            cliente.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos > 0 ? opciones.TimeoutSegundos : 10);
        });

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

