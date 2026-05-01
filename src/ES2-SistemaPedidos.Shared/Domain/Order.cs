namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Order(Guid id, string customerId, decimal totalAmount, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string CustomerId { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ApprovalReason { get; private set; }

    public string? RejectionReason { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    public void AddItem(Guid itemId, string productId, int quantity, decimal unitPrice, string? description)
    {
        _items.Add(new OrderItem(itemId, Id, productId, quantity, unitPrice, description));
    }

    public void MarkAsProcessing(DateTimeOffset now)
    {
        TransitionTo(OrderStatus.Processing);
        ProcessingStartedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsApproved(string reason, DateTimeOffset now)
    {
        TransitionTo(OrderStatus.Approved);
        ApprovalReason = reason;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsRejected(string reason, DateTimeOffset now)
    {
        TransitionTo(OrderStatus.Rejected);
        RejectionReason = reason;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsFailed(string errorMessage, DateTimeOffset now)
    {
        if (Status == OrderStatus.Failed)
        {
            return;
        }

        Status = OrderStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = now;
        UpdatedAt = now;
    }

    private void TransitionTo(OrderStatus nextStatus)
    {
        if (!CanTransitionTo(nextStatus))
        {
            throw new InvalidOperationException($"Invalid order status transition from {Status} to {nextStatus}.");
        }

        Status = nextStatus;
    }

    private bool CanTransitionTo(OrderStatus nextStatus)
    {
        return (Status, nextStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Processing, OrderStatus.Approved) => true,
            (OrderStatus.Processing, OrderStatus.Rejected) => true,
            (_, OrderStatus.Failed) => true,
            _ => false
        };
    }
}
