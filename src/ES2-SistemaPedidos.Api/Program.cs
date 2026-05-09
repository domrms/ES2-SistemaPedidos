using System.Text.Json.Serialization;
using Amazon;
using Amazon.SQS;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Api.Infrastructure.Messaging;
using ES2_SistemaPedidos.Shared;
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
    opcoes.AddPolicy("PermitirOrigens", construtor =>
    {
        construtor
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

construtorAplicacao.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
{
    opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

construtorAplicacao.Services.AddEndpointsApiExplorer();
construtorAplicacao.Services.AddSwaggerGen();
construtorAplicacao.Services.AddPersistenciaPedidos(construtorAplicacao.Configuration);
construtorAplicacao.Services.AddScoped<PedidoService>();
construtorAplicacao.Services.AddSingleton(TimeProvider.System);
construtorAplicacao.Services.AddSingleton<IPublicadorEventoSolicitacao, PedidoPublisherEventSqs>();
construtorAplicacao.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var nomeRegiao = construtorAplicacao.Configuration["AWS_REGIAO"]
        ?? construtorAplicacao.Configuration["AWS_REGION"]
        ?? construtorAplicacao.Configuration["AWS:Regiao"]
        ?? construtorAplicacao.Configuration["AWS:Region"];
    if (string.IsNullOrWhiteSpace(nomeRegiao))
    {
        throw new InvalidOperationException("Regiao AWS nao configurada. Defina AWS:Regiao.");
    }

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
aplicacao.UseCors("PermitirOrigens");
aplicacao.MapControllers();

aplicacao.Run();

public partial class Program;
