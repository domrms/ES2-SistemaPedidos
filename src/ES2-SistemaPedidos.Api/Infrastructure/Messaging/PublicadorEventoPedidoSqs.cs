using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Infrastructure.Messaging;

public sealed class PublicadorEventoPedidoSqs(
    IAmazonSQS sqs,
    IConfiguration configuracao,
    ILogger<PublicadorEventoPedidoSqs> registrador)
    : IPublicadorEventoSolicitacao
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task PublicarAsync(EventoSolicitacaoCliente evento, CancellationToken tokenCancelamento)
    {
        var filaUrl = configuracao["SQS_FILA_URL"]
            ?? configuracao["SQS_QUEUE_URL"]
            ?? configuracao["AWS:FilaSolicitacoesUrl"]
            ?? configuracao["AWS:FilaPedidosUrl"]
            ?? configuracao["AWS:SqsQueueUrl"];

        if (string.IsNullOrWhiteSpace(filaUrl))
        {
            throw new InvalidOperationException("URL da fila SQS nao configurada. Defina SQS_FILA_URL ou AWS:FilaSolicitacoesUrl.");
        }

        var mensagem = JsonSerializer.Serialize(evento, OpcoesJson);
        registrador.LogInformation("Payload enviado para SQS: {PayloadSqs}", mensagem);

        var resposta = await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = filaUrl,
            MessageBody = mensagem,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["tipoEvento"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "SolicitacaoCliente"
                }
            }
        }, tokenCancelamento);

        registrador.LogInformation(
            "Publicado evento {EventoId} do cliente {ClienteId} e produto {ProdutoId} na mensagem SQS {MensagemId}",
            evento.EventoId,
            evento.ClienteId,
            evento.ProdutoId,
            resposta.MessageId);
    }
}
