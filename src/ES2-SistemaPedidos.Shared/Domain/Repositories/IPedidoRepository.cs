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

public sealed record EventoClienteDetalhado(
    long Id,
    string NomeCliente,
    string NomeProduto,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);

