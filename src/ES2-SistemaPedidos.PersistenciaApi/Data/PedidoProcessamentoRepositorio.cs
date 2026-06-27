using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ES2_SistemaPedidos.PersistenciaApi.Data;

public interface IPedidoProcessamentoRepositorio
{
    Task RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido, CancellationToken cancellationToken);

    Task RegistrarErroAsync(RequisicaoProcessamentoPedido pedido, string detalhe,
        CancellationToken cancellationToken);
}

public sealed class PedidoProcessamentoRepositorio(ApplicationDbContext dbContext)
    : IPedidoProcessamentoRepositorio
{
    public Task RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido, CancellationToken cancellationToken)
    {
        return RegistrarComStatusAsync(pedido,
        [
            (EstadoPedido.Recebido, pedido.DataHoraEvento, null),
            (EstadoPedido.Processando, pedido.SalvoEm, null),
            (EstadoPedido.Concluido, pedido.SalvoEm, null)
        ], cancellationToken);
    }

    public Task RegistrarErroAsync(RequisicaoProcessamentoPedido pedido, string detalhe,
        CancellationToken cancellationToken)
    {
        return RegistrarComStatusAsync(pedido,
        [
            (EstadoPedido.Recebido, pedido.DataHoraEvento, null),
            (EstadoPedido.Erro, pedido.SalvoEm, detalhe)
        ], cancellationToken);
    }

    private async Task RegistrarComStatusAsync(RequisicaoProcessamentoPedido requisicao,
        IReadOnlyCollection<(EstadoPedido Status, DateTimeOffset RegistradoEm, string? Detalhe)> transicoes,
        CancellationToken cancellationToken)
    {
        try
        {
            await PersistirAsync(requisicao, transicoes, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                                                {
                                                    SqlState: PostgresErrorCodes.UniqueViolation
                                                })
        {
            // Uma entrega concorrente pode inserir o mesmo evento entre a consulta e o SaveChanges.
            // Limpa o estado local e repete para complementar somente os status ainda ausentes.
            dbContext.ChangeTracker.Clear();
            await PersistirAsync(requisicao, transicoes, cancellationToken);
        }
    }

    private async Task PersistirAsync(RequisicaoProcessamentoPedido requisicao,
        IReadOnlyCollection<(EstadoPedido Status, DateTimeOffset RegistradoEm, string? Detalhe)> transicoes,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pedido = await dbContext.Eventos
            .Include(evento => evento.HistoricoStatus)
            .SingleOrDefaultAsync(evento => evento.EventoId == requisicao.EventoId, cancellationToken);

        if (pedido is null)
        {
            pedido = new EventoCliente(
                0,
                requisicao.ClienteId,
                requisicao.ProdutoId,
                requisicao.EventoId,
                requisicao.DataHoraEvento.ToUniversalTime(),
                requisicao.SalvoEm.ToUniversalTime());
            dbContext.Eventos.Add(pedido);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var statusRegistrados = pedido.HistoricoStatus
            .Select(status => status.Status)
            .ToHashSet();

        foreach (var transicao in transicoes.Where(transicao => !statusRegistrados.Contains(transicao.Status)))
            dbContext.PedidoStatus.Add(new PedidoStatus(
                0,
                pedido.Id,
                transicao.Status,
                transicao.RegistradoEm.ToUniversalTime(),
                transicao.Detalhe));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
