using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;
using ES2_SistemaPedidos.Shared.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace ES2_SistemaPedidos.LambdaConsumerSQS;

public sealed class Function
{
    private readonly IServiceProvider _servicos;

    [ExcludeFromCodeCoverage]
    public Function()
        : this(CriarServiceProvider())
    {
    }

    internal Function(IServiceProvider servicos)
    {
        _servicos = servicos;
    }

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent eventoSqs, ILambdaContext contexto)
    {
        using var escopo = _servicos.CreateScope();
        var processador = escopo.ServiceProvider.GetRequiredService<ProcessadorPedidoService>();
        var registrador = escopo.ServiceProvider.GetRequiredService<ILogger<Function>>();
        var falhas = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var mensagem in eventoSqs.Records)
            try
            {
                var processada = await processador.ProcessMessageAsync(
                    mensagem.MessageId,
                    mensagem.Body,
                    CancellationToken.None);

                if (!processada)
                {
                    registrador.LogWarning("Mensagem {MensagemId} possui payload invalido e sera marcada como falha",
                        mensagem.MessageId);
                    falhas.Add(new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier = mensagem.MessageId
                    });
                }
            }
            catch (Exception excecao)
            {
                registrador.LogError(excecao,
                    "Erro ao processar mensagem {MensagemId}; a mensagem sera marcada como falha", mensagem.MessageId);
                falhas.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = mensagem.MessageId
                });
            }

        return new SQSBatchResponse
        {
            BatchItemFailures = falhas
        };
    }

    [ExcludeFromCodeCoverage]
    private static IServiceProvider CriarServiceProvider()
    {
        var ambiente = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                       ?? "Production";

        var diretorioAssembly = Path.GetDirectoryName(typeof(Function).Assembly.Location)
                                ?? AppContext.BaseDirectory;

        var configuracao = new ConfigurationBuilder()
            .SetBasePath(diretorioAssembly)
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile($"appsettings.{ambiente}.json", true, false)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(new DateTimeConsoleFormatter())
            .CreateLogger();

        var servicos = new ServiceCollection();
        servicos.AddSingleton<IConfiguration>(configuracao);
        servicos.AddLogging(builder => builder.AddSerilog(Log.Logger, false));
        servicos.AddProcessamentoPedidos(configuracao);

        return servicos.BuildServiceProvider();
    }
}
