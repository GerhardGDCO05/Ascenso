using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Application.Tests.Dobles;

/// <summary>
/// Outbox simulado. Los eventos permanecen pendientes hasta que la unidad de
/// trabajo confirma y se descartan si la transacción se revierte.
/// </summary>
public sealed class OutboxBecaFalso : IOutboxBeca
{
    private readonly List<IDomainEvent> _pendientes = [];

    public IReadOnlyList<IDomainEvent> Pendientes => _pendientes;

    public List<IDomainEvent> Confirmados { get; } = [];

    public bool Descartado { get; private set; }

    public Task EncolarAsync(
        IDomainEvent evento,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);
        cancellationToken.ThrowIfCancellationRequested();
        _pendientes.Add(evento);
        return Task.CompletedTask;
    }

    public void Confirmar()
    {
        Confirmados.AddRange(_pendientes);
        _pendientes.Clear();
    }

    public void Descartar()
    {
        _pendientes.Clear();
        Descartado = true;
    }
}
