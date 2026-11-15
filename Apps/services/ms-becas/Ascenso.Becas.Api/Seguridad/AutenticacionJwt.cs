using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Configura la autenticación JWT Bearer contra Keycloak. La validación cubre
/// firma, emisor, audiencia y vigencia; los claims se conservan con sus nombres
/// originales (<c>MapInboundClaims = false</c>).
/// </summary>
public static class AutenticacionJwt
{
    public static IServiceCollection AgregarAutenticacionJwt(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        var jwt = configuracion.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
            ?? new JwtOptions();

        servicios.Configure<JwtOptions>(configuracion.GetSection(JwtOptions.Seccion));

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opciones => ConfigurarJwtBearer(opciones, jwt));

        return servicios;
    }

    /// <summary>
    /// Aplica la configuración de validación a las opciones del esquema Bearer.
    /// </summary>
    public static void ConfigurarJwtBearer(JwtBearerOptions opciones, JwtOptions jwt)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentNullException.ThrowIfNull(jwt);

        opciones.MapInboundClaims = false;
        opciones.SaveToken = false;
        opciones.TokenValidationParameters = CrearParametrosValidacion(jwt);

        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            opciones.Authority = jwt.Authority;
            if (!string.IsNullOrWhiteSpace(jwt.MetadataAddress))
            {
                opciones.MetadataAddress = jwt.MetadataAddress;
            }

            opciones.Audience = jwt.Audience;
            opciones.RequireHttpsMetadata = jwt.RequireHttpsMetadata;
        }
    }

    /// <summary>
    /// Construye los parámetros de validación. Cuando se configura una clave
    /// simétrica (solo pruebas o desarrollo) no se consulta el metadata OIDC.
    /// </summary>
    public static TokenValidationParameters CrearParametrosValidacion(JwtOptions jwt)
    {
        ArgumentNullException.ThrowIfNull(jwt);

        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidAudience = jwt.Audience,
            RoleClaimType = jwt.RoleClaimType,
            NameClaimType = jwt.NameClaimType
        };

        if (!string.IsNullOrWhiteSpace(jwt.Issuer))
        {
            parametros.ValidIssuer = jwt.Issuer;
        }

        if (!string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            parametros.IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SigningKey));
            parametros.ValidIssuer ??= "ascenso-test";
        }

        return parametros;
    }
}
