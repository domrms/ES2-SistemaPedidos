using ES2_SistemaPedidos.Worker.Application.Abstractions;
using ES2_SistemaPedidos.Worker.Application.Services;
using ES2_SistemaPedidos.Worker.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Worker;

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
