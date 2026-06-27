using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;

public interface IPedidoProcessamentoClient
{
    Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento);

    Task RegistrarErroAsync(EventoProcessamento evento, string detalhe, CancellationToken tokenCancelamento);
}