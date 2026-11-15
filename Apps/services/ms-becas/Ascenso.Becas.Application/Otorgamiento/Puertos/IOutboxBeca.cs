using Ascenso.Becas.Domain.Comun;

namespace Ascenso.Becas.Application.Otorgamiento.Puertos;

/// <summary>
/// Cola transaccional de eventos de integración. Los eventos se encolan antes
/// de confirmar el cambio de estado y solo se publican cuando la transacción
/// local confirma.
/// </summary>
public interface IOutboxBeca
{
    Task EncolarAsync(IDomainEvent evento, CancellationToken cancellationToken = default);
}
