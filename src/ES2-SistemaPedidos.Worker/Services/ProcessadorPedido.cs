using System.Text.Json;
using System.Text.Json.Serialization;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain;
using ES2_SistemaPedidos.Shared.Domain.Repositories;
using ES2_SistemaPedidos.Worker.Configuracoes;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.Worker.Services;

public sealed class ProcessadorPedido(
    IPedidoRepositorio pedidoRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    OpcoesProcessamentoPedidos opcoes,
    TimeProvider provedorTempo,
    ILogger<ProcessadorPedido> registrador)
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<bool> ProcessMessageAsync(string mensagemSqsId, string corpoMensagem, CancellationToken tokenCancelamento)
    {
        var mensagemId = mensagemSqsId;
        var payload = corpoMensagem;

        if (await pedidoRepositorio.IsMensagemProcessadaAsync(mensagemId, tokenCancelamento))
        {
            registrador.LogInformation("Mensagem {MensagemId} ja processada; ignorando duplicidade", mensagemId);
            return true;
        }

        var eventoPedidoCriado = JsonSerializer.Deserialize<EventoPedidoCriado>(payload, OpcoesJson);
        if (eventoPedidoCriado is null || eventoPedidoCriado.PedidoId == Guid.Empty)
        {
            registrador.LogWarning("Mensagem {MensagemId} possui payload invalido", mensagemId);
            return false;
        }

        var pedido = await pedidoRepositorio.GetPedidoPorIdAsync(eventoPedidoCriado.PedidoId, tokenCancelamento);
        if (pedido is null)
        {
            registrador.LogWarning("Pedido {PedidoId} nao encontrado para mensagem {MensagemId}", eventoPedidoCriado.PedidoId, mensagemId);
            return false;
        }

        if (pedido.Status is StatusPedido.Aprovado or StatusPedido.Rejeitado or StatusPedido.Falhou)
        {
            await pedidoRepositorio.AddMensagemProcessadaAsync(
                new MensagemProcessada(mensagemId, pedido.Id, eventoPedidoCriado.TipoEvento, "SUCESSO", provedorTempo.GetUtcNow()),
                tokenCancelamento);
            await unidadeTrabalho.SaveChangesAsync(tokenCancelamento);
            return true;
        }

        try
        {
            var agora = provedorTempo.GetUtcNow();
            if (pedido.Status == StatusPedido.Pendente)
            {
                pedido.MarkAsProcessando(agora);
                await unidadeTrabalho.SaveChangesAsync(tokenCancelamento);
            }

            if (pedido.ValorTotal < opcoes.ValorLimiteAprovacao)
            {
                var motivo = $"Valor abaixo do limite ({pedido.ValorTotal:0.00} < {opcoes.ValorLimiteAprovacao:0.00})";
                pedido.MarkAsAprovado(motivo, provedorTempo.GetUtcNow());
            }
            else
            {
                var motivo = $"Valor igual ou acima do limite ({pedido.ValorTotal:0.00} >= {opcoes.ValorLimiteAprovacao:0.00})";
                pedido.MarkAsRejeitado(motivo, provedorTempo.GetUtcNow());
            }

            await pedidoRepositorio.AddMensagemProcessadaAsync(
                new MensagemProcessada(mensagemId, pedido.Id, eventoPedidoCriado.TipoEvento, "SUCESSO", provedorTempo.GetUtcNow()),
                tokenCancelamento);
            await unidadeTrabalho.SaveChangesAsync(tokenCancelamento);

            return true;
        }
        catch (Exception excecao)
        {
            pedido.MarkAsFalhou(excecao.Message, provedorTempo.GetUtcNow());
            await pedidoRepositorio.AddMensagemProcessadaAsync(
                new MensagemProcessada(mensagemId, pedido.Id, eventoPedidoCriado.TipoEvento, "FALHA", provedorTempo.GetUtcNow(), excecao.Message),
                tokenCancelamento);
            await unidadeTrabalho.SaveChangesAsync(tokenCancelamento);
            throw;
        }
    }
}
