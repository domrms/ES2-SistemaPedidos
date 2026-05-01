using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using ES2_SistemaPedidos.Shared.Logging;
using ES2_SistemaPedidos.Worker;
using ES2_SistemaPedidos.Worker.Application.Abstractions;
using ES2_SistemaPedidos.Worker.Application.Services;
using ES2_SistemaPedidos.Worker.Infrastructure.Data;
using ES2_SistemaPedidos.Worker.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new HorarioBrasiliaConsoleFormatter())
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((contexto, servicos, configuracaoLog) =>
    {
        configuracaoLog
            .Enrich.FromLogContext()
            .WriteTo.Console(new HorarioBrasiliaConsoleFormatter());
    })
    .ConfigureServices((contexto, servicos) =>
    {
        servicos.AddOrderProcessingOptions(contexto.Configuration);
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<IAmazonSQS>(_ => CriarClienteSqs(contexto.Configuration));
        servicos.AddScoped<IPedidoProcessamentoRepositorio, PedidoProcessamentoRepositorioDapper>();
        servicos.AddScoped<ProcessadorPedido>();
        servicos.AddHostedService<ServicoWorkerPedidos>();
    })
    .Build();

await host.RunAsync();

static AmazonSQSClient CriarClienteSqs(IConfiguration configuracao)
{
    var nomeRegiao = GetNomeRegiao(configuracao);
    var configuracaoSqs = new AmazonSQSConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(nomeRegiao)
    };

    var urlServico = GetUrlServicoAws(configuracao);
    if (!string.IsNullOrWhiteSpace(urlServico))
    {
        configuracaoSqs.ServiceURL = urlServico;
        configuracaoSqs.AuthenticationRegion = nomeRegiao;
        return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), configuracaoSqs);
    }

    return new AmazonSQSClient(configuracaoSqs);
}

static string GetNomeRegiao(IConfiguration configuracao)
{
    return configuracao["AWS_REGIAO"]
           ?? configuracao["AWS_REGION"]
           ?? configuracao["AWS:Regiao"]
           ?? configuracao["AWS:Region"]
           ?? "us-east-1";
}

static string? GetUrlServicoAws(IConfiguration configuracao)
{
    return configuracao["AWS_URL_SERVICO"]
           ?? configuracao["AWS_ENDPOINT_URL"]
           ?? configuracao["AWS:UrlServico"]
           ?? configuracao["AWS:ServiceUrl"];
}