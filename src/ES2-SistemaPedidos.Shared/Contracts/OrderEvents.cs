using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.Shared.Contracts;

public sealed record OrderCreatedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt,
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("totalAmount")] decimal TotalAmount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("items")] IReadOnlyCollection<OrderEventItem> Items,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string>? Metadata);

public sealed record OrderApprovedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt,
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("approvalReason")] string ApprovalReason,
    [property: JsonPropertyName("approverService")] string ApproverService,
    [property: JsonPropertyName("thresholdAmount")] decimal? ThresholdAmount,
    [property: JsonPropertyName("actualAmount")] decimal? ActualAmount,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string>? Metadata);

public sealed record OrderRejectedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt,
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("rejectionReason")] string RejectionReason,
    [property: JsonPropertyName("rejecterService")] string RejecterService,
    [property: JsonPropertyName("thresholdAmount")] decimal? ThresholdAmount,
    [property: JsonPropertyName("actualAmount")] decimal? ActualAmount,
    [property: JsonPropertyName("requiresManualReview")] bool RequiresManualReview,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string>? Metadata);

public sealed record OrderProcessingFailedEvent(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("eventType")] string EventType,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("publishedAt")] DateTimeOffset PublishedAt,
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("customerId")] string CustomerId,
    [property: JsonPropertyName("failureReason")] string FailureReason,
    [property: JsonPropertyName("errorCode")] string ErrorCode,
    [property: JsonPropertyName("errorMessage")] string ErrorMessage,
    [property: JsonPropertyName("stackTrace")] string? StackTrace,
    [property: JsonPropertyName("retryable")] bool Retryable,
    [property: JsonPropertyName("correlationId")] string? CorrelationId,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("metadata")] IReadOnlyDictionary<string, string>? Metadata);

public sealed record OrderEventItem(
    [property: JsonPropertyName("productId")] string ProductId,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unitPrice")] decimal UnitPrice,
    [property: JsonPropertyName("lineTotal")] decimal LineTotal,
    [property: JsonPropertyName("description")] string? Description);
