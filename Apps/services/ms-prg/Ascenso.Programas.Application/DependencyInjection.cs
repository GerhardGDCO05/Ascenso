using Ascenso.Programas.Application.Donantes.Comandos.ActualizarContactoDonante;
using Ascenso.Programas.Application.Donantes.Comandos.DesactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.ReactivarDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarAporteDonante;
using Ascenso.Programas.Application.Donantes.Comandos.RegistrarDonante;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarDonantes;
using Ascenso.Programas.Application.Donantes.Consultas.ConsultarHistorialAportes;
using Ascenso.Programas.Application.Donantes.Consultas.ObtenerDonantePorId;
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

        return services;
    }
}

