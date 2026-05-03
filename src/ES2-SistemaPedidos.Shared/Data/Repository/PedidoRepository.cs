using ES2_SistemaPedidos.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data.Repositorios;

public sealed class ClienteRepositorio(ApplicationDbContext contextoBanco) : IClienteRepositorio
{
    public async Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento)
    {
        return await contextoBanco.Clientes
            .AsNoTracking()
            .AnyAsync(cliente => cliente.Id == clienteId, tokenCancelamento);
    }
}

public sealed class ProdutoRepositorio(ApplicationDbContext contextoBanco) : IProdutoRepositorio
{
    public async Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento)
    {
        return await contextoBanco.Produtos
            .AsNoTracking()
            .AnyAsync(produto => produto.Id == produtoId, tokenCancelamento);
    }
}
