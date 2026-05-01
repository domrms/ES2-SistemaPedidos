using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Api.Services;

public sealed class OrderService(
    ApplicationDbContext dbContext,
    IOrderEventPublisher eventPublisher,
    TimeProvider timeProvider,
    IConfiguration configuration)
{
    private const decimal AmountTolerance = 0.01m;
    private const decimal MaxAmount = 999_999.99m;
    private const int MaxItems = 1000;
    private const int MaxQuantity = 10_000;

    public async Task<Result<CreateOrderResponse>> CreateAsync(
        CreateOrderRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateOrder(request);
        if (validationErrors.Count > 0)
        {
            return Result<CreateOrderResponse>.ValidationFailed(ToValidationResponse(validationErrors));
        }

        var now = timeProvider.GetUtcNow();
        var order = new Order(Guid.NewGuid(), request.CustomerId!.Trim(), request.TotalAmount, now);

        foreach (var item in request.Items!)
        {
            order.AddItem(
                Guid.NewGuid(),
                item.ProductId!.Trim(),
                item.Quantity,
                decimal.Round(item.UnitPrice, 2, MidpointRounding.AwayFromZero),
                string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim());
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var orderCreatedEvent = ToOrderCreatedEvent(order, correlationId, now);
        await eventPublisher.PublishOrderCreatedAsync(orderCreatedEvent, cancellationToken);

        return Result<CreateOrderResponse>.Success(new CreateOrderResponse(
            order.Id,
            order.CustomerId,
            order.Status,
            order.TotalAmount,
            order.Items.Count,
            order.CreatedAt,
            order.UpdatedAt));
    }

    public async Task<OrderDetailsResponse?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(entity => entity.Items)
            .FirstOrDefaultAsync(entity => entity.Id == orderId, cancellationToken);

        return order is null ? null : ToDetailsResponse(order);
    }

    public async Task<Result<ListOrdersResponse>> ListAsync(
        string customerId,
        OrderStatus? status,
        int skip,
        int take,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateListOrders(customerId, skip, take, dateFrom, dateTo);
        if (validationErrors.Count > 0)
        {
            return Result<ListOrdersResponse>.ValidationFailed(ToValidationResponse(validationErrors));
        }

        var query = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.CustomerId == customerId.Trim());

        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        if (dateFrom.HasValue)
        {
            var from = new DateTimeOffset(dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            query = query.Where(order => order.CreatedAt >= from);
        }

        if (dateTo.HasValue)
        {
            var to = new DateTimeOffset(dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
            query = query.Where(order => order.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(order => order.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(order => new OrderSummaryResponse(
                order.Id,
                order.CustomerId,
                order.Status,
                order.TotalAmount,
                order.Items.Count,
                order.CreatedAt,
                order.UpdatedAt,
                order.CompletedAt))
            .ToListAsync(cancellationToken);

        return Result<ListOrdersResponse>.Success(new ListOrdersResponse(
            orders,
            new PaginationResponse(skip, take, total, skip + orders.Count < total, (int)Math.Ceiling(total / (double)take))));
    }

    private List<ValidationError> ValidateCreateOrder(CreateOrderRequest request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            errors.Add(new ValidationError("customerId", "Customer ID is required."));
        }
        else if (request.CustomerId.Length > 255)
        {
            errors.Add(new ValidationError("customerId", "Customer ID must have at most 255 characters."));
        }

        if (request.TotalAmount <= 0)
        {
            errors.Add(new ValidationError("totalAmount", "Total amount must be greater than 0."));
        }
        else if (request.TotalAmount > MaxAmount)
        {
            errors.Add(new ValidationError("totalAmount", "Total amount must be at most 999999.99."));
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            errors.Add(new ValidationError("items", "At least 1 item is required."));
            return errors;
        }

        if (request.Items.Count > MaxItems)
        {
            errors.Add(new ValidationError("items", $"An order cannot contain more than {MaxItems} items."));
        }

        var calculatedTotal = 0m;
        var index = 0;
        foreach (var item in request.Items)
        {
            var prefix = $"items[{index}]";

            if (string.IsNullOrWhiteSpace(item.ProductId))
            {
                errors.Add(new ValidationError($"{prefix}.productId", "Product ID is required."));
            }
            else if (item.ProductId.Length > 255)
            {
                errors.Add(new ValidationError($"{prefix}.productId", "Product ID must have at most 255 characters."));
            }

            if (item.Quantity <= 0 || item.Quantity > MaxQuantity)
            {
                errors.Add(new ValidationError($"{prefix}.quantity", $"Quantity must be between 1 and {MaxQuantity}."));
            }

            if (item.UnitPrice < 0 || item.UnitPrice > MaxAmount)
            {
                errors.Add(new ValidationError($"{prefix}.unitPrice", "Unit price must be between 0 and 999999.99."));
            }

            if (item.Description?.Length > 500)
            {
                errors.Add(new ValidationError($"{prefix}.description", "Description must have at most 500 characters."));
            }

            calculatedTotal += decimal.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
            index++;
        }

        if (Math.Abs(request.TotalAmount - calculatedTotal) > AmountTolerance)
        {
            errors.Add(new ValidationError("totalAmount", $"Total amount must match the sum of items. Calculated total is {calculatedTotal:0.00}."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateListOrders(string customerId, int skip, int take, DateOnly? dateFrom, DateOnly? dateTo)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            errors.Add(new ValidationError("customerId", "Customer ID is required."));
        }

        if (skip < 0)
        {
            errors.Add(new ValidationError("skip", "skip must be greater than or equal to 0."));
        }

        if (take is < 1 or > 100)
        {
            errors.Add(new ValidationError("take", "take must be between 1 and 100."));
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
        {
            errors.Add(new ValidationError("dateFrom", "dateFrom must be earlier than or equal to dateTo."));
        }

        return errors;
    }

    private OrderCreatedEvent ToOrderCreatedEvent(Order order, string correlationId, DateTimeOffset publishedAt)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "development";

        return new OrderCreatedEvent(
            $"evt-{Guid.NewGuid()}",
            "OrderCreated",
            "1.0.0",
            publishedAt,
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            "EUR",
            order.Items.Select(item => new OrderEventItem(
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.Description)).ToList(),
            correlationId,
            "es2-api",
            new Dictionary<string, string>
            {
                ["environment"] = environment,
                ["apiVersion"] = "1.0.0"
            });
    }

    private static OrderDetailsResponse ToDetailsResponse(Order order)
    {
        return new OrderDetailsResponse(
            order.Id,
            order.CustomerId,
            order.Status,
            order.TotalAmount,
            order.Items.Select(item => new OrderItemResponse(
                item.Id,
                item.ProductId,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal,
                item.Description)).ToList(),
            order.CreatedAt,
            order.UpdatedAt,
            order.ProcessingStartedAt,
            order.CompletedAt,
            order.ApprovalReason,
            order.RejectionReason,
            order.ErrorMessage);
    }

    private static ValidationErrorResponse ToValidationResponse(IReadOnlyCollection<ValidationError> validationErrors)
    {
        return new ValidationErrorResponse(
            "ValidationFailed",
            "Order validation failed",
            validationErrors);
    }
}
