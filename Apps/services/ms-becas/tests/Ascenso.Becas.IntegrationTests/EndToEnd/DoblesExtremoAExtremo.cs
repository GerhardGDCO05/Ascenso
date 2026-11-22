using System.Collections.Concurrent;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using Ascenso.Becas.Domain.Ports;

namespace Ascenso.Becas.IntegrationTests.EndToEnd;

/// <summary>
/// Outbox en memoria para verificar los eventos publicados sin depender de
/// RabbitMQ. El Outbox real se prueba en <c>MensajeriaRabbitMqTests</c>.
/// </summary>
internal sealed class OutboxExtremoAExtremo : IOutboxBeca
{
    private readonly ConcurrentQueue<IDomainEvent> _eventos = new();

    public IReadOnlyList<IDomainEvent> Eventos => _eventos.ToArray();

    public Task EncolarAsync(
        IDomainEvent evento,
        int aggregateVersion = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        cancellationToken.ThrowIfCancellationRequested();
        _eventos.Enqueue(evento);
        return Task.CompletedTask;
    }
}

internal sealed class CartaOfertaExtremoAExtremo : ICartaOfertaPdfGenerator
{
    public Task<Stream> GenerarAsync(
        DatosCartaOferta datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<Stream>(
            new MemoryStream("%PDF-1.7 carta de oferta"u8.ToArray()));
    }
}

internal sealed class PlantillaCompromisoExtremoAExtremo : IPlantillaCompromisoPdfGenerator
{
    public Task<Stream> GenerarAsync(
        DatosPlantillaCompromiso datos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(datos);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<Stream>(
            new MemoryStream("%PDF-1.7 plantilla de compromiso"u8.ToArray()));
    }
}

internal sealed class AlmacenamientoExtremoAExtremo : IAlmacenamientoArchivos
{
    public Task<ArchivoAlmacenado> SubirAsync(
        Stream contenido,
        string tipoMime,
        string rutaLogica,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoMime);
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaLogica);
        cancellationToken.ThrowIfCancellationRequested();

        var tamanio = contenido.CanSeek ? contenido.Length : 0;
        var hash = new string('a', 64);

        return Task.FromResult(new ArchivoAlmacenado(
            $"https://archivos.prueba/{rutaLogica}",
            hash,
            tamanio == 0 ? 1 : tamanio));
    }

    public Task EliminarAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task<string> ResolverUrlLecturaAsync(
        string referencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(referencia);
    }
}
