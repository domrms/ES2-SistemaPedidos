using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.Api;

public sealed record CreateOrderRequest(
    string? CustomerId,
    IReadOnlyCollection<CreateOrderItemRequest>? Items,
    decimal TotalAmount);

public sealed record CreateOrderItemRequest(
    string? ProductId,
    int Quantity,
    decimal UnitPrice,
    string? Description);

public sealed record CreateOrderResponse(
    Guid OrderId,
    string CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderDetailsResponse(
    Guid OrderId,
    string CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyCollection<OrderItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? CompletedAt,
    string? ApprovalReason,
    string? RejectionReason,
    string? ErrorMessage);

public sealed record OrderItemResponse(
    Guid ItemId,
    string ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? Description);

public sealed record OrderSummaryResponse(
    Guid OrderId,
    string CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ListOrdersResponse(
    IReadOnlyCollection<OrderSummaryResponse> Orders,
    PaginationResponse Pagination);

public sealed record PaginationResponse(
    int Skip,
    int Take,
    int Total,
    bool HasMore,
    int PageCount);

public sealed record ErrorResponse(
    string Error,
    string Message,
    object? Details = null);

public sealed record ValidationError(
    string Field,
    string Error);

public sealed record ValidationErrorResponse(
    string Error,
    string Message,
    IReadOnlyCollection<ValidationError> Details);
