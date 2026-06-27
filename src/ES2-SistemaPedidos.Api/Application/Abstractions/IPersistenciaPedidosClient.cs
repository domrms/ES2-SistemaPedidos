namespace ES2_SistemaPedidos.Api.Application.Abstractions;

public interface IPersistenciaPedidosClient
{
    Task<bool> ExisteClienteAsync(int clienteId, CancellationToken cancellationToken);

    Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RespostaEventoDetalhado>> ListarEventosAsync(
        CancellationToken cancellationToken);

    Task<RespostaHistoricoPedido?> ObterHistoricoAsync(long pedidoId,
        CancellationToken cancellationToken);
}
