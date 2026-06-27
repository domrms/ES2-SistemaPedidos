using System.Diagnostics.CodeAnalysis;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;
using ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Persistencia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.LambdaConsumerSQS;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddProcessamentoPedidos(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        var baseUrl = configuration["PersistenciaApi:UrlBase"]
                      ?? throw new InvalidOperationException("URL da API de persistencia nao configurada.");
        services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        });
        services.AddScoped<IPedidoProcessamentoClient, PedidoProcessamentoHttpClient>();
        services.AddScoped<ProcessadorPedidoService>();

        return services;
    }
}
