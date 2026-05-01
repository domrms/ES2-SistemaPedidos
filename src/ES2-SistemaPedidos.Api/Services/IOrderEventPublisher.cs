using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Services;

public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken);
}
