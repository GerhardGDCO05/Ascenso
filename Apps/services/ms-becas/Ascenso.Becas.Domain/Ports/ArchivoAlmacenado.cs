namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Metadatos devueltos después de almacenar un archivo binario.
/// </summary>
public sealed record ArchivoAlmacenado(
    string Url,
    string HashSha256,
    long TamanioBytes);
