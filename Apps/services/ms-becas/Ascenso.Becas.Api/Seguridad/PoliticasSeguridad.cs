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
}
