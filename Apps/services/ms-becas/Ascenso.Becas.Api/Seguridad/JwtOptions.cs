namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Configuración de la validación del JWT emitido por Keycloak.
/// </summary>
public sealed class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Authority { get; set; } = string.Empty;

    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Emisor esperado. Si se omite, se usa el emisor publicado por la
    /// autoridad OIDC.
    /// </summary>
    public string? Issuer { get; set; }

    public string Audience { get; set; } = "ms-bec";

    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Clave simétrica exclusiva para desarrollo y pruebas automatizadas. En
    /// producción se deja vacía y se valida contra las claves publicadas por
    /// Keycloak.
    /// </summary>
    public string? SigningKey { get; set; }

    public string RoleClaimType { get; set; } = "roles";

    public string NameClaimType { get; set; } = "preferred_username";
}
