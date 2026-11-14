using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Postulaciones.Application;

public static class DependencyInjection
{
    public static IServiceCollection AgregarAplicacionPostulaciones(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
