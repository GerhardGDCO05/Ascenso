namespace Ascenso.Postulaciones.Api.Seguridad;

public static class PoliticasAutorizacionListaEspera
{
    public const string ConsultaListaEspera = "ConsultaListaEspera";
    public const string GestionListaEspera = "GestionListaEspera";
    public const string RegistroVacanteListaEsperaInterna = "RegistroVacanteListaEsperaInterna";
    public const string RolCoordinadorPrograma = PoliticasAutorizacionRubricas.RolCoordinadorPrograma;
    public const string RolDireccion = PoliticasAutorizacionConsolidacion.RolDireccion;
    public const string ScopeEscrituraInterna = "postulaciones.internal.write";
    public const string PermisoEscrituraInterna = "postulaciones:escribir";
}
