using System.Globalization;
using System.Security.Cryptography;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.Api.Application.Pedidos;

public sealed class PedidoService(
    IPersistenciaPedidosClient persistenciaPedidos,
    IPublicadorEventoSolicitacao eventPublisher,
    TimeProvider timeProvider)
{
    private const string ValidationFailedCode = "ValidacaoFalhou";
    private const string ValidationFailedMessage = "A validacao da solicitacao falhou";

    public async Task<Resultado<RespostaCriarSolicitacao>> CriarSolicitacaoAsync(
        RequisicaoCriarSolicitacao requisicao,
        CancellationToken cancellationToken)
    {
        if (requisicao.ClienteId <= 0)
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                ValidationFailedCode,
                ValidationFailedMessage,
                [new ErroValidacao("clienteId", "O clienteId deve ser maior que zero.")]));

        if (requisicao.ProdutoId <= 0)
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                ValidationFailedCode,
                ValidationFailedMessage,
                [new ErroValidacao("produtoId", "O produtoId deve ser maior que zero.")]));

        if (!await persistenciaPedidos.ExisteClienteAsync(requisicao.ClienteId, cancellationToken))
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                ValidationFailedCode,
                ValidationFailedMessage,
                [new ErroValidacao("clienteId", $"Cliente {requisicao.ClienteId} nao encontrado.")]));

        if (!await persistenciaPedidos.ExisteProdutoAsync(requisicao.ProdutoId, cancellationToken))
            return Resultado<RespostaCriarSolicitacao>.ValidationFailed(new RespostaErroValidacao(
                ValidationFailedCode,
                ValidationFailedMessage,
                [new ErroValidacao("produtoId", $"Produto {requisicao.ProdutoId} nao encontrado.")]));

        var brasiliaDateTime = GetBrasiliaDateTime(timeProvider.GetUtcNow());
        var evento = new EventoSolicitacaoCliente(
            requisicao.ClienteId,
            requisicao.ProdutoId,
            GenerateEventId(brasiliaDateTime),
            brasiliaDateTime);

        await eventPublisher.PublicarAsync(evento, cancellationToken);

        return Resultado<RespostaCriarSolicitacao>.Success(new RespostaCriarSolicitacao(
            evento.ClienteId,
            evento.ProdutoId,
            evento.EventoId,
            evento.DataHoraRequisicao));
    }

    public async Task<RespostaListarEventos> ListarEventosAsync(CancellationToken cancellationToken)
    {
        var eventos = await persistenciaPedidos.ListarEventosAsync(cancellationToken);
        var eventosDetalhados = eventos.Select(e => new RespostaEventoDetalhado(
                e.Id,
                e.NomeCliente,
                e.NomeProduto,
                e.EventoId,
                GetBrasiliaDateTime(e.DataHoraEvento),
                GetBrasiliaDateTime(e.SalvoEm)))
            .ToList();

        return new RespostaListarEventos(eventosDetalhados.AsReadOnly());
    }

    public async Task<ResultadoConsulta<RespostaHistoricoPedido>> ObterHistoricoAsync(long pedidoId,
        CancellationToken cancellationToken)
    {
        if (pedidoId <= 0)
            return ResultadoConsulta<RespostaHistoricoPedido>.RequisicaoInvalida(new RespostaErro(
                "PedidoIdInvalido",
                "O id do pedido deve ser maior que zero."));

        var historico = await persistenciaPedidos.ObterHistoricoAsync(pedidoId, cancellationToken);
        if (historico is null)
            return ResultadoConsulta<RespostaHistoricoPedido>.NaoEncontrado(new RespostaErro(
                "PedidoNaoEncontrado",
                $"Pedido {pedidoId} nao encontrado."));

        var transicoes = historico.Historico
            .Select(transicao => new RespostaTransicaoPedido(
                transicao.Id,
                transicao.Status,
                GetBrasiliaDateTime(transicao.RegistradoEm),
                transicao.Detalhe))
            .ToList()
            .AsReadOnly();

        return ResultadoConsulta<RespostaHistoricoPedido>.Sucesso(new RespostaHistoricoPedido(
            historico.PedidoId,
            historico.EventoId,
            transicoes));
    }

    private static string GenerateEventId(DateTimeOffset brasiliaDateTime)
    {
        var randomDigits =
            RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        return $"ES2-{randomDigits}-{brasiliaDateTime:HHmmss}";
    }

    private static DateTimeOffset GetBrasiliaDateTime(DateTimeOffset utcDateTime)
    {
        var brasiliaTimeZone = GetBrasiliaTimeZone();
        return TimeZoneInfo.ConvertTime(utcDateTime, brasiliaTimeZone);
    }

    private static TimeZoneInfo GetBrasiliaTimeZone()
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
