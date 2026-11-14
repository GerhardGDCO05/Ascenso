using Microsoft.AspNetCore.Authorization;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Registra las políticas de autorización de la API y sus manejadores.
/// </summary>
public static class AutorizacionApi
{
    public static IServiceCollection AgregarAutorizacion(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios.AddOptions<AutorizacionOptions>()
            .Bind(configuracion.GetSection(AutorizacionOptions.Seccion))
            .Validate(
                opciones => !string.IsNullOrWhiteSpace(opciones.AcrSegundoFactor)
                    || opciones.AmrSegundoFactor.Any(valor =>
                        !string.IsNullOrWhiteSpace(valor)),
                "Debe configurarse al menos un mecanismo de segundo factor (acr o amr).")
            .Validate(
                opciones => opciones.AmrSegundoFactor.All(valor =>
                    !string.IsNullOrWhiteSpace(valor)),
                "Los valores de amr para segundo factor no pueden estar vacíos.")
            .ValidateOnStart();

        servicios.AddSingleton<IAuthorizationHandler, Requiere2FAHandler>();
        servicios.AddSingleton<IAuthorizationHandler, AlcanceProgramaHandler>();
        servicios.AddSingleton<IAuthorizationHandler, PropiedadPostulanteHandler>();
        servicios.AddSingleton<IAuthorizationHandler, CuentaServicioHandler>();
        servicios.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            AutorizacionResultadoHandler>();

        servicios.AddAuthorization(opciones =>
        {
            opciones.AddPolicy(PoliticasSeguridad.Requiere2FA, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new Requiere2FARequirement()));

            opciones.AddPolicy(PoliticasSeguridad.AlcancePrograma, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new AlcanceProgramaRequirement()));

            opciones.AddPolicy(PoliticasSeguridad.PropiedadPostulante, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new PropiedadPostulanteRequirement()));

            opciones.AddPolicy(PoliticasSeguridad.ServiceAccountInterno, politica => politica
                .RequireAuthenticatedUser()
                .AddRequirements(new CuentaServicioRequirement()));
        });

        return servicios;
    }
}
