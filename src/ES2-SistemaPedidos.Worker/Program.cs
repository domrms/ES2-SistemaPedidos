using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using ES2_SistemaPedidos.Shared;
using ES2_SistemaPedidos.Worker;
using ES2_SistemaPedidos.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((contexto, servicos, configuracaoLog) =>
    {
        configuracaoLog
            .Enrich.FromLogContext()
            .WriteTo.Console();
    })
    .ConfigureServices((contexto, servicos) =>
    {
        servicos.AddOrderProcessingOptions(contexto.Configuration);
        servicos.AddPersistenciaPedidos(contexto.Configuration);
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddSingleton<IAmazonSQS>(_ => CriarClienteSqs(contexto.Configuration));
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
