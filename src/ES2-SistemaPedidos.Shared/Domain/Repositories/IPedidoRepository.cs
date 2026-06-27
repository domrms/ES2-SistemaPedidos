namespace ES2_SistemaPedidos.Shared.Domain.Repositories;

public interface IClienteRepositorio
{
    Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento);
}

public interface IProdutoRepositorio
{
    Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento);
}

public interface IEventoRepositorio
{
    Task<IReadOnlyCollection<EventoClienteDetalhado>> ListarTodosEventosAsync(CancellationToken tokenCancelamento);
}

public interface IPedidoStatusRepositorio
{
    Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId, CancellationToken tokenCancelamento);
}

public sealed record HistoricoPedidoDetalhado(
    long PedidoId,
    string EventoId,
    IReadOnlyCollection<TransicaoPedidoDetalhada> Transicoes);

public sealed record TransicaoPedidoDetalhada(
    long Id,
    EstadoPedido Status,
    DateTimeOffset RegistradoEm,
    string? Detalhe);

public sealed record EventoClienteDetalhado(
    long Id,
    string NomeCliente,
    string NomeProduto,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);
