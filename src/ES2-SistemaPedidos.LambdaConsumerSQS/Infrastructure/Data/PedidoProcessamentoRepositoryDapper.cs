using Dapper;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Data;

public sealed class PedidoProcessamentoRepositoryDapper(IConfiguration configuracao) : IPedidoProcessamentoRepository
{
    private readonly string _stringConexao = configuracao.GetConnectionString("BancoPedidos")
                                             ?? configuracao["DATABASE_URL"]
                                             ?? throw new InvalidOperationException(
                                                 "String de conexao nao configurada. Defina ConnectionStrings:BancoPedidos ou DATABASE_URL.");

    public async Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento)
    {
        const string sql = """
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
                           ON CONFLICT (evento_id) DO NOTHING;
                           """;

        var parametros = evento with
        {
            DataHoraEvento = evento.DataHoraEvento.ToUniversalTime(),
            SalvoEm = evento.SalvoEm.ToUniversalTime()
        };

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await conexao.ExecuteAsync(new CommandDefinition(sql, parametros, cancellationToken: tokenCancelamento));
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync(CancellationToken tokenCancelamento)
    {
        var conexao = new NpgsqlConnection(_stringConexao);
        await conexao.OpenAsync(tokenCancelamento);
        return conexao;
    }
}