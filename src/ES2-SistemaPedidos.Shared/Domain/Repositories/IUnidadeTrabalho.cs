namespace ES2_SistemaPedidos.Shared.Domain.Repositories;

public interface IUnidadeTrabalho
{
    Task SaveChangesAsync(CancellationToken tokenCancelamento);
}
