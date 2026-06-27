using ES2_SistemaPedidos.Shared.Domain;
using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.Api;

public sealed record RequisicaoCriarSolicitacao(
    [property: JsonRequired] int ClienteId,
    [property: JsonRequired] int ProdutoId);

public sealed record RespostaCriarSolicitacao(
    int ClienteId,
    int ProdutoId,
    string EventoId,
    DateTimeOffset DataHoraRequisicao);

public sealed record RespostaEventoDetalhado(
    long Id,
    string NomeCliente,
    string NomeProduto,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);

public sealed record RespostaListarEventos(
    IReadOnlyCollection<RespostaEventoDetalhado> Eventos);

public sealed record RespostaHistoricoPedido(
    long PedidoId,
    string EventoId,
    IReadOnlyCollection<RespostaTransicaoPedido> Historico);

public sealed record RespostaTransicaoPedido(
    long Id,
    EstadoPedido Status,
    DateTimeOffset RegistradoEm,
    string? Detalhe);

public enum TipoResultadoConsulta
{
    Sucesso,
    RequisicaoInvalida,
    NaoEncontrado
}

public sealed record ResultadoConsulta<T>(TipoResultadoConsulta Tipo, T? Valor, RespostaErro? Erro)
{
    public static ResultadoConsulta<T> Sucesso(T value)
    {
        return new ResultadoConsulta<T>(TipoResultadoConsulta.Sucesso, value, null);
    }

    public static ResultadoConsulta<T> RequisicaoInvalida(RespostaErro error)
    {
        return new ResultadoConsulta<T>(TipoResultadoConsulta.RequisicaoInvalida, default, error);
    }

    public static ResultadoConsulta<T> NaoEncontrado(RespostaErro error)
    {
        return new ResultadoConsulta<T>(TipoResultadoConsulta.NaoEncontrado, default, error);
    }
}

public sealed record RespostaErro(
    string Erro,
    string Mensagem,
    object? Detalhes = null);

public sealed record ErroValidacao(
    string Campo,
    string Erro);

public sealed record RespostaErroValidacao(
    string Erro,
    string Mensagem,
    IReadOnlyCollection<ErroValidacao> Detalhes);
