using Ascenso.Becas.Application.IntegracionEventos;
using Ascenso.Becas.Application.Otorgamiento.Puertos;
using Ascenso.Becas.Domain.Comun;
using MassTransit;

namespace Ascenso.Becas.Infrastructure.Mensajeria;

/// <summary>
/// Adaptador del puerto <see cref="IOutboxBeca"/> sobre el Outbox transaccional
/// de MassTransit. La publicación se ejecuta dentro de la transacción del
/// <c>BecasDbContext</c> y se materializa en <c>outbox_message</c> antes del
/// commit.
/// </summary>
public sealed class OutboxBeca : IOutboxBeca
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OutboxBeca(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public Task EncolarAsync(
        IDomainEvent evento,
        int aggregateVersion = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var mensaje = MapeadorEventosIntegracion.Mapear(evento, aggregateVersion);

        return _publishEndpoint.Publish(mensaje, cancellationToken);
    }
}

/// <summary>
/// Implementación de respaldo cuando la mensajería está deshabilitada. Falla
/// de forma explícita para no perder eventos silenciosamente.
/// </summary>
public sealed class OutboxBecaNoDisponible : IOutboxBeca
{
    public Task EncolarAsync(
        IDomainEvent evento,
        int aggregateVersion = 0,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "La mensajería no está habilitada; no es posible encolar eventos de integración.");
}
