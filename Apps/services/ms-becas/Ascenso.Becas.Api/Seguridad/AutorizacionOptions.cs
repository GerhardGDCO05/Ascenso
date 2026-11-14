namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Configuración de las políticas de autorización y del contrato de claims de
/// Keycloak.
/// </summary>
public sealed class AutorizacionOptions
{
    public const string Seccion = "Autorizacion";

    public string AcrSegundoFactor { get; set; } = "urn:ascenso:loa:2fa";

    public string[] AmrSegundoFactor { get; set; } = ["otp", "totp"];

    public string[] RolesAdministrativos { get; set; } = ["COORDINADOR_PROGRAMA", "DIRECCION"];

    public string[] ClientesServicioPermitidos { get; set; } = ["ms-bec-internal"];

    public string ScopeInterno { get; set; } = "becas.internal.read";

    public string ClaimPostulanteId { get; set; } = "postulante_id";

    public string ClaimProgramaIds { get; set; } = "programa_ids";

    public string ClaimClientId { get; set; } = "client_id";

    public string ClaimScope { get; set; } = "scope";

    public string ClaimRol { get; set; } = "roles";

    public string ClaimAcr { get; set; } = "acr";

    public string ClaimAmr { get; set; } = "amr";

    public string RolDireccion { get; set; } = "DIRECCION";
}
