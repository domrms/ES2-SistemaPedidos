using ES2_SistemaPedidos.Shared.Data;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.PersistenciaApi.Data;

public interface IClienteRepositorio
{
    Task<bool> ExisteAsync(int clienteId, CancellationToken tokenCancelamento);
}

public interface IProdutoRepositorio
{
    Task<bool> ExisteAsync(int produtoId, CancellationToken tokenCancelamento);
}

public interface IEventoRepositorio
{
    Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken tokenCancelamento);
}

public interface IPedidoStatusRepositorio
{
    Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId, CancellationToken tokenCancelamento);
}

public sealed class ClienteRepositorio(ApplicationDbContext contextoBanco) : IClienteRepositorio
{
    public Task<bool> ExisteAsync(int clienteId, CancellationToken tokenCancelamento)
    {
        return contextoBanco.Clientes.AsNoTracking().AnyAsync(cliente => cliente.Id == clienteId, tokenCancelamento);
    }
}

public sealed class ProdutoRepositorio(ApplicationDbContext contextoBanco) : IProdutoRepositorio
{
    public Task<bool> ExisteAsync(int produtoId, CancellationToken tokenCancelamento)
    {
        return contextoBanco.Produtos.AsNoTracking().AnyAsync(produto => produto.Id == produtoId, tokenCancelamento);
    }
}

public sealed class EventoRepositorio(ApplicationDbContext contextoBanco) : IEventoRepositorio
{
    public async Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken tokenCancelamento)
    {
        var eventos = await contextoBanco.Eventos
            .AsNoTracking()
            .OrderBy(evento => evento.SalvoEm)
            .Select(evento => new EventoDetalhado(
                evento.Id,
                evento.Cliente!.Nome,
                evento.Produto!.Nome,
                evento.EventoId,
                evento.DataHoraEvento,
                evento.SalvoEm))
            .ToListAsync(tokenCancelamento);

        return eventos.AsReadOnly();
    }
}

public sealed class PedidoStatusRepositorio(ApplicationDbContext contextoBanco) : IPedidoStatusRepositorio
{
    public async Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
        CancellationToken tokenCancelamento)
    {
        var pedido = await contextoBanco.Eventos
            .AsNoTracking()
            .Include(evento => evento.HistoricoStatus)
            .SingleOrDefaultAsync(evento => evento.Id == pedidoId, tokenCancelamento);

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