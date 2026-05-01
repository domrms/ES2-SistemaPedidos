using Dapper;
using ES2_SistemaPedidos.Worker.Application.Abstractions;
using ES2_SistemaPedidos.Worker.Application.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ES2_SistemaPedidos.Worker.Infrastructure.Data;

public sealed class PedidoProcessamentoRepositorioDapper(IConfiguration configuracao) : IPedidoProcessamentoRepositorio
{
    private readonly string _stringConexao = configuracao.GetConnectionString("BancoPedidos")
        ?? configuracao["DATABASE_URL"]
        ?? "Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev";

    public async Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento)
    {
        const string sql = """
            INSERT INTO eventos (
                cliente_id,
                evento_id,
                data_hora_evento,
                salvo_em
            )
            VALUES (
                @ClienteId,
                @EventoId,
                @DataHoraEvento,
                @SalvoEm
            )
            ON CONFLICT (evento_id) DO NOTHING;
            """;

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await conexao.ExecuteAsync(new CommandDefinition(sql, evento, cancellationToken: tokenCancelamento));
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync(CancellationToken tokenCancelamento)
    {
        var conexao = new NpgsqlConnection(_stringConexao);
        await conexao.OpenAsync(tokenCancelamento);
        return conexao;
    }
}
