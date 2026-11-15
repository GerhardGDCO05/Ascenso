namespace Ascenso.Becas.Application.Excepciones;

/// <summary>
/// Se produce cuando el archivo de compromiso no es un PDF válido, excede el
/// tamaño permitido o reutiliza el hash de un intento rechazado.
/// </summary>
public sealed class ArchivoCompromisoInvalidoException : AplicacionException
{
    public const string CodigoError = "ARCHIVO_COMPROMISO_INVALIDO";

    public ArchivoCompromisoInvalidoException(string detalle)
        : base(CodigoError, 400, detalle)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detalle);
    }
}
