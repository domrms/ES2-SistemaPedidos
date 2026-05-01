using ES2_SistemaPedidos.Shared.Domain;
using ES2_SistemaPedidos.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Shared.Data.Repositorios;

public sealed class PedidoRepositorio(ApplicationDbContext contextoBanco) : IPedidoRepositorio
{
    public async Task AddPedidoAsync(Pedido pedido, CancellationToken tokenCancelamento)
    {
        await contextoBanco.Pedidos.AddAsync(pedido, tokenCancelamento);
    }

    public async Task<Pedido?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento)
    {
        return await contextoBanco.Pedidos
            .Include(pedido => pedido.Itens)
            .FirstOrDefaultAsync(pedido => pedido.Id == pedidoId, tokenCancelamento);
    }

    public async Task<IReadOnlyCollection<Pedido>> ListPedidosAsync(
        string clienteId,
        StatusPedido? status,
        int pular,
        int quantidade,
        DateOnly? dataDe,
        DateOnly? dataAte,
        CancellationToken tokenCancelamento)
    {
        return await CriarConsultaPedidos(clienteId, status, dataDe, dataAte)
            .AsNoTracking()
            .Include(pedido => pedido.Itens)
            .OrderByDescending(pedido => pedido.CriadoEm)
            .Skip(pular)
            .Take(quantidade)
            .ToListAsync(tokenCancelamento);
    }

    public async Task<int> CountPedidosAsync(
        string clienteId,
        StatusPedido? status,
        DateOnly? dataDe,
        DateOnly? dataAte,
        CancellationToken tokenCancelamento)
    {
        return await CriarConsultaPedidos(clienteId, status, dataDe, dataAte)
            .CountAsync(tokenCancelamento);
    }

    public async Task<bool> IsMensagemProcessadaAsync(string mensagemId, CancellationToken tokenCancelamento)
    {
        return await contextoBanco.MensagensProcessadas
            .AnyAsync(mensagem => mensagem.MensagemId == mensagemId, tokenCancelamento);
    }

    public async Task AddMensagemProcessadaAsync(MensagemProcessada mensagemProcessada, CancellationToken tokenCancelamento)
    {
        await contextoBanco.MensagensProcessadas.AddAsync(mensagemProcessada, tokenCancelamento);
    }

    private IQueryable<Pedido> CriarConsultaPedidos(
        string clienteId,
        StatusPedido? status,
        DateOnly? dataDe,
        DateOnly? dataAte)
    {
        var consulta = contextoBanco.Pedidos
            .Where(pedido => pedido.ClienteId == clienteId.Trim());

        if (status.HasValue)
        {
            consulta = consulta.Where(pedido => pedido.Status == status.Value);
        }

        if (dataDe.HasValue)
        {
            var inicio = new DateTimeOffset(dataDe.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            consulta = consulta.Where(pedido => pedido.CriadoEm >= inicio);
        }

        if (dataAte.HasValue)
        {
            var fim = new DateTimeOffset(dataAte.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            consulta = consulta.Where(pedido => pedido.CriadoEm <= fim);
        }

        return consulta;
    }
}
