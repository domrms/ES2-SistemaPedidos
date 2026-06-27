using System.Globalization;
using System.Security.Cryptography;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Application.Pedidos;

public sealed class PedidoService(
    IPersistenciaPedidosClient persistenciaPedidos,
    IPublicadorEventoSolicitacao publicadorEvento,
    TimeProvider provedorTempo)
{
    public async Task<Resultado<RespostaCriarSolicitacao>> CriarSolicitacaoAsync(
        RequisicaoCriarSolicitacao requisicao,
        CancellationToken tokenCancelamento)
    {
        if (requisicao.ClienteId <= 0)
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("clienteId", "O clienteId deve ser maior que zero.")]));

        if (requisicao.ProdutoId <= 0)
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("produtoId", "O produtoId deve ser maior que zero.")]));

        if (!await persistenciaPedidos.ExisteClienteAsync(requisicao.ClienteId, tokenCancelamento))
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("clienteId", $"Cliente {requisicao.ClienteId} nao encontrado.")]));

        if (!await persistenciaPedidos.ExisteProdutoAsync(requisicao.ProdutoId, tokenCancelamento))
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                "ValidacaoFalhou",
                "A validacao da solicitacao falhou",
                [new ErroValidacao("produtoId", $"Produto {requisicao.ProdutoId} nao encontrado.")]));

        var dataHoraBrasilia = ObterDataHoraBrasilia(provedorTempo.GetUtcNow());
        var evento = new EventoSolicitacaoCliente(
            requisicao.ClienteId,
            requisicao.ProdutoId,
            GerarEventoId(dataHoraBrasilia),
            dataHoraBrasilia);

        await publicadorEvento.PublicarAsync(evento, tokenCancelamento);

        return Resultado<RespostaCriarSolicitacao>.Success(new RespostaCriarSolicitacao(
            evento.ClienteId,
            evento.ProdutoId,
            evento.EventoId,
            evento.DataHoraRequisicao));
    }

    public async Task<RespostaListarEventos> ListarEventosAsync(CancellationToken tokenCancelamento)
    {
        var eventos = await persistenciaPedidos.ListarEventosAsync(tokenCancelamento);
        var eventosDetalhados = eventos.Select(e => new RespostaEventoDetalhado(
                e.Id,
                e.NomeCliente,
                e.NomeProduto,
                e.EventoId,
                ObterDataHoraBrasilia(e.DataHoraEvento),
                ObterDataHoraBrasilia(e.SalvoEm)))
            .ToList();

        return new RespostaListarEventos(eventosDetalhados.AsReadOnly());
    }

    public async Task<ResultadoConsulta<RespostaHistoricoPedido>> ObterHistoricoAsync(long pedidoId,
        CancellationToken tokenCancelamento)
    {
        if (pedidoId <= 0)
            return ResultadoConsulta<RespostaHistoricoPedido>.RequisicaoInvalida(new RespostaErro(
                "PedidoIdInvalido",
                "O id do pedido deve ser maior que zero."));

        var historico = await persistenciaPedidos.ObterHistoricoAsync(pedidoId, tokenCancelamento);
        if (historico is null)
            return ResultadoConsulta<RespostaHistoricoPedido>.NaoEncontrado(new RespostaErro(
                "PedidoNaoEncontrado",
                $"Pedido {pedidoId} nao encontrado."));

        var transicoes = historico.Historico
            .Select(transicao => new RespostaTransicaoPedido(
                transicao.Id,
                transicao.Status,
                ObterDataHoraBrasilia(transicao.RegistradoEm),
                transicao.Detalhe))
            .ToList()
            .AsReadOnly();

        return ResultadoConsulta<RespostaHistoricoPedido>.Sucesso(new RespostaHistoricoPedido(
            historico.PedidoId,
            historico.EventoId,
            transicoes));
    }

    private static string GerarEventoId(DateTimeOffset dataHoraBrasilia)
    {
        var numerosAleatorios =
            RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        return $"ES2-{numerosAleatorios}-{dataHoraBrasilia:HHmmss}";
    }

    private static DateTimeOffset ObterDataHoraBrasilia(DateTimeOffset dataHoraUtc)
    {
        var fusoBrasilia = ObterFusoBrasilia();
        return TimeZoneInfo.ConvertTime(dataHoraUtc, fusoBrasilia);
    }

    private static TimeZoneInfo ObterFusoBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
    }
}