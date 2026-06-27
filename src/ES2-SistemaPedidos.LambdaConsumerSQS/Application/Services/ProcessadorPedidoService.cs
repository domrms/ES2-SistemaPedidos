using System.Text.Json;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;

public sealed class ProcessadorPedidoService(
    IPedidoProcessamentoClient clienteProcessamento,
    TimeProvider timeProvider,
    ILogger<ProcessadorPedidoService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> ProcessMessageAsync(string sqsMessageId, string messageBody,
        CancellationToken cancellationToken)
    {
        var evento = JsonSerializer.Deserialize<EventoSolicitacaoCliente>(messageBody, JsonOptions);
        if (evento is null
            || evento.ClienteId <= 0
            || evento.ProdutoId <= 0
            || string.IsNullOrWhiteSpace(evento.EventoId))
        {
            logger.LogWarning("Mensagem {MensagemId} possui payload invalido", sqsMessageId);
            return false;
        }

        var processamento = new EventoProcessamento(
            evento.ClienteId,
            evento.ProdutoId,
            evento.EventoId,
            evento.DataHoraRequisicao,
            timeProvider.GetUtcNow());

        try
        {
            await clienteProcessamento.RegistrarEventoAsync(processamento, cancellationToken);
        }
        catch (Exception exception)
        {
            try
            {
                await clienteProcessamento.RegistrarErroAsync(
                    processamento,
                    "Falha durante o processamento da solicitacao.",
                    cancellationToken);
            }
            catch (Exception errorRegistrationException)
            {
                logger.LogError(errorRegistrationException,
                    "Nao foi possivel registrar o estado de erro do evento {EventoId}", evento.EventoId);
            }

            throw new InvalidOperationException($"Falha ao processar o evento {evento.EventoId}.", exception);
        }

        logger.LogInformation(
            "Evento {EventoId} do cliente {ClienteId} e produto {ProdutoId} salvo no banco",
            evento.EventoId,
            evento.ClienteId,
            evento.ProdutoId);

        return true;
    }
}
