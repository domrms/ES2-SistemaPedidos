using ES2_SistemaPedidos.Worker.Configuracoes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderProcessingOptions(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var opcoes = new ProcessamentoPedidosOptions
        {
            FilaUrl = configuracao["SQS_FILA_URL"]
                ?? configuracao["SQS_QUEUE_URL"]
                ?? configuracao["AWS:FilaSolicitacoesUrl"]
                ?? configuracao["AWS:FilaPedidosUrl"]
                ?? configuracao["AWS:SqsQueueUrl"]
                ?? "http://localhost:4566/000000000000/processamento-solicitacoes"
        };

        servicos.AddSingleton(opcoes);
        return servicos;
    }
}
