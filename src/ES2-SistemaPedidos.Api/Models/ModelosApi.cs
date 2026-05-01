namespace ES2_SistemaPedidos.Api;

public sealed record RequisicaoCriarSolicitacao(int ClienteId);

public sealed record RespostaCriarSolicitacao(
    int ClienteId,
    Guid RequisicaoId,
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