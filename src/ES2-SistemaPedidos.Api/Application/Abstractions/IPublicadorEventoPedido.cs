using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Application.Abstractions;

public interface IPublicadorEventoPedido
{
    Task PublishPedidoCriadoAsync(EventoPedidoCriado eventoPedidoCriado, CancellationToken tokenCancelamento);
}
