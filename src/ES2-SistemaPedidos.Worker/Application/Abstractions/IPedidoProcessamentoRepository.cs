using ES2_SistemaPedidos.Worker.Application.Models;

namespace ES2_SistemaPedidos.Worker.Application.Abstractions;

public interface IPedidoProcessamentoRepository
{
    Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento);
}
