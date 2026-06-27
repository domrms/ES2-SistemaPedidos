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
    public static IServiceCollection AddProcessamentoPedidos(this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        servicos.AddSingleton(TimeProvider.System);
        var urlBase = configuracao["PersistenciaApi:UrlBase"]
                      ?? "http://localhost:5080";
        servicos.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(urlBase),
            Timeout = TimeSpan.FromSeconds(15)
        });
        servicos.AddScoped<IPedidoProcessamentoClient, PedidoProcessamentoHttpClient>();
        servicos.AddScoped<ProcessadorPedidoService>();

        return servicos;
    }
}
