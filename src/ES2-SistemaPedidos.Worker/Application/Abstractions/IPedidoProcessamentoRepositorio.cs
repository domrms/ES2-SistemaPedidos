using ES2_SistemaPedidos.Worker.Application.Models;

namespace ES2_SistemaPedidos.Worker.Application.Abstractions;

public interface IPedidoProcessamentoRepositorio
{
    Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento);
}
