using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Services;

public sealed class SnsOrderEventPublisher(
    IAmazonSimpleNotificationService sns,
    IConfiguration configuration,
    ILogger<SnsOrderEventPublisher> logger)
    : IOrderEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishOrderCreatedAsync(OrderCreatedEvent orderCreatedEvent, CancellationToken cancellationToken)
    {
        var topicArn = configuration["SNS_TOPIC_ARN"] ?? configuration["AWS:SnsTopicArn"];
        if (string.IsNullOrWhiteSpace(topicArn))
        {
            throw new InvalidOperationException("SNS topic ARN is not configured. Set SNS_TOPIC_ARN or AWS:SnsTopicArn.");
        }

        var message = JsonSerializer.Serialize(orderCreatedEvent, JsonOptions);
        var response = await sns.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = message,
            Subject = "Order Created",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = orderCreatedEvent.EventType
                }
            }
        }, cancellationToken);

        logger.LogInformation(
            "Published OrderCreatedEvent {EventId} for order {OrderId} to SNS message {MessageId}",
            orderCreatedEvent.EventId,
            orderCreatedEvent.OrderId,
            response.MessageId);
    }
}
