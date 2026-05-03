namespace ES2_SistemaPedidos.Shared.Domain.Repositories;

public interface IClienteRepositorio
{
    Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento);
}

public interface IProdutoRepositorio
{
    Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento);
}
