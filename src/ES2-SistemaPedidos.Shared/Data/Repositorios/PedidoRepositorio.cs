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