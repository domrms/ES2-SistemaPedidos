using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.Api;

public sealed record RequisicaoCriarPedido(
    string? ClienteId,
    IReadOnlyCollection<RequisicaoCriarItemPedido>? Itens,
    decimal ValorTotal);

public sealed record RequisicaoCriarItemPedido(
    string? ProdutoId,
    int Quantidade,
    decimal PrecoUnitario,
    string? Descricao);

public sealed record RespostaCriarPedido(
    Guid PedidoId,
    string ClienteId,
    StatusPedido Status,
    decimal ValorTotal,
    int QuantidadeItens,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

public sealed record RespostaDetalhesPedido(
    Guid PedidoId,
    string ClienteId,
    StatusPedido Status,
    decimal ValorTotal,
    IReadOnlyCollection<RespostaItemPedido> Itens,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm,
    DateTimeOffset? ProcessamentoIniciadoEm,
    DateTimeOffset? ConcluidoEm,
    string? MotivoAprovacao,
    string? MotivoRejeicao,
    string? MensagemErro);

public sealed record RespostaItemPedido(
    Guid ItemPedidoId,
    string ProdutoId,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorLinha,
    string? Descricao);

public sealed record RespostaResumoPedido(
    Guid PedidoId,
    string ClienteId,
    StatusPedido Status,
    decimal ValorTotal,
    int QuantidadeItens,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm,
    DateTimeOffset? ConcluidoEm);

public sealed record RespostaListarPedidos(
    IReadOnlyCollection<RespostaResumoPedido> Pedidos,
    RespostaPaginacao Paginacao);

public sealed record RespostaPaginacao(
    int Pular,
    int Quantidade,
    int Total,
    bool TemMais,
    int QuantidadePaginas);

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
