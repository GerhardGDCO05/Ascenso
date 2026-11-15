using Ascenso.Postulaciones.Application.Postulantes.Comandos.RegistrarPostulante;
using Ascenso.Postulaciones.Application.Postulantes.Consultas.ObtenerMiPerfil;
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
        services.AddScoped<ObtenerMiPerfilQueryHandler>();

        return services;
    }
}
