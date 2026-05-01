namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(Guid id, Guid orderId, string productId, int quantity, decimal unitPrice, string? description)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineTotal = decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        Description = description;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string ProductId { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal { get; private set; }

    public string? Description { get; private set; }

    public Order? Order { get; private set; }
}
