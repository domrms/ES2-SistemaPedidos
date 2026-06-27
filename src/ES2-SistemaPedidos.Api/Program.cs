using System.Diagnostics.CodeAnalysis;
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

var construtorAplicacao = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(construtorAplicacao.Configuration)
    .CreateLogger();

construtorAplicacao.Host.UseSerilog((contexto, servicos, configuracaoLog) =>
{
    configuracaoLog
        .ReadFrom.Configuration(contexto.Configuration);
});

construtorAplicacao.Services.AddCors(opcoes =>
{
    opcoes.AddPolicy("AllowOrigins", construtor =>
    {
        construtor
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

construtorAplicacao.Services
    .AddControllers()
    .AddJsonOptions(opcoes => { opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

construtorAplicacao.Services.AddEndpointsApiExplorer();
construtorAplicacao.Services.AddSwaggerGen();
construtorAplicacao.Services.AddScoped<PedidoService>();
construtorAplicacao.Services.AddSingleton(TimeProvider.System);
construtorAplicacao.Services.AddSingleton<IPublicadorEventoSolicitacao, PedidoPublisherEventSqs>();
construtorAplicacao.Services.AddHttpClient<IPersistenciaPedidosClient, PersistenciaPedidosHttpClient>(cliente =>
{
    var urlBase = construtorAplicacao.Configuration["PersistenciaApi:UrlBase"]
                  ?? "http://localhost:5080";
    cliente.BaseAddress = new Uri(urlBase);
    cliente.Timeout = TimeSpan.FromSeconds(10);
});
construtorAplicacao.Services.AddHttpClient("FlociHealthCheck",
    cliente => { cliente.Timeout = TimeSpan.FromSeconds(5); });
construtorAplicacao.Services.AddHttpClient(nameof(PersistenciaApiHealthCheck), cliente =>
{
    cliente.BaseAddress = new Uri(construtorAplicacao.Configuration["PersistenciaApi:UrlBase"]
                                  ?? "http://localhost:5080");
    cliente.Timeout = TimeSpan.FromSeconds(5);
});
construtorAplicacao.Services.AddHealthChecks()
    .AddCheck<PersistenciaApiHealthCheck>("persistencia-api", tags: ["ready"])
    .AddCheck<FlociHealthCheck>("floci", tags: ["ready"]);
construtorAplicacao.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var nomeRegiao = construtorAplicacao.Configuration["AWS_REGIAO"]
                     ?? construtorAplicacao.Configuration["AWS_REGION"]
                     ?? construtorAplicacao.Configuration["AWS:Regiao"]
                     ?? construtorAplicacao.Configuration["AWS:Region"];
    if (string.IsNullOrWhiteSpace(nomeRegiao))
        throw new InvalidOperationException("Regiao AWS nao configurada. Defina AWS:Regiao.");

    var urlServico = construtorAplicacao.Configuration["AWS_ENDPOINT_URL"]
                     ?? construtorAplicacao.Configuration["AWS:ServiceUrl"]
                     ?? construtorAplicacao.Configuration["AWS:EndpointUrl"];

    var configuracaoSqs = new AmazonSQSConfig();
    if (string.IsNullOrWhiteSpace(urlServico))
    {
        configuracaoSqs.RegionEndpoint = RegionEndpoint.GetBySystemName(nomeRegiao);
    }
    else
    {
        configuracaoSqs.ServiceURL = urlServico;
        configuracaoSqs.AuthenticationRegion = nomeRegiao;
    }

    return new AmazonSQSClient(configuracaoSqs);
});

var aplicacao = construtorAplicacao.Build();

aplicacao.UseSwagger();
aplicacao.UseSwaggerUI();
aplicacao.UseCors("AllowOrigins");
aplicacao.MapControllers();
aplicacao.MapHealthChecks("/api/healthcheck", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.EscreverAsync
});

aplicacao.Run();

[ExcludeFromCodeCoverage]
public partial class Program;