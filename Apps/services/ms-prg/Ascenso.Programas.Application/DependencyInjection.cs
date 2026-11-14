using Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;
using Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.ReactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;
using Ascenso.Programas.Application.Donantes.Consultas.ObtenerDonantePorId;
using Ascenso.Programas.Application.Programas.Comandos.AsignarPresupuestoDonantes;
using Ascenso.Programas.Application.Programas.Comandos.ConfigurarCriteriosElegibilidad;
using Ascenso.Programas.Application.Programas.Comandos.CrearProgramaBeca;
using Ascenso.Programas.Application.Programas.Comandos.ReajustarPresupuestoDonante;
using Ascenso.Programas.Application.Programas.Consultas.ConsultarProgramas;
using Ascenso.Programas.Application.Programas.Consultas.ObtenerProgramaPorId;
using Ascenso.Programas.Application.Universidades.Comandos.ActualizarCostoMatricula;
using Ascenso.Programas.Application.Universidades.Comandos.AgregarCarreraElegible;
using Ascenso.Programas.Application.Universidades.Comandos.MarcarConvenioVencido;
using Ascenso.Programas.Application.Universidades.Comandos.RegistrarNuevoConvenio;
using Ascenso.Programas.Application.Universidades.Comandos.RegistrarUniversidadConvenio;
using Ascenso.Programas.Application.Universidades.Consultas.ConsultarHistorialCostosCarrera;
using Ascenso.Programas.Application.Universidades.Consultas.ConsultarUniversidades;
using Ascenso.Programas.Application.Universidades.Consultas.ObtenerUniversidadPorId;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Programas.Application;

public static class DependencyInjection
{
    public static IServiceCollection AgregarAplicacionProgramas(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<RegistrarDonanteCommandHandler>();
        services.AddScoped<ActualizarContactoDonanteCommandHandler>();
        services.AddScoped<DesactivarDonanteCommandHandler>();
        services.AddScoped<ReactivarDonanteCommandHandler>();
        services.AddScoped<RegistrarAporteDonanteCommandHandler>();

        services.AddScoped<ConsultarDonantesQueryHandler>();
        services.AddScoped<ObtenerDonantePorIdQueryHandler>();
        services.AddScoped<ConsultarHistorialAportesQueryHandler>();

        services.AddScoped<RegistrarUniversidadConvenioCommandHandler>();
        services.AddScoped<RegistrarNuevoConvenioCommandHandler>();
        services.AddScoped<MarcarConvenioVencidoCommandHandler>();
        services.AddScoped<AgregarCarreraElegibleCommandHandler>();
        services.AddScoped<ActualizarCostoMatriculaCommandHandler>();

        services.AddScoped<ConsultarUniversidadesQueryHandler>();
        services.AddScoped<ObtenerUniversidadPorIdQueryHandler>();
        services.AddScoped<ConsultarHistorialCostosCarreraQueryHandler>();

        services.AddScoped<CrearProgramaBecaCommandHandler>();
        services.AddScoped<AsignarPresupuestoDonantesCommandHandler>();
        services.AddScoped<ReajustarPresupuestoDonanteCommandHandler>();
        services.AddScoped<ConfigurarCriteriosElegibilidadCommandHandler>();

        services.AddScoped<ConsultarProgramasQueryHandler>();
        services.AddScoped<ObtenerProgramaPorIdQueryHandler>();

        return services;
    }
}

