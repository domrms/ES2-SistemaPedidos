using ES2_SistemaPedidos.Shared.Domain;
using ES2_SistemaPedidos.Worker.Application.Models;

namespace ES2_SistemaPedidos.Worker.Application.Abstractions;

public interface IPedidoProcessamentoRepositorio
{
    Task<bool> IsMensagemProcessadaAsync(string mensagemId, CancellationToken tokenCancelamento);

    Task<PedidoProcessamento?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento);

    Task MarcarPedidoComoProcessandoAsync(Guid pedidoId, DateTimeOffset agora, CancellationToken tokenCancelamento);

    Task RegistrarPedidoJaProcessadoAsync(string mensagemId, Guid pedidoId, string tipoMensagem, DateTimeOffset agora, CancellationToken tokenCancelamento);

    Task AprovarPedidoAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string motivo,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento);

    Task RejeitarPedidoAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string motivo,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento);

    Task RegistrarFalhaAsync(
        Guid pedidoId,
        string mensagemId,
        string tipoMensagem,
        string erro,
        DateTimeOffset agora,
        CancellationToken tokenCancelamento);
}
