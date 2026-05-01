namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(string messageId, Guid? orderId, string messageType, string status, DateTimeOffset processedAt, string? errorDetails = null)
    {
        MessageId = messageId;
        OrderId = orderId;
        MessageType = messageType;
        Status = status;
        ProcessedAt = processedAt;
        ErrorDetails = errorDetails;
    }

    public string MessageId { get; private set; } = string.Empty;

    public Guid? OrderId { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string? ErrorDetails { get; private set; }

    public Order? Order { get; private set; }
}
