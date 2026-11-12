namespace Ascenso.Becas.Domain.Otorgamiento.Eventos;

public sealed record class CompromisoBecarioCargadoEvento : EventoCompromisoBecario
{
    public CompromisoBecarioCargadoEvento(
        Guid becaId,
        Guid compromisoId,
        int numeroIntentoCarga,
        Guid correlacionId,
        DateTimeOffset occurredAt)
        : base(becaId, compromisoId, correlacionId, occurredAt)
    {
        if (numeroIntentoCarga < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numeroIntentoCarga),
                "El número de intento de carga debe ser mayor que cero.");
        }

        NumeroIntentoCarga = numeroIntentoCarga;
    }

    public int NumeroIntentoCarga { get; }
}
