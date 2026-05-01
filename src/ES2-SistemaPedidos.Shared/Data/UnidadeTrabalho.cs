using ES2_SistemaPedidos.Shared.Domain.Repositories;

namespace ES2_SistemaPedidos.Shared.Data;

public sealed class UnidadeTrabalho(ApplicationDbContext contextoBanco) : IUnidadeTrabalho
{
    public async Task SaveChangesAsync(CancellationToken tokenCancelamento)
    {
        await contextoBanco.SaveChangesAsync(tokenCancelamento);
    }
}
