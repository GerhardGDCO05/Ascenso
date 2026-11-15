using Ascenso.Becas.Application.Despacho;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Ascenso.Becas.Application;

/// <summary>
/// Registro de los casos de uso, validadores y despachadores de la capa de
/// aplicación.
/// </summary>
public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddValidatorsFromAssembly(
            typeof(RegistroAplicacion).Assembly,
            ServiceLifetime.Scoped);

        servicios.AddScoped<ICommandDispatcher, CommandDispatcher>();
        servicios.AddScoped<IQueryDispatcher, QueryDispatcher>();

        RegistrarHandlers(servicios);

        return servicios;
    }

    private static void RegistrarHandlers(IServiceCollection servicios)
    {
        var tipos = typeof(RegistroAplicacion).Assembly
            .GetTypes()
            .Where(tipo => tipo is { IsClass: true, IsAbstract: false })
            .ToArray();

        foreach (var tipo in tipos)
        {
            foreach (var contrato in tipo.GetInterfaces())
            {
                if (EsContratoDeHandler(contrato))
                {
                    servicios.AddScoped(contrato, tipo);
                }
            }
        }
    }

    private static bool EsContratoDeHandler(Type contrato)
    {
        if (!contrato.IsGenericType)
        {
            return false;
        }

        var definicion = contrato.GetGenericTypeDefinition();

        return definicion == typeof(ICommandHandler<>)
            || definicion == typeof(ICommandHandler<,>)
            || definicion == typeof(IQueryHandler<,>);
    }
}
