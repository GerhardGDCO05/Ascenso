using Ascenso.Postulaciones.Application.Postulantes.Comandos.ActualizarPerfilSocioeconomico;
using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.AgregarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.BloquearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.CrearRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EditarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.EliminarCriterio;
using Ascenso.Postulaciones.Application.Rubricas.Comandos.PublicarRubrica;
using Ascenso.Postulaciones.Application.Rubricas.Consultas.ObtenerRubrica;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.Application;

public static class DependencyInjection
{
    public static IServiceCollection AgregarAplicacionPostulaciones(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<RegistrarPostulanteCommandHandler>();
        services.AddScoped<ActualizarPerfilSocioeconomicoCommandHandler>();
        services.AddScoped<ObtenerMiPerfilQueryHandler>();
        services.AddScoped<CrearRubricaCommandHandler>();
        services.AddScoped<AgregarCriterioCommandHandler>();
        services.AddScoped<EditarCriterioCommandHandler>();
        services.AddScoped<EliminarCriterioCommandHandler>();
        services.AddScoped<PublicarRubricaCommandHandler>();
        services.AddScoped<BloquearRubricaCommandHandler>();
        services.AddScoped<ObtenerRubricaQueryHandler>();

        return services;
    }
}
