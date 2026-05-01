using System.Text.Json;
using System.Text.Json.Serialization;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain;
using ES2_SistemaPedidos.Worker.Application.Abstractions;
using ES2_SistemaPedidos.Worker.Configuracoes;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.Worker.Application.Services;

public sealed class ProcessadorPedido(
    IPedidoProcessamentoRepositorio pedidoRepositorio,
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
        if (await pedidoRepositorio.IsMensagemProcessadaAsync(mensagemSqsId, tokenCancelamento))
        {
            registrador.LogInformation("Mensagem {MensagemId} ja processada; ignorando duplicidade", mensagemSqsId);
            return true;
        }

        var eventoPedidoCriado = JsonSerializer.Deserialize<EventoPedidoCriado>(corpoMensagem, OpcoesJson);
        if (eventoPedidoCriado is null || eventoPedidoCriado.PedidoId == Guid.Empty)
        {
            registrador.LogWarning("Mensagem {MensagemId} possui payload invalido", mensagemSqsId);
            return false;
        }

        var pedido = await pedidoRepositorio.GetPedidoPorIdAsync(eventoPedidoCriado.PedidoId, tokenCancelamento);
        if (pedido is null)
        {
            registrador.LogWarning("Pedido {PedidoId} nao encontrado para mensagem {MensagemId}", eventoPedidoCriado.PedidoId, mensagemSqsId);
            return false;
        }

        if (pedido.Status is StatusPedido.Aprovado or StatusPedido.Rejeitado or StatusPedido.Falhou)
        {
            await pedidoRepositorio.RegistrarPedidoJaProcessadoAsync(
                mensagemSqsId,
                pedido.Id,
                eventoPedidoCriado.TipoEvento,
                provedorTempo.GetUtcNow(),
                tokenCancelamento);
            return true;
        }

        try
        {
            var agora = provedorTempo.GetUtcNow();
            if (pedido.Status == StatusPedido.Pendente)
            {
                await pedidoRepositorio.MarcarPedidoComoProcessandoAsync(pedido.Id, agora, tokenCancelamento);
            }

            agora = provedorTempo.GetUtcNow();
            if (pedido.ValorTotal < opcoes.ValorLimiteAprovacao)
            {
                var motivo = $"Valor abaixo do limite ({pedido.ValorTotal:0.00} < {opcoes.ValorLimiteAprovacao:0.00})";
                await pedidoRepositorio.AprovarPedidoAsync(
                    pedido.Id,
                    mensagemSqsId,
                    eventoPedidoCriado.TipoEvento,
                    motivo,
                    agora,
                    tokenCancelamento);
            }
            else
            {
                var motivo = $"Valor igual ou acima do limite ({pedido.ValorTotal:0.00} >= {opcoes.ValorLimiteAprovacao:0.00})";
                await pedidoRepositorio.RejeitarPedidoAsync(
                    pedido.Id,
                    mensagemSqsId,
                    eventoPedidoCriado.TipoEvento,
                    motivo,
                    agora,
                    tokenCancelamento);
            }

            return true;
        }
        catch (Exception excecao)
        {
            await pedidoRepositorio.RegistrarFalhaAsync(
                pedido.Id,
                mensagemSqsId,
                eventoPedidoCriado.TipoEvento,
                excecao.Message,
                provedorTempo.GetUtcNow(),
                tokenCancelamento);
            throw;
        }
    }
}
