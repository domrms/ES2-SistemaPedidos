using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Infrastructure.Messaging;

public sealed partial class PedidoPublisherEventSqs(
    IAmazonSQS sqs,
    IConfiguration configuration,
    ILogger<PedidoPublisherEventSqs> logger)
    : IPublicadorEventoSolicitacao
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublicarAsync(EventoSolicitacaoCliente evento, CancellationToken cancellationToken)
    {
        var queueUrl = configuration["SQS_FILA_URL"]
                       ?? configuration["SQS_QUEUE_URL"]
                       ?? configuration["AWS:FilaSolicitacoesUrl"]
                       ?? configuration["AWS:FilaPedidosUrl"]
                       ?? configuration["AWS:SqsQueueUrl"];

        if (string.IsNullOrWhiteSpace(queueUrl))
            throw new InvalidOperationException(
                "URL da fila SQS nao configurada. Defina SQS_FILA_URL ou AWS:FilaSolicitacoesUrl.");

        var message = JsonSerializer.Serialize(evento, JsonOptions);
        LogSqsPayload(logger, message);

        var response = await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["tipoEvento"] = new()
                {
                    DataType = "String",
                    StringValue = "SolicitacaoCliente"
                }
            }
        }, cancellationToken);

        LogPublishedEvent(logger, evento.EventoId, evento.ClienteId, evento.ProdutoId, response.MessageId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Payload enviado para SQS: {PayloadSqs}")]
    private static partial void LogSqsPayload(ILogger logger, string payloadSqs);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Publicado evento {EventoId} do cliente {ClienteId} e produto {ProdutoId} na mensagem SQS {MensagemId}")]
    private static partial void LogPublishedEvent(ILogger logger, string eventoId, int clienteId, int produtoId,
        string mensagemId);
}
