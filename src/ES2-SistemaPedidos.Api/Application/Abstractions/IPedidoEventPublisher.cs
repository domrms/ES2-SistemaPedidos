using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Application.Abstractions;

public interface IPublicadorEventoSolicitacao
{
    Task PublicarAsync(EventoSolicitacaoCliente evento, CancellationToken tokenCancelamento);
}