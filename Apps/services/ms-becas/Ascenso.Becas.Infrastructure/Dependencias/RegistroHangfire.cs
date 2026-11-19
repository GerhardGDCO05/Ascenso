using Ascenso.Becas.Infrastructure.Trabajos;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Infrastructure.Dependencias;

/// <summary>
/// Registro de Hangfire con almacenamiento persistente en el esquema
/// <c>hangfire</c> de PostgreSQL.
/// </summary>
public static class RegistroHangfire
{
    public const string Esquema = "hangfire";

    public const string ColaCritica = "critical";

    public const string ColaPorDefecto = "default";

    public static IServiceCollection AgregarHangfire(
        this IServiceCollection servicios,
        string cadenaConexion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cadenaConexion);

        servicios.AddHangfire(configuracion => configuracion
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                opciones => opciones.UseNpgsqlConnection(cadenaConexion),
                new PostgreSqlStorageOptions
                {
                    SchemaName = Esquema,
                    PrepareSchemaIfNecessary = true
                }));

        servicios.AddSingleton<JobActivator, ActivadorHangfire>();

        return servicios;
    }
}
