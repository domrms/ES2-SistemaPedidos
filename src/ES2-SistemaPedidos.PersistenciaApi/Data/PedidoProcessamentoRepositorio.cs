using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ES2_SistemaPedidos.PersistenciaApi.Data;

public interface IPedidoProcessamentoRepositorio
{
    Task RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido, CancellationToken tokenCancelamento);

    Task RegistrarErroAsync(RequisicaoProcessamentoPedido pedido, string detalhe,
        CancellationToken tokenCancelamento);
}

public sealed class PedidoProcessamentoRepositorio(ApplicationDbContext contextoBanco)
    : IPedidoProcessamentoRepositorio
{
    public Task RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido, CancellationToken tokenCancelamento)
    {
        return RegistrarComStatusAsync(pedido,
        [
            (EstadoPedido.Recebido, pedido.DataHoraEvento, null),
            (EstadoPedido.Processando, pedido.SalvoEm, null),
            (EstadoPedido.Concluido, pedido.SalvoEm, null)
        ], tokenCancelamento);
    }

    public Task RegistrarErroAsync(RequisicaoProcessamentoPedido pedido, string detalhe,
        CancellationToken tokenCancelamento)
    {
        return RegistrarComStatusAsync(pedido,
        [
            (EstadoPedido.Recebido, pedido.DataHoraEvento, null),
            (EstadoPedido.Erro, pedido.SalvoEm, detalhe)
        ], tokenCancelamento);
    }

    private async Task RegistrarComStatusAsync(RequisicaoProcessamentoPedido requisicao,
        IReadOnlyCollection<(EstadoPedido Status, DateTimeOffset RegistradoEm, string? Detalhe)> transicoes,
        CancellationToken tokenCancelamento)
    {
        try
        {
            await PersistirAsync(requisicao, transicoes, tokenCancelamento);
        }
        catch (DbUpdateException excecao) when (excecao.InnerException is PostgresException
                                                {
                                                    SqlState: PostgresErrorCodes.UniqueViolation
                                                })
        {
            // Uma entrega concorrente pode inserir o mesmo evento entre a consulta e o SaveChanges.
            // Limpa o estado local e repete para complementar somente os status ainda ausentes.
            contextoBanco.ChangeTracker.Clear();
            await PersistirAsync(requisicao, transicoes, tokenCancelamento);
        }
    }

    private async Task PersistirAsync(RequisicaoProcessamentoPedido requisicao,
        IReadOnlyCollection<(EstadoPedido Status, DateTimeOffset RegistradoEm, string? Detalhe)> transicoes,
        CancellationToken tokenCancelamento)
    {
        await using var transacao = await contextoBanco.Database.BeginTransactionAsync(tokenCancelamento);

        var pedido = await contextoBanco.Eventos
            .Include(evento => evento.HistoricoStatus)
            .SingleOrDefaultAsync(evento => evento.EventoId == requisicao.EventoId, tokenCancelamento);

        if (pedido is null)
        {
            pedido = new EventoCliente(
                0,
                requisicao.ClienteId,
                requisicao.ProdutoId,
                requisicao.EventoId,
                requisicao.DataHoraEvento.ToUniversalTime(),
                requisicao.SalvoEm.ToUniversalTime());
            contextoBanco.Eventos.Add(pedido);
            await contextoBanco.SaveChangesAsync(tokenCancelamento);
        }

        var statusRegistrados = pedido.HistoricoStatus
            .Select(status => status.Status)
            .ToHashSet();

        foreach (var transicao in transicoes.Where(transicao => !statusRegistrados.Contains(transicao.Status)))
            contextoBanco.PedidoStatus.Add(new PedidoStatus(
                0,
                pedido.Id,
                transicao.Status,
                transicao.RegistradoEm.ToUniversalTime(),
                transicao.Detalhe));

        await contextoBanco.SaveChangesAsync(tokenCancelamento);
        await transacao.CommitAsync(tokenCancelamento);
    }
}