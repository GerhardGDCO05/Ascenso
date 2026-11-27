namespace Ascenso.Becas.Api.Seguridad;

/// <summary>
/// Nombres de las políticas de autorización registradas por la API.
/// </summary>
public static class PoliticasSeguridad
{
    public const string Requiere2FA = "Requiere2FA";

    public const string AlcancePrograma = "AlcancePrograma";

    public const string PropiedadPostulante = "PropiedadPostulante";

    public const string ServiceAccountInterno = "ServiceAccountInterno";

    public const string RolCoordinadorPrograma = "COORDINADOR_PROGRAMA";

    public const string RolAdministradorFinanciero = "ADMIN_FINANCIERO";

    public const string RolDireccion = "DIRECCION";

    public const string RolPostulante = "POSTULANTE";

    public const string RolBecario = "BECARIO";

    /// <summary>
    /// Roles que pueden ejecutar operaciones administrativas de otorgamiento.
    /// </summary>
    public const string RolesAdministrativos =
        RolCoordinadorPrograma + "," + RolDireccion;
}
