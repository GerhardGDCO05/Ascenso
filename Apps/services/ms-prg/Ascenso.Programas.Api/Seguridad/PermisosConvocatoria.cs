namespace Ascenso.Programas.Api.Seguridad;

/// <summary>
/// Catálogo de permisos PBAC usados por el módulo de convocatorias.
/// </summary>
public static class PermisosConvocatoria
{
    public const string Prefijo = "convocatorias:";

    public const string Crear = Prefijo + "crear";

    public const string Editar = Prefijo + "editar";

    public const string Leer = Prefijo + "leer";

    public const string Publicar = Prefijo + "publicar";

    public const string Cerrar = Prefijo + "cerrar";

    public const string ClaimPermisos = "permissions";

    public static bool EsPoliticaConvocatoria(string? politica)
        => !string.IsNullOrWhiteSpace(politica)
            && politica.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase);
}
