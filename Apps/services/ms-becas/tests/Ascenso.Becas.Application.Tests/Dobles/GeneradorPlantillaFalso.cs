using System.Text;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Generador de la plantilla del compromiso simulado que devuelve un PDF
/// mínimo en memoria.
/// </summary>
public sealed class GeneradorPlantillaFalso : IPlantillaCompromisoPdfGenerator
{
    public DatosPlantillaCompromiso? Datos { get; private set; }

    public Exception? Error { get; set; }

    public string Contenido { get; set; } =
        "%PDF-1.4 plantilla de compromiso de prueba";

    public Task<Stream> GenerarAsync(
        DatosPlantillaCompromiso datos,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Datos = datos;

        if (Error is not null)
        {
            throw Error;
        }

        Stream contenido = new MemoryStream(Encoding.UTF8.GetBytes(Contenido));
        return Task.FromResult(contenido);
    }
}
