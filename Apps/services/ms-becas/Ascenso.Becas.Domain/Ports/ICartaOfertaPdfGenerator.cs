namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Puerto de generación de la carta de oferta en formato PDF.
/// </summary>
public interface ICartaOfertaPdfGenerator
{
    Task<Stream> GenerarAsync(
        DatosCartaOferta datos,
        CancellationToken cancellationToken = default);
}
