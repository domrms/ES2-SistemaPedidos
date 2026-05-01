using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Services;

public interface IPublicadorEventoPedido
{
    Task PublishPedidoCriadoAsync(EventoPedidoCriado eventoPedidoCriado, CancellationToken tokenCancelamento);
}
