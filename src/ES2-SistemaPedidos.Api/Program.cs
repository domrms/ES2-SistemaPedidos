using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using ES2_SistemaPedidos.Api;
using ES2_SistemaPedidos.Api.Security;
using ES2_SistemaPedidos.Api.Services;
using ES2_SistemaPedidos.Shared;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOrderPersistence(builder.Configuration);
builder.Services.AddScoped<OrderService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IOrderEventPublisher, SnsOrderEventPublisher>();
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    var regionName = builder.Configuration["AWS_REGION"] ?? builder.Configuration["AWS:Region"] ?? "us-east-1";
    var config = new AmazonSimpleNotificationServiceConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(regionName)
    };

    var serviceUrl = builder.Configuration["AWS_ENDPOINT_URL"] ?? builder.Configuration["AWS:ServiceUrl"];
    if (!string.IsNullOrWhiteSpace(serviceUrl))
    {
        config.ServiceURL = serviceUrl;
        config.AuthenticationRegion = regionName;
        return new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("test", "test"), config);
    }

    return new AmazonSimpleNotificationServiceClient(config);
});

builder.Services
    .AddAuthentication(SimpleBearerAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, SimpleBearerAuthenticationHandler>(
        SimpleBearerAuthenticationDefaults.AuthenticationScheme,
        options => { });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTimeOffset.UtcNow,
    version = "1.0.0"
}));

var orders = app.MapGroup("/api/orders")
    .RequireAuthorization();

orders.MapPost("/", async (CreateOrderRequest request, OrderService orderService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    Result<CreateOrderResponse> result;
    try
    {
        result = await orderService.CreateAsync(request, httpContext.TraceIdentifier, cancellationToken);
    }
    catch (Exception exception) when (IsDependencyFailure(exception))
    {
        return Results.Json(
            new ErrorResponse("ServiceUnavailable", "Database or messaging service temporarily unavailable", new { retryAfter = 30 }),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return result.Match<IResult>(
        success => Results.Created($"/api/orders/{success.OrderId}", success),
        validation => Results.BadRequest(validation));
});

orders.MapGet("/{orderId:guid}", async (Guid orderId, OrderService orderService, CancellationToken cancellationToken) =>
{
    var order = await orderService.GetByIdAsync(orderId, cancellationToken);

    return order is null
        ? Results.NotFound(new ErrorResponse("OrderNotFound", $"Order with ID {orderId} not found"))
        : Results.Ok(order);
});

orders.MapGet("/", async (
    string customerId,
    OrderStatus? status,
    int? skip,
    int? take,
    DateOnly? dateFrom,
    DateOnly? dateTo,
    OrderService orderService,
    CancellationToken cancellationToken) =>
{
    var result = await orderService.ListAsync(customerId, status, skip ?? 0, take ?? 20, dateFrom, dateTo, cancellationToken);

    return result.Match<IResult>(
        success => Results.Ok(success),
        validation => Results.BadRequest(validation));
});

app.Run();

static bool IsDependencyFailure(Exception exception)
{
    return exception is DbUpdateException
        or AmazonServiceException
        or HttpRequestException
        || exception is InvalidOperationException invalidOperationException
        && invalidOperationException.Message.Contains("SNS topic ARN", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
