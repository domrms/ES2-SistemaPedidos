using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Services;

public sealed class PublicadorEventoPedidoSqs(
    IAmazonSQS sqs,
    IConfiguration configuracao,
    ILogger<PublicadorEventoPedidoSqs> registrador)
    : IPublicadorEventoPedido
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task PublishPedidoCriadoAsync(EventoPedidoCriado eventoPedidoCriado, CancellationToken tokenCancelamento)
    {
        var filaUrl = configuracao["SQS_FILA_URL"]
            ?? configuracao["SQS_QUEUE_URL"]
            ?? configuracao["AWS:FilaPedidosUrl"]
            ?? configuracao["AWS:SqsQueueUrl"];

        if (string.IsNullOrWhiteSpace(filaUrl))
        {
            throw new InvalidOperationException("URL da fila SQS nao configurada. Defina SQS_FILA_URL ou AWS:FilaPedidosUrl.");
        }

        var mensagem = JsonSerializer.Serialize(eventoPedidoCriado, OpcoesJson);
        var resposta = await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = filaUrl,
            MessageBody = mensagem,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["tipoEvento"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = eventoPedidoCriado.TipoEvento
                }
            }
        }, tokenCancelamento);

        registrador.LogInformation(
            "Publicado evento {EventoId} do pedido {PedidoId} na mensagem SQS {MensagemId}",
            eventoPedidoCriado.EventoId,
            eventoPedidoCriado.PedidoId,
            resposta.MessageId);
    }
}
