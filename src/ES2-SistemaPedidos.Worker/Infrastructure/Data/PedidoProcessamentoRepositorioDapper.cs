using Dapper;
using ES2_SistemaPedidos.Shared.Domain;
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

    public async Task<bool> IsMensagemProcessadaAsync(string mensagemId, CancellationToken tokenCancelamento)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM mensagens_processadas
                WHERE mensagem_id = @MensagemId
            );
            """;

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        return await conexao.ExecuteScalarAsync<bool>(new CommandDefinition(sql, new { MensagemId = mensagemId }, cancellationToken: tokenCancelamento));
    }

    public async Task<PedidoProcessamento?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento)
    {
        const string sql = """
            SELECT
                id,
                cliente_id AS ClienteId,
                valor_total AS ValorTotal,
                status
            FROM pedidos
            WHERE id = @PedidoId;
            """;

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        var registro = await conexao.QuerySingleOrDefaultAsync<PedidoProcessamentoRegistro>(
            new CommandDefinition(sql, new { PedidoId = pedidoId }, cancellationToken: tokenCancelamento));

        return registro is null
            ? null
            : new PedidoProcessamento(registro.Id, registro.ClienteId, registro.ValorTotal, (StatusPedido)registro.Status);
    }

    public async Task MarcarPedidoComoProcessandoAsync(Guid pedidoId, DateTimeOffset agora, CancellationToken tokenCancelamento)
    {
        const string sql = """
            UPDATE pedidos
            SET status = @StatusProcessando,
                processamento_iniciado_em = @Agora,
                atualizado_em = @Agora
            WHERE id = @PedidoId
              AND status = @StatusPendente;
            """;

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await conexao.ExecuteAsync(new CommandDefinition(sql, new
        {
            PedidoId = pedidoId,
            Agora = agora,
            StatusProcessando = (short)StatusPedido.Processando,
            StatusPendente = (short)StatusPedido.Pendente
        }, cancellationToken: tokenCancelamento));
    }

    public async Task RegistrarPedidoJaProcessadoAsync(string mensagemId, Guid pedidoId, string tipoMensagem, DateTimeOffset agora, CancellationToken tokenCancelamento)
    {
        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await using var transacao = await conexao.BeginTransactionAsync(tokenCancelamento);

        await RegistrarMensagemProcessadaAsync(
            conexao,
            transacao,
            mensagemId,
            pedidoId,
            tipoMensagem,
            "SUCESSO",
            agora,
            detalhesErro: null,
            tokenCancelamento);

        await transacao.CommitAsync(tokenCancelamento);
    }

    public async Task AprovarPedidoAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string motivo,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento)
    {
        const string sql = """
            UPDATE pedidos
            SET status = @StatusAprovado,
                motivo_aprovacao = @Motivo,
                concluido_em = @Agora,
                atualizado_em = @Agora
            WHERE id = @PedidoId
              AND status = @StatusProcessando;
            """;

        await ConcluirPedidoAsync(sql, pedidoId, mensagemId, tipoMensagem, motivo, agora, tokenCancelamento);
    }

    public async Task RejeitarPedidoAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string motivo,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento)
    {
        const string sql = """
            UPDATE pedidos
            SET status = @StatusRejeitado,
                motivo_rejeicao = @Motivo,
                concluido_em = @Agora,
                atualizado_em = @Agora
            WHERE id = @PedidoId
              AND status = @StatusProcessando;
            """;

        await ConcluirPedidoAsync(sql, pedidoId, mensagemId, tipoMensagem, motivo, agora, tokenCancelamento);
    }

    public async Task RegistrarFalhaAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string erro,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento)
    {
        const string sql = """
            UPDATE pedidos
            SET status = @StatusFalhou,
                mensagem_erro = @Erro,
                concluido_em = @Agora,
                atualizado_em = @Agora
            WHERE id = @PedidoId
              AND status <> @StatusFalhou;
            """;

        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await using var transacao = await conexao.BeginTransactionAsync(tokenCancelamento);

        await conexao.ExecuteAsync(new CommandDefinition(sql, new
        {
            PedidoId = pedidoId,
            Erro = erro,
            Agora = agora,
            StatusFalhou = (short)StatusPedido.Falhou
        }, transacao, cancellationToken: tokenCancelamento));

        await RegistrarMensagemProcessadaAsync(
            conexao,
            transacao,
            mensagemId,
            pedidoId,
            tipoMensagem,
            "FALHA",
            agora,
            erro,
            tokenCancelamento);

        await transacao.CommitAsync(tokenCancelamento);
    }

    private async Task ConcluirPedidoAsync(
        string sql,
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string motivo,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento)
    {
        await using var conexao = await AbrirConexaoAsync(tokenCancelamento);
        await using var transacao = await conexao.BeginTransactionAsync(tokenCancelamento);

        await conexao.ExecuteAsync(new CommandDefinition(sql, new
        {
            PedidoId = pedidoId,
            Motivo = motivo,
            Agora = agora,
            StatusProcessando = (short)StatusPedido.Processando,
            StatusAprovado = (short)StatusPedido.Aprovado,
            StatusRejeitado = (short)StatusPedido.Rejeitado
        }, transacao, cancellationToken: tokenCancelamento));

        await RegistrarMensagemProcessadaAsync(
            conexao,
            transacao,
            mensagemId,
            pedidoId,
            tipoMensagem,
            "SUCESSO",
            agora,
            detalhesErro: null,
            tokenCancelamento);

        await transacao.CommitAsync(tokenCancelamento);
    }

    private async Task<NpgsqlConnection> AbrirConexaoAsync(CancellationToken tokenCancelamento)
    {
        var conexao = new NpgsqlConnection(_stringConexao);
        await conexao.OpenAsync(tokenCancelamento);
        return conexao;
    }

    private static async Task RegistrarMensagemProcessadaAsync(
        NpgsqlConnection conexao,
        NpgsqlTransaction transacao,
        string mensagemId,
        Guid pedidoId,
        string tipoMensagem,
        string status,
        DateTimeOffset processadaEm,
        string? detalhesErro,
        CancellationToken tokenCancelamento)
    {
        const string sql = """
            INSERT INTO mensagens_processadas (
                mensagem_id,
                pedido_id,
                tipo_mensagem,
                status,
                processada_em,
                detalhes_erro
            )
            VALUES (
                @MensagemId,
                @PedidoId,
                @TipoMensagem,
                @Status,
                @ProcessadaEm,
                @DetalhesErro
            );
            """;

        await conexao.ExecuteAsync(new CommandDefinition(sql, new
        {
            MensagemId = mensagemId,
            PedidoId = pedidoId,
            TipoMensagem = tipoMensagem,
            Status = status,
            ProcessadaEm = processadaEm,
            DetalhesErro = detalhesErro
        }, transacao, cancellationToken: tokenCancelamento));
    }

    private sealed record PedidoProcessamentoRegistro(
        Guid Id,
        string ClienteId,
        decimal ValorTotal,
        short Status);
}
