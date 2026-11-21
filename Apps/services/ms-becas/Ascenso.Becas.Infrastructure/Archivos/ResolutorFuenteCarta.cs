using System.Reflection;
using PdfSharpCore.Fonts;

namespace Ascenso.Becas.Infrastructure.Archivos;

/// <summary>
/// Resolutor de fuentes autocontenido: usa Liberation Sans embebida para que la
/// carta se genere igual en cualquier entorno, sin depender de las fuentes del
/// sistema operativo.
/// </summary>
internal sealed class ResolutorFuenteCarta : IFontResolver
{
    internal const string Familia = "CartaAscenso";
    private const string NombreCara = "CartaAscensoRegular";
    private const string RecursoFuente =
        "Ascenso.Becas.Infrastructure.Archivos.Recursos.LiberationSans-Regular.ttf";

    private static readonly Lazy<byte[]> _datosFuente = new(
        CargarFuente,
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly object _candado = new();

    public string DefaultFontName => Familia;

    public FontResolverInfo ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic)
        => new(NombreCara, isBold, isItalic);

    public byte[] GetFont(string faceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(faceName);
        return _datosFuente.Value;
    }

    public static void AsegurarRegistrado()
    {
        lock (_candado)
        {
            if (GlobalFontSettings.FontResolver is not ResolutorFuenteCarta)
            {
                GlobalFontSettings.FontResolver = new ResolutorFuenteCarta();
            }
        }
    }

    private static byte[] CargarFuente()
    {
        var asamblea = Assembly.GetExecutingAssembly();
        using var recurso = asamblea.GetManifestResourceStream(RecursoFuente)
            ?? throw new InvalidOperationException(
                $"No se encontró la fuente embebida '{RecursoFuente}'.");

        using var memoria = new MemoryStream();
        recurso.CopyTo(memoria);
        return memoria.ToArray();
    }
}
