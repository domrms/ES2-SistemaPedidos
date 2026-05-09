using System.Text.Json;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;

public sealed class ProcessadorPedidoService(
    IPedidoProcessamentoRepository repository,
    TimeProvider provedorTempo,
    ILogger<ProcessadorPedidoService> registrador)
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task<bool> ProcessMessageAsync(string mensagemSqsId, string corpoMensagem, CancellationToken tokenCancelamento)
    {
        var evento = JsonSerializer.Deserialize<EventoSolicitacaoCliente>(corpoMensagem, OpcoesJson);
        if (evento is null
            || evento.ClienteId <= 0
            || evento.ProdutoId <= 0
            || string.IsNullOrWhiteSpace(evento.EventoId))
        {
            registrador.LogWarning("Mensagem {MensagemId} possui payload invalido", mensagemSqsId);
            return false;
        }

        await repository.RegistrarEventoAsync(new EventoProcessamento(
            evento.ClienteId,
            evento.ProdutoId,
            evento.EventoId,
            evento.DataHoraRequisicao,
            provedorTempo.GetUtcNow()), tokenCancelamento);

        registrador.LogInformation(
            "Evento {EventoId} do cliente {ClienteId} e produto {ProdutoId} salvo no banco",
            evento.EventoId,
            evento.ClienteId,
            evento.ProdutoId);

        return true;
    }
}
