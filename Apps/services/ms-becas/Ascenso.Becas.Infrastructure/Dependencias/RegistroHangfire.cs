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

        return servicios;
    }
}
