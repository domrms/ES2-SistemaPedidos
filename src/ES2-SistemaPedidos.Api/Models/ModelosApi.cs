namespace ES2_SistemaPedidos.Api;

public sealed record RequisicaoCriarSolicitacao(int ClienteId, int ProdutoId);

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
    Shared.Domain.EstadoPedido Status,
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
    public static ResultadoConsulta<T> Sucesso(T valor) => new(TipoResultadoConsulta.Sucesso, valor, null);

    public static ResultadoConsulta<T> RequisicaoInvalida(RespostaErro erro) =>
        new(TipoResultadoConsulta.RequisicaoInvalida, default, erro);

    public static ResultadoConsulta<T> NaoEncontrado(RespostaErro erro) =>
        new(TipoResultadoConsulta.NaoEncontrado, default, erro);
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
