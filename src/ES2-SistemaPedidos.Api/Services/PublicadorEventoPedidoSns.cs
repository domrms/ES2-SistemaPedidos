using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Services;

public sealed class PublicadorEventoPedidoSns(
    IAmazonSimpleNotificationService sns,
    IConfiguration configuracao,
    ILogger<PublicadorEventoPedidoSns> registrador)
    : IPublicadorEventoPedido
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task PublishPedidoCriadoAsync(EventoPedidoCriado eventoPedidoCriado, CancellationToken tokenCancelamento)
    {
        var topicoArn = configuracao["SNS_TOPICO_ARN"]
            ?? configuracao["SNS_TOPIC_ARN"]
            ?? configuracao["AWS:TopicoSnsArn"]
            ?? configuracao["AWS:SnsTopicArn"];

        if (string.IsNullOrWhiteSpace(topicoArn))
        {
            throw new InvalidOperationException("ARN do topico SNS nao configurado. Defina SNS_TOPICO_ARN ou AWS:TopicoSnsArn.");
        }

        var mensagem = JsonSerializer.Serialize(eventoPedidoCriado, OpcoesJson);
        var resposta = await sns.PublishAsync(new PublishRequest
        {
            TopicArn = topicoArn,
            Message = mensagem,
            Subject = "Pedido Criado",
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
            "Publicado evento {EventoId} do pedido {PedidoId} na mensagem SNS {MensagemId}",
            eventoPedidoCriado.EventoId,
            eventoPedidoCriado.PedidoId,
            resposta.MessageId);
    }
}
