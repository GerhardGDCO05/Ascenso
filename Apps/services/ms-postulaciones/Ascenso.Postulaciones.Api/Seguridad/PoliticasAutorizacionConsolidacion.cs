namespace Ascenso.Postulaciones.Api.Seguridad;

public static class PoliticasAutorizacionConsolidacion
{
    public const string ConsultaConsolidacion = "ConsultaConsolidacion";
    public const string GestionConsolidacion = "GestionConsolidacion";
    public const string ResolucionCasosLimiteRanking = "ResolucionCasosLimiteRanking";
    public const string ConsultaAdjudicabilidadInterna = "ConsultaAdjudicabilidadInterna";
    public const string RolCoordinadorPrograma = PoliticasAutorizacionRubricas.RolCoordinadorPrograma;
    public const string RolMiembroComite = PoliticasAutorizacionRubricas.RolMiembroComite;
    public const string RolDireccion = "DIRECCION";
    public const string ScopeLecturaInterna = "postulaciones.internal.read";
    public const string PermisoLecturaInterna = "postulaciones:leer";
}
