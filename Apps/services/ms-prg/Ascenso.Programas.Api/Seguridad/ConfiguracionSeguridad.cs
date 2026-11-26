using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Configura la autenticación JWT de Keycloak y la autorización PBAC del módulo
/// de convocatorias. Si no hay <c>Keycloak:Authority</c>, la API arranca con un
/// esquema que no autentica y las rutas protegidas responden 401.
/// </summary>
public static class ConfiguracionSeguridad
{
    public static IServiceCollection AgregarSeguridadProgramas(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var authority = configuration["Keycloak:Authority"];
        var audience = configuration["Keycloak:Audience"];
        var requireHttpsMetadata = configuration.GetValue("Keycloak:RequireHttpsMetadata", true);
        var roleClaimType = configuration["Keycloak:RoleClaimType"] ?? "roles";

        if (string.IsNullOrWhiteSpace(authority))
        {
            services
                .AddAuthentication(AutenticacionNoConfiguradaHandler.Esquema)
                .AddScheme<AuthenticationSchemeOptions, AutenticacionNoConfiguradaHandler>(
                    AutenticacionNoConfiguradaHandler.Esquema,
                    _ => { });
        }
        else
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority.Trim();
                    options.Audience = string.IsNullOrWhiteSpace(audience) ? null : audience.Trim();
                    options.RequireHttpsMetadata = requireHttpsMetadata;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authority.Trim(),
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = string.IsNullOrWhiteSpace(audience) ? null : audience.Trim(),
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        RoleClaimType = roleClaimType,
                        NameClaimType = "preferred_username"
                    };
                });
        }

        services.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticasPermiso>();
        services.AddSingleton<IAuthorizationHandler, ManejadorRequisitoPermiso>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ManejadorResultadoAutorizacionConvocatorias>();
        services.AddAuthorization();

        return services;
    }
}
