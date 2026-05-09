using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;
using ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.LambdaConsumerSQS;

public static class DependencyInjection
{
    public static IServiceCollection AddProcessamentoPedidos(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(TimeProvider.System);
        servicos.AddScoped<IPedidoProcessamentoRepository, PedidoProcessamentoRepositoryDapper>();
        servicos.AddScoped<ProcessadorPedidoService>();

        return servicos;
    }
}
