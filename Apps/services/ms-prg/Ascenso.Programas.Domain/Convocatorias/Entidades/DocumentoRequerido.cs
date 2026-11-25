using Ascenso.Programas.Domain.Comun;
using Ascenso.Programas.Domain.Excepciones;

namespace Ascenso.Programas.Domain.Convocatorias.Entidades;

/// <summary>
/// Documento de soporte exigido por una convocatoria. La carga real del archivo
/// pertenece a la HU 3.1; aquí solo se configura el requisito.
/// </summary>
public sealed class DocumentoRequerido : EntidadAuditable
{
    private DocumentoRequerido()
    {
    }

    private DocumentoRequerido(
        Guid id,
        Guid convocatoriaId,
        string tipoDocumento,
        string nombre,
        string? descripcion,
        bool obligatorio,
        IReadOnlyList<string> tiposMimePermitidos,
        int tamanioMaximoMb,
        DateTimeOffset fechaCreacion,
        Guid usuarioCreacionId)
        : base(id, fechaCreacion, usuarioCreacionId)
    {
        ConvocatoriaId = convocatoriaId;
        TipoDocumento = tipoDocumento;
        Nombre = nombre;
        Descripcion = descripcion;
        Obligatorio = obligatorio;
        TiposMimePermitidos = tiposMimePermitidos;
        TamanioMaximoMb = tamanioMaximoMb;
    }

    public Guid ConvocatoriaId { get; private set; }

    public string TipoDocumento { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public bool Obligatorio { get; private set; }

    public IReadOnlyList<string> TiposMimePermitidos { get; private set; } = Array.Empty<string>();

    public int TamanioMaximoMb { get; private set; }

    internal static DocumentoRequerido Crear(
        Guid convocatoriaId,
        string tipoDocumento,
        string nombre,
        string? descripcion,
        bool obligatorio,
        IReadOnlyCollection<string> tiposMimePermitidos,
        int tamanioMaximoMb,
        Guid usuarioCreacionId,
        DateTimeOffset fechaCreacion)
    {
        if (convocatoriaId == Guid.Empty)
        {
            throw new DocumentoRequeridoInvalidoException("la convocatoria asociada es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(tipoDocumento))
        {
            throw new DocumentoRequeridoInvalidoException("el tipo de documento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new DocumentoRequeridoInvalidoException("el nombre del documento es obligatorio.");
        }

        if (tamanioMaximoMb <= 0)
        {
            throw new DocumentoRequeridoInvalidoException("el tamaño máximo debe ser mayor que cero.");
        }

        ArgumentNullException.ThrowIfNull(tiposMimePermitidos);

        var mimesNormalizados = tiposMimePermitidos
            .Where(mime => !string.IsNullOrWhiteSpace(mime))
            .Select(mime => mime.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (mimesNormalizados.Length == 0)
        {
            throw new DocumentoRequeridoInvalidoException("debe indicar al menos un tipo MIME permitido.");
        }

        var fechaUtc = fechaCreacion.ToUniversalTime();

        return new DocumentoRequerido(
            UuidV7.Nuevo(fechaUtc),
            convocatoriaId,
            tipoDocumento.Trim().ToUpperInvariant(),
            nombre.Trim(),
            string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            obligatorio,
            Array.AsReadOnly(mimesNormalizados),
            tamanioMaximoMb,
            fechaUtc,
            usuarioCreacionId);
    }
}
