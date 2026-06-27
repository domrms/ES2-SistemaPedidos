using System.Diagnostics.CodeAnalysis;
using Dapper;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Data;

[ExcludeFromCodeCoverage]
public sealed class PedidoProcessamentoRepositoryDapper(IConfiguration configuracao) : IPedidoProcessamentoRepository
{
    private const string InserirEventoSql = """
                                                    INSERT INTO eventos (
                                                        cliente_id,
                                                        produto_id,
                                                        evento_id,
                                                        data_hora_evento,
                                                        salvo_em
                                                    )
                                                    VALUES (
                                                        @ClienteId,
                                                        @ProdutoId,
                                                        @EventoId,
                                                        @DataHoraEvento,
                                                        @SalvoEm
                                                    )
                                                    ON CONFLICT (evento_id) DO NOTHING
                                                    RETURNING id;
                                        """;

    private const string ConsultarPedidoSql = "SELECT id FROM eventos WHERE evento_id = @EventoId;";

    private const string InserirStatusSql = """
                                                   INSERT INTO pedido_status (pedido_id, status, registrado_em, detalhe)
                                                   VALUES (@PedidoId, @Status, @RegistradoEm, @Detalhe)
                                                   ON CONFLICT (pedido_id, status) DO NOTHING;
                                           """;

    private readonly string _stringConexao = configuracao.GetConnectionString("BancoPedidos")
                                             ?? configuracao["DATABASE_URL"]
                                             ?? throw new InvalidOperationException(
                                                 "String de conexao nao configurada. Defina ConnectionStrings:BancoPedidos ou DATABASE_URL.");

    public async Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento)
    {
        await RegistrarComStatusAsync(evento,
        [
            ("Recebido", evento.DataHoraEvento, null),
            ("Processando", evento.SalvoEm, null),
            ("Concluido", evento.SalvoEm, null)
        ], tokenCancelamento);
    }

    public async Task RegistrarErroAsync(EventoProcessamento evento, string detalhe,
        CancellationToken tokenCancelamento)
    {
        await RegistrarComStatusAsync(evento,
        [
            ("Recebido", evento.DataHoraEvento, null),
            ("Erro", evento.SalvoEm, detalhe)
        ], tokenCancelamento);
    }

    private async Task RegistrarComStatusAsync(EventoProcessamento evento,
        IReadOnlyCollection<(string Status, DateTimeOffset RegistradoEm, string? Detalhe)> status,
        CancellationToken tokenCancelamento)
    {
        var parametros = evento with
        {
            DataHoraEvento = evento.DataHoraEvento.ToUniversalTime(),
            SalvoEm = evento.SalvoEm.ToUniversalTime()
        };

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await using var transacao = await conexao.BeginTransactionAsync(tokenCancelamento);
        try
        {
            var pedidoId = await conexao.ExecuteScalarAsync<long?>(new CommandDefinition(
                InserirEventoSql,
                parametros,
                transacao,
                cancellationToken: tokenCancelamento));

            pedidoId ??= await conexao.ExecuteScalarAsync<long>(new CommandDefinition(
                ConsultarPedidoSql,
                new { evento.EventoId },
                transacao,
                cancellationToken: tokenCancelamento));

            foreach (var transicao in status)
                await RegistrarStatusAsync(conexao, transacao, pedidoId.Value, transicao.Status,
                    transicao.RegistradoEm.ToUniversalTime(), transicao.Detalhe, tokenCancelamento);

            await transacao.CommitAsync(tokenCancelamento);
        }
        catch
        {
            await transacao.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task RegistrarStatusAsync(NpgsqlConnection conexao, NpgsqlTransaction transacao,
        long pedidoId, string status, DateTimeOffset registradoEm, string? detalhe,
        CancellationToken tokenCancelamento)
    {
        await conexao.ExecuteAsync(new CommandDefinition(
            InserirStatusSql,
            new { PedidoId = pedidoId, Status = status, RegistradoEm = registradoEm, Detalhe = detalhe },
            transacao,
            cancellationToken: tokenCancelamento));
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync(CancellationToken tokenCancelamento)
    {
        var conexao = new NpgsqlConnection(_stringConexao);
        await conexao.OpenAsync(tokenCancelamento);
        return conexao;
    }
}
