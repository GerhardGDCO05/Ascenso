using Ascenso.Becas.Application.Otorgamiento.Puertos;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Planificador de expiraciones simulado.
/// </summary>
public sealed class ProgramadorExpiracionFalso : IProgramadorExpiracionOfertas
{
    public string Referencia { get; set; } = "job-expiracion-oferta";

    public Exception? ErrorProgramar { get; set; }

    public Exception? ErrorCancelar { get; set; }

    public bool ResultadoCancelar { get; set; } = true;

    public List<(Guid OfertaId, DateTimeOffset FechaVencimiento)> Programadas { get; } = [];

    public List<string> SolicitudesCancelacion { get; } = [];

    public List<string> Canceladas { get; } = [];

    public Task<string> ProgramarAsync(
        Guid ofertaId,
        DateTimeOffset fechaVencimiento,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ErrorProgramar is not null)
        {
            throw ErrorProgramar;
        }

        Programadas.Add((ofertaId, fechaVencimiento));
        return Task.FromResult(Referencia);
    }

    public Task<bool> CancelarAsync(
        string referenciaJob,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SolicitudesCancelacion.Add(referenciaJob);

        if (ErrorCancelar is not null)
        {
            throw ErrorCancelar;
        }

        if (ResultadoCancelar)
        {
            Canceladas.Add(referenciaJob);
        }

        return Task.FromResult(ResultadoCancelar);
    }
}
