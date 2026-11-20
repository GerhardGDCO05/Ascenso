namespace Ascenso.Becas.Domain.Ports;

/// <summary>
/// Genera la plantilla en PDF del compromiso del becario, con las condiciones
/// del programa y de la beca.
/// </summary>
public interface IPlantillaCompromisoPdfGenerator
{
    Task<Stream> GenerarAsync(
        DatosPlantillaCompromiso datos,
        CancellationToken cancellationToken = default);
}
