using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain.Repositories;

namespace ES2_SistemaPedidos.Api.Application.Pedidos;

public sealed class ServicoPedido(
    IClienteRepositorio clienteRepositorio,
    IPublicadorEventoSolicitacao publicadorEvento,
    TimeProvider provedorTempo)
{
    public async Task<Resultado<RespostaCriarSolicitacao>> CriarSolicitacaoAsync(
        RequisicaoCriarSolicitacao requisicao,
        CancellationToken tokenCancelamento)
    {
        if (requisicao.ClienteId <= 0)
        {
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("clienteId", "O clienteId deve ser maior que zero.")]));
        }

        if (!await clienteRepositorio.ExisteClienteAsync(requisicao.ClienteId, tokenCancelamento))
        {
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("clienteId", $"Cliente {requisicao.ClienteId} nao encontrado.")]));
        }

        var evento = new EventoSolicitacaoCliente(
            requisicao.ClienteId,
            Guid.NewGuid(),
            provedorTempo.GetUtcNow());

        await publicadorEvento.PublicarAsync(evento, tokenCancelamento);

        return Resultado<RespostaCriarSolicitacao>.Success(new RespostaCriarSolicitacao(
            evento.ClienteId,
            evento.RequisicaoId,
            evento.DataHoraRequisicao));
    }
}
