using Microsoft.Extensions.Configuration;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Configuración del almacenamiento de archivos. El proveedor local se usa en
/// desarrollo y Firebase/Google Cloud Storage en integración y producción.
/// </summary>
public sealed class OpcionesAlmacenamiento
{
    public const string Seccion = "Storage";

    public string Proveedor { get; init; } = ProveedoresAlmacenamiento.Local;

    public OpcionesAlmacenamientoLocal Local { get; init; } = new();

    public OpcionesAlmacenamientoFirebase Firebase { get; init; } = new();

    public static OpcionesAlmacenamiento Leer(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);

        var seccion = configuracion.GetSection(Seccion);

        return new OpcionesAlmacenamiento
        {
            Proveedor = seccion["Provider"] ?? ProveedoresAlmacenamiento.Local,
            Local = new OpcionesAlmacenamientoLocal
            {
                RutaBase = seccion["Local:RutaBase"] ?? "almacenamiento",
                UrlBase = VacioComoNulo(seccion["Local:UrlBase"])
            },
            Firebase = new OpcionesAlmacenamientoFirebase
            {
                Bucket = seccion["Firebase:Bucket"] ?? string.Empty,
                RutaCredenciales = VacioComoNulo(seccion["Firebase:RutaCredenciales"]),
                UrlBase = VacioComoNulo(seccion["Firebase:UrlBase"]),
                UsaEmulador = seccion.GetValue<bool?>("Firebase:UsaEmulador") ?? false
            }
        };
    }

    private static string? VacioComoNulo(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

/// <summary>
/// Nombres de los proveedores de almacenamiento admitidos.
/// </summary>
public static class ProveedoresAlmacenamiento
{
    public const string Local = "Local";

    public const string Firebase = "Firebase";
}

/// <summary>
/// Opciones del adaptador local de archivos.
/// </summary>
public sealed class OpcionesAlmacenamientoLocal
{
    public string RutaBase { get; init; } = "almacenamiento";

    public string? UrlBase { get; init; }
}

/// <summary>
/// Opciones del adaptador de Firebase/Google Cloud Storage.
/// </summary>
public sealed class OpcionesAlmacenamientoFirebase
{
    public string Bucket { get; init; } = string.Empty;

    public string? RutaCredenciales { get; init; }

    public string? UrlBase { get; init; }

    public bool UsaEmulador { get; init; }
}
