namespace ES2_SistemaPedidos.Api;

public sealed record RequisicaoCriarSolicitacao(int ClienteId, int ProdutoId);

public sealed record RespostaCriarSolicitacao(
    int ClienteId,
    int ProdutoId,
    string EventoId,
    DateTimeOffset DataHoraRequisicao);

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
