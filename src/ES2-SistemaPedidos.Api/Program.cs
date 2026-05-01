using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Api.Infrastructure.Messaging;
using ES2_SistemaPedidos.Api.Security;
using ES2_SistemaPedidos.Shared;
using Microsoft.AspNetCore.Authentication;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var construtorAplicacao = WebApplication.CreateBuilder(args);

construtorAplicacao.Host.UseSerilog((contexto, servicos, configuracaoLog) =>
{
    configuracaoLog
        .Enrich.FromLogContext()
        .WriteTo.Console();
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
construtorAplicacao.Services.AddScoped<ServicoPedido>();
construtorAplicacao.Services.AddSingleton(TimeProvider.System);
construtorAplicacao.Services.AddSingleton<IPublicadorEventoPedido, PublicadorEventoPedidoSqs>();
construtorAplicacao.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var nomeRegiao = construtorAplicacao.Configuration["AWS_REGIAO"]
        ?? construtorAplicacao.Configuration["AWS_REGION"]
        ?? construtorAplicacao.Configuration["AWS:Regiao"]
        ?? construtorAplicacao.Configuration["AWS:Region"]
        ?? "us-east-1";
    var configuracaoSqs = new AmazonSQSConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(nomeRegiao)
    };

    var urlServico = construtorAplicacao.Configuration["AWS_URL_SERVICO"]
        ?? construtorAplicacao.Configuration["AWS_ENDPOINT_URL"]
        ?? construtorAplicacao.Configuration["AWS:UrlServico"]
        ?? construtorAplicacao.Configuration["AWS:ServiceUrl"];
    if (!string.IsNullOrWhiteSpace(urlServico))
    {
        configuracaoSqs.ServiceURL = urlServico;
        configuracaoSqs.AuthenticationRegion = nomeRegiao;
        return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), configuracaoSqs);
    }

    return new AmazonSQSClient(configuracaoSqs);
});

construtorAplicacao.Services
    .AddAuthentication(PadroesAutenticacaoBearerSimples.EsquemaAutenticacao)
    .AddScheme<AuthenticationSchemeOptions, ManipuladorAutenticacaoBearerSimples>(
        PadroesAutenticacaoBearerSimples.EsquemaAutenticacao,
        opcoes => { });
construtorAplicacao.Services.AddAuthorization();

var aplicacao = construtorAplicacao.Build();

aplicacao.UseSwagger();
aplicacao.UseSwaggerUI();
aplicacao.UseAuthentication();
aplicacao.UseAuthorization();
aplicacao.MapControllers();

aplicacao.Run();

public partial class Program;
