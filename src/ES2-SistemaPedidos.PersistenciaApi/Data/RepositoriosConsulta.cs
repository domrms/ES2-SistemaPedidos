using ES2_SistemaPedidos.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.PersistenciaApi.Data;

public interface IClienteRepositorio
{
    Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken);
}

public interface IProdutoRepositorio
{
    Task<bool> ExisteAsync(int produtoId, CancellationToken cancellationToken);
}

public interface IEventoRepositorio
{
    Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken cancellationToken);
}

public interface IPedidoStatusRepositorio
{
    Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId, CancellationToken cancellationToken);
}

public sealed class ClienteRepositorio(ApplicationDbContext dbContext) : IClienteRepositorio
{
    public Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken)
    {
        return dbContext.Clientes.AsNoTracking().AnyAsync(cliente => cliente.Id == clienteId, cancellationToken);
    }
}

public sealed class ProdutoRepositorio(ApplicationDbContext dbContext) : IProdutoRepositorio
{
    public Task<bool> ExisteAsync(int produtoId, CancellationToken cancellationToken)
    {
        return dbContext.Produtos.AsNoTracking().AnyAsync(produto => produto.Id == produtoId, cancellationToken);
    }
}

public sealed class EventoRepositorio(ApplicationDbContext dbContext) : IEventoRepositorio
{
    public async Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken cancellationToken)
    {
        var eventos = await dbContext.Eventos
            .AsNoTracking()
            .OrderBy(evento => evento.SalvoEm)
            .Select(evento => new EventoDetalhado(
                evento.Id,
                evento.Cliente!.Nome,
                evento.Produto!.Nome,
                evento.EventoId,
                evento.DataHoraEvento,
                evento.SalvoEm))
            .ToListAsync(cancellationToken);

        return eventos.AsReadOnly();
    }
}

public sealed class PedidoStatusRepositorio(ApplicationDbContext dbContext) : IPedidoStatusRepositorio
{
    public async Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
        CancellationToken cancellationToken)
    {
        var pedido = await dbContext.Eventos
            .AsNoTracking()
            .Include(evento => evento.HistoricoStatus)
            .SingleOrDefaultAsync(evento => evento.Id == pedidoId, cancellationToken);

        if (pedido is null) return null;

        var historico = pedido.HistoricoStatus
            .OrderBy(status => status.Id)
            .Select(status => new TransicaoPedidoDetalhada(
                status.Id, status.Status, status.RegistradoEm, status.Detalhe))
            .ToList()
            .AsReadOnly();

        return new HistoricoPedidoDetalhado(pedido.Id, pedido.EventoId, historico);
    }
}
