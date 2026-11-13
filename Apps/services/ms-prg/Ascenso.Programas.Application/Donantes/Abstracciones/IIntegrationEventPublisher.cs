namespace Ascenso.Programas.Application.Donantes.Abstracciones;

/// <summary>
/// Puerto para publicar eventos de integración en el bus de mensajería (respaldado por Transactional Outbox).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublicarAsync<TEvento>(TEvento evento, CancellationToken cancellationToken = default)
        where TEvento : class;
}

