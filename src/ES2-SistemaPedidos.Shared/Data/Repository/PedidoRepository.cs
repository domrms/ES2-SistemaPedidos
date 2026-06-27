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

public sealed class EventoRepositorio(ApplicationDbContext contextoBanco) : IEventoRepositorio
{
    public async Task<IReadOnlyCollection<EventoClienteDetalhado>> ListarTodosEventosAsync(
        CancellationToken tokenCancelamento)
    {
        var eventos = await contextoBanco.Eventos
            .Include(e => e.Cliente)
            .Include(e => e.Produto)
            .AsNoTracking()
            .OrderBy(e => e.SalvoEm)
            .Select(e => new EventoClienteDetalhado(
                e.Id,
                e.Cliente!.Nome,
                e.Produto!.Nome,
                e.EventoId,
                e.DataHoraEvento,
                e.SalvoEm))
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

        var transicoes = pedido.HistoricoStatus
            .OrderBy(status => status.Id)
            .Select(status => new TransicaoPedidoDetalhada(
                status.Id,
                status.Status,
                status.RegistradoEm,
                status.Detalhe))
            .ToList()
            .AsReadOnly();

        return new HistoricoPedidoDetalhado(pedido.Id, pedido.EventoId, transicoes);
    }
}
