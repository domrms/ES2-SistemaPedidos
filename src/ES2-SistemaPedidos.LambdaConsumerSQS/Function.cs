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
    private readonly IServiceProvider _services;

    [ExcludeFromCodeCoverage]
    public Function()
        : this(CreateServiceProvider())
    {
    }

    internal Function(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent eventoSqs, ILambdaContext context)
    {
        using var scope = _services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<ProcessadorPedidoService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Function>>();
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var message in eventoSqs.Records)
            try
            {
                var processed = await processor.ProcessMessageAsync(
                    message.MessageId,
                    message.Body,
                    CancellationToken.None);

                if (!processed)
                {
                    logger.LogWarning("Mensagem {MensagemId} possui payload invalido e sera marcada como falha",
                        message.MessageId);
                    failures.Add(new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier = message.MessageId
                    });
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Erro ao processar mensagem {MensagemId}; a mensagem sera marcada como falha", message.MessageId);
                failures.Add(new SQSBatchResponse.BatchItemFailure
                {
                    ItemIdentifier = message.MessageId
                });
            }

        return new SQSBatchResponse
        {
            BatchItemFailures = failures
        };
    }

    [ExcludeFromCodeCoverage]
    private static ServiceProvider CreateServiceProvider()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                       ?? "Production";

        var assemblyDirectory = Path.GetDirectoryName(typeof(Function).Assembly.Location)
                                ?? AppContext.BaseDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(assemblyDirectory)
            .AddJsonFile("appsettings.json", false, false)
            .AddJsonFile($"appsettings.{environment}.json", true, false)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(new DateTimeConsoleFormatter())
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddSerilog(Log.Logger));
        services.AddProcessamentoPedidos(configuration);

        return services.BuildServiceProvider();
    }
}
