namespace ES2_SistemaPedidos.Shared.Domain.Repositories;

public interface IPedidoRepositorio
{
    Task AddPedidoAsync(Pedido pedido, CancellationToken tokenCancelamento);

    Task<Pedido?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento);

    Task<IReadOnlyCollection<Pedido>> ListPedidosAsync(
        string clienteId,
        StatusPedido? status,
        int pular,
        int quantidade,
        DateOnly? dataDe,
        DateOnly? dataAte,
        CancellationToken tokenCancelamento);

    Task<int> CountPedidosAsync(
        string clienteId,
        StatusPedido? status,
        DateOnly? dataDe,
        DateOnly? dataAte,
        CancellationToken tokenCancelamento);

    Task<bool> IsMensagemProcessadaAsync(string mensagemId, CancellationToken tokenCancelamento);

    Task AddMensagemProcessadaAsync(MensagemProcessada mensagemProcessada, CancellationToken tokenCancelamento);
}
