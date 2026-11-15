using System.Text;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Generador de cartas simulado que devuelve un PDF mínimo en memoria.
/// </summary>
public sealed class GeneradorCartaFalso : ICartaOfertaPdfGenerator
{
    public DatosCartaOferta? Datos { get; private set; }

    public Exception? Error { get; set; }

    public string Contenido { get; set; } = "%PDF-1.4 carta de otorgamiento de prueba";

    public Task<Stream> GenerarAsync(
        DatosCartaOferta datos,
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
