using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Consultas;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Ascenso.Becas.Infrastructure.Tiempo;
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
        servicios.AddScoped<IBecaRepository, BecaRepository>();
        servicios.AddScoped<IOfertaBecaRepository, OfertaBecaRepository>();
        servicios.AddScoped<ICompromisoBecarioRepository, CompromisoBecarioRepository>();
        servicios.AddScoped<IUnitOfWork, UnidadDeTrabajo>();
        servicios.AddScoped<IProyeccionesOtorgamiento, ProyeccionesOtorgamiento>();
        servicios.AddScoped<IConsultasBeca, ConsultasBeca>();
        servicios.AddScoped<ActualizadorProyeccionesRef>();
        servicios.AddSingleton<IClock, RelojSistema>();

        return servicios;
    }
}
