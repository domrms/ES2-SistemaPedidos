namespace ES2_SistemaPedidos.Api.Application.Abstractions;

public interface IPersistenciaPedidosClient
{
    Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento);

    Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento);

    Task<IReadOnlyCollection<RespostaEventoDetalhado>> ListarEventosAsync(
        CancellationToken tokenCancelamento);

    Task<RespostaHistoricoPedido?> ObterHistoricoAsync(long pedidoId,
        CancellationToken tokenCancelamento);
}