using Ascenso.Becas.Application.Idempotencia;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Application.Renovacion.Puertos;
using Ascenso.Becas.Application.Renovacion.Servicios;
using Ascenso.Becas.Application.SeguimientoAcademico.Puertos;
using Ascenso.Becas.Domain.Ports;
using Ascenso.Becas.Infrastructure.Clientes;
using Ascenso.Becas.Infrastructure.Persistence;
using Ascenso.Becas.Infrastructure.Persistence.Consultas;
using Ascenso.Becas.Infrastructure.Persistence.Proyecciones;
using Ascenso.Becas.Infrastructure.Persistence.Repositorios;
using Ascenso.Becas.Infrastructure.Persistence.UnidadDeTrabajo;
using Ascenso.Becas.Infrastructure.Seguridad;
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
        servicios.AddScoped<IReporteAcademicoRepository, ReporteAcademicoRepository>();
        servicios.AddScoped<ISolicitudRenovacionRepository, SolicitudRenovacionRepository>();
        servicios.AddScoped<IConsultasRenovacion, ConsultasRenovacion>();
        servicios.AddScoped<ICalendarioAcademicoPrograma, CalendarioAcademicoProgramaNoDisponible>();
        servicios.AddScoped<IVentanaRenovacionPrograma, VentanaRenovacionProgramaNoDisponible>();
        servicios.AddScoped<IPresupuestoRenovacionPrograma, PresupuestoRenovacionProgramaNoDisponible>();
        servicios.AddScoped<ILiberacionCupoPrograma, LiberacionCupoProgramaNoDisponible>();
        servicios.AddScoped<EvaluadorElegibilidadRenovacion>();
        servicios.AddScoped<ICalendarioDesembolsoRepository, CalendarioDesembolsoRepository>();
        servicios.AddScoped<IDatosPagoBecarioRepository, DatosPagoBecarioRepository>();
        servicios.AddScoped<IOfertaBecaRepository, OfertaBecaRepository>();
        servicios.AddScoped<ICompromisoBecarioRepository, CompromisoBecarioRepository>();
        servicios.AddScoped<IUnitOfWork, UnidadDeTrabajo>();
        servicios.AddScoped<IdempotencyRepository>();
        servicios.AddScoped<IProyeccionesOtorgamiento, ProyeccionesOtorgamiento>();
        servicios.AddScoped<IConsultasBeca, ConsultasBeca>();
        servicios.AddScoped<ActualizadorProyeccionesRef>();
        servicios.AddSingleton<IClock, RelojSistema>();
        servicios.AddSingleton<ICifradorCuentaBancaria>(_ =>
            CifradorCuentaBancariaAesGcm.DesdeSecretoDeEntorno("BECAS_CUENTAS_AES_KEY_B64"));

        return servicios;
    }
}
