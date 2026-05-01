using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain;
using ES2_SistemaPedidos.Shared.Domain.Repositories;
using ES2_SistemaPedidos.Worker.Configuracoes;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.Worker.Services;

public sealed class ProcessadorPedido(
    IPedidoRepositorio pedidoRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAmazonSimpleNotificationService sns,
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
        var envelope = GetEnvelopeSns(corpoMensagem);
        var mensagemId = envelope?.MessageId ?? mensagemSqsId;
        var payload = envelope?.Message ?? corpoMensagem;

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
                await PublicarEventoAsync(ToEventoPedidoAprovado(pedido, eventoPedidoCriado, motivo), tokenCancelamento);
            }
            else
            {
                var motivo = $"Valor igual ou acima do limite ({pedido.ValorTotal:0.00} >= {opcoes.ValorLimiteAprovacao:0.00})";
                pedido.MarkAsRejeitado(motivo, provedorTempo.GetUtcNow());
                await PublicarEventoAsync(ToEventoPedidoRejeitado(pedido, eventoPedidoCriado, motivo), tokenCancelamento);
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
            await PublicarEventoAsync(ToEventoProcessamentoFalhou(pedido, eventoPedidoCriado, excecao), tokenCancelamento);
            await pedidoRepositorio.AddMensagemProcessadaAsync(
                new MensagemProcessada(mensagemId, pedido.Id, eventoPedidoCriado.TipoEvento, "FALHA", provedorTempo.GetUtcNow(), excecao.Message),
                tokenCancelamento);
            await unidadeTrabalho.SaveChangesAsync(tokenCancelamento);
            throw;
        }
    }

    private async Task PublicarEventoAsync<TEvento>(TEvento evento, CancellationToken tokenCancelamento)
    {
        var mensagem = JsonSerializer.Serialize(evento, OpcoesJson);
        await sns.PublishAsync(new PublishRequest
        {
            TopicArn = opcoes.TopicoSnsArn,
            Message = mensagem,
            Subject = typeof(TEvento).Name
        }, tokenCancelamento);
    }

    private EventoPedidoAprovado ToEventoPedidoAprovado(Pedido pedido, EventoPedidoCriado eventoOriginal, string motivo)
    {
        return new EventoPedidoAprovado(
            $"evt-{Guid.NewGuid()}",
            "PedidoAprovado",
            "1.0.0",
            provedorTempo.GetUtcNow(),
            pedido.Id,
            pedido.ClienteId,
            motivo,
            "es2-worker",
            opcoes.ValorLimiteAprovacao,
            pedido.ValorTotal,
            eventoOriginal.CorrelacaoId,
            "es2-worker",
            new Dictionary<string, string> { ["eventoOriginalId"] = eventoOriginal.EventoId });
    }

    private EventoPedidoRejeitado ToEventoPedidoRejeitado(Pedido pedido, EventoPedidoCriado eventoOriginal, string motivo)
    {
        return new EventoPedidoRejeitado(
            $"evt-{Guid.NewGuid()}",
            "PedidoRejeitado",
            "1.0.0",
            provedorTempo.GetUtcNow(),
            pedido.Id,
            pedido.ClienteId,
            motivo,
            "es2-worker",
            opcoes.ValorLimiteAprovacao,
            pedido.ValorTotal,
            true,
            eventoOriginal.CorrelacaoId,
            "es2-worker",
            new Dictionary<string, string> { ["eventoOriginalId"] = eventoOriginal.EventoId });
    }

    private EventoProcessamentoPedidoFalhou ToEventoProcessamentoFalhou(Pedido pedido, EventoPedidoCriado eventoOriginal, Exception excecao)
    {
        return new EventoProcessamentoPedidoFalhou(
            $"evt-{Guid.NewGuid()}",
            "ProcessamentoPedidoFalhou",
            "1.0.0",
            provedorTempo.GetUtcNow(),
            pedido.Id,
            pedido.ClienteId,
            "Erro ao processar pedido",
            excecao.GetType().Name,
            excecao.Message,
            excecao.StackTrace,
            true,
            eventoOriginal.CorrelacaoId,
            "es2-worker",
            new Dictionary<string, string> { ["eventoOriginalId"] = eventoOriginal.EventoId });
    }

    private static EnvelopeSns? GetEnvelopeSns(string corpoMensagem)
    {
        try
        {
            return JsonSerializer.Deserialize<EnvelopeSns>(corpoMensagem, OpcoesJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record EnvelopeSns(
        [property: JsonPropertyName("MessageId")] string? MessageId,
        [property: JsonPropertyName("Message")] string? Message);
}
