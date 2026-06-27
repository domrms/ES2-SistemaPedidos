using System.Text.Json.Serialization;
using Amazon;
using Amazon.SQS;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Api.Infrastructure.Health;
using ES2_SistemaPedidos.Api.Infrastructure.Messaging;
using ES2_SistemaPedidos.Api.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog((context, _, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigins", policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPublicadorEventoSolicitacao, PedidoPublisherEventSqs>();
builder.Services.AddHttpClient<IPersistenciaPedidosClient, PersistenciaPedidosHttpClient>(client =>
{
    var baseUrl = builder.Configuration["PersistenciaApi:UrlBase"]
                  ?? throw new InvalidOperationException("URL da API de persistencia nao configurada.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient("FlociHealthCheck",
    client => client.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddHttpClient(nameof(PersistenciaApiHealthCheck), client =>
{
    var baseUrl = builder.Configuration["PersistenciaApi:UrlBase"]
                  ?? throw new InvalidOperationException("URL da API de persistencia nao configurada.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHealthChecks()
    .AddCheck<PersistenciaApiHealthCheck>("persistencia-api", tags: ["ready"])
    .AddCheck<FlociHealthCheck>("floci", tags: ["ready"]);
builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var regionName = builder.Configuration["AWS_REGIAO"]
                     ?? builder.Configuration["AWS_REGION"]
                     ?? builder.Configuration["AWS:Regiao"]
                     ?? builder.Configuration["AWS:Region"];
    if (string.IsNullOrWhiteSpace(regionName))
        throw new InvalidOperationException("Regiao AWS nao configurada. Defina AWS:Regiao.");

    var serviceUrl = builder.Configuration["AWS_ENDPOINT_URL"]
                     ?? builder.Configuration["AWS:ServiceUrl"]
                     ?? builder.Configuration["AWS:EndpointUrl"];

    var sqsConfiguration = new AmazonSQSConfig();
    if (string.IsNullOrWhiteSpace(serviceUrl))
    {
        sqsConfiguration.RegionEndpoint = RegionEndpoint.GetBySystemName(regionName);
    }
    else
    {
        sqsConfiguration.ServiceURL = serviceUrl;
        sqsConfiguration.AuthenticationRegion = regionName;
    }

    return new AmazonSQSClient(sqsConfiguration);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowOrigins");
app.MapControllers();
app.MapHealthChecks("/api/healthcheck", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

await app.RunAsync();
