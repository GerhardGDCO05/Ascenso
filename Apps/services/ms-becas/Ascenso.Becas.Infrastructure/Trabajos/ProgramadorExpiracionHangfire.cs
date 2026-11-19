using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Ports;
using Hangfire;

namespace Ascenso.Becas.Infrastructure.Trabajos;

/// <summary>
/// Adaptador de <see cref="IProgramadorExpiracionOfertas"/> sobre Hangfire. Si
/// la fecha ya venció, el job se encola de inmediato; en caso contrario queda
/// programado para la fecha exacta.
/// </summary>
public sealed class ProgramadorExpiracionHangfire : IProgramadorExpiracionOfertas
{
    private readonly IBackgroundJobClient _cliente;
    private readonly IClock _reloj;

    public ProgramadorExpiracionHangfire(
        IBackgroundJobClient cliente,
        IClock reloj)
    {
        _cliente = cliente ?? throw new ArgumentNullException(nameof(cliente));
        _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
    }

    public Task<string> ProgramarAsync(
        Guid ofertaId,
        DateTimeOffset fechaVencimiento,
        CancellationToken cancellationToken = default)
    {
        if (ofertaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la oferta es obligatorio.",
                nameof(ofertaId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var referencia = fechaVencimiento <= _reloj.UtcNow
            ? _cliente.Enqueue<ExpirarOfertaBecaJob>(
                job => job.Ejecutar(ofertaId, CancellationToken.None))
            : _cliente.Schedule<ExpirarOfertaBecaJob>(
                job => job.Ejecutar(ofertaId, CancellationToken.None),
                fechaVencimiento - _reloj.UtcNow);

        return Task.FromResult(referencia);
    }

    public Task CancelarAsync(
        string referenciaJob,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(referenciaJob))
        {
            _cliente.Delete(referenciaJob);
        }

        return Task.CompletedTask;
    }
}
