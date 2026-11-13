using Ascenso.Programas.Application.Donantes.Abstracciones;
using MassTransit;

namespace Ascenso.Programas.Infrastructure.Mensajeria;

/// <summary>
/// Adaptador de mensajería sobre MassTransit que encola eventos de integración en el Outbox transaccional.
/// </summary>
public sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public async Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken = default)
        where TEvento : class
    {
        ArgumentNullException.ThrowIfNull(evento);
        await _publishEndpoint.Publish(evento, cancellationToken);
    }
}

