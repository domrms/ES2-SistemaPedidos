using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.Shared.Contracts;

public sealed record EventoSolicitacaoCliente(
    [property: JsonPropertyName("clienteId")]
    int ClienteId,
    [property: JsonPropertyName("produtoId")]
    int ProdutoId,
    [property: JsonPropertyName("eventoId")]
    string EventoId,
    [property: JsonPropertyName("dataHoraRequisicao")]
    DateTimeOffset DataHoraRequisicao);

public sealed record RequisicaoProcessamentoPedido(
    int ClienteId,
    int ProdutoId,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);

public sealed record RequisicaoErroProcessamentoPedido(
    RequisicaoProcessamentoPedido Pedido,
    string Detalhe);