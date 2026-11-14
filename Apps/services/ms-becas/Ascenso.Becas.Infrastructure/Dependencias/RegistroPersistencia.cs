using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Dependencias;

/// <summary>
/// Registro de la persistencia PostgreSQL y de los almacenes transversales.
/// </summary>
public static class RegistroPersistencia
{
    public static IServiceCollection AgregarPersistencia(
        this IServiceCollection servicios,
        string cadenaConexion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        servicios.AddDbContext<BecasDbContext>(opciones =>
            opciones.UseNpgsql(
                cadenaConexion,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    BecasDbContext.Esquema)));

        servicios.AddScoped<IAlmacenIdempotencia, AlmacenIdempotencia>();

        return servicios;
    }
}
