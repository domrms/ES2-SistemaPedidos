using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.Worker.Application.Models;

public sealed record PedidoProcessamento(
    Guid Id,
    string ClienteId,
    decimal ValorTotal,
    StatusPedido Status);
