using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.Shared.Contracts;

public sealed record EventoPedidoCriado(
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("tipoEvento")] string TipoEvento,
    [property: JsonPropertyName("versao")] string Versao,
    [property: JsonPropertyName("publicadoEm")] DateTimeOffset PublicadoEm,
    [property: JsonPropertyName("pedidoId")] Guid PedidoId,
    [property: JsonPropertyName("clienteId")] string ClienteId,
    [property: JsonPropertyName("valorTotal")] decimal ValorTotal,
    [property: JsonPropertyName("moeda")] string Moeda,
    [property: JsonPropertyName("itens")] IReadOnlyCollection<ItemEventoPedido> Itens,
    [property: JsonPropertyName("correlacaoId")] string? CorrelacaoId,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("metadados")] IReadOnlyDictionary<string, string>? Metadados);

public sealed record EventoPedidoAprovado(
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("tipoEvento")] string TipoEvento,
    [property: JsonPropertyName("versao")] string Versao,
    [property: JsonPropertyName("publicadoEm")] DateTimeOffset PublicadoEm,
    [property: JsonPropertyName("pedidoId")] Guid PedidoId,
    [property: JsonPropertyName("clienteId")] string ClienteId,
    [property: JsonPropertyName("motivoAprovacao")] string MotivoAprovacao,
    [property: JsonPropertyName("servicoAprovador")] string ServicoAprovador,
    [property: JsonPropertyName("valorLimite")] decimal? ValorLimite,
    [property: JsonPropertyName("valorPedido")] decimal? ValorPedido,
    [property: JsonPropertyName("correlacaoId")] string? CorrelacaoId,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("metadados")] IReadOnlyDictionary<string, string>? Metadados);

public sealed record EventoPedidoRejeitado(
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("tipoEvento")] string TipoEvento,
    [property: JsonPropertyName("versao")] string Versao,
    [property: JsonPropertyName("publicadoEm")] DateTimeOffset PublicadoEm,
    [property: JsonPropertyName("pedidoId")] Guid PedidoId,
    [property: JsonPropertyName("clienteId")] string ClienteId,
    [property: JsonPropertyName("motivoRejeicao")] string MotivoRejeicao,
    [property: JsonPropertyName("servicoRejeitador")] string ServicoRejeitador,
    [property: JsonPropertyName("valorLimite")] decimal? ValorLimite,
    [property: JsonPropertyName("valorPedido")] decimal? ValorPedido,
    [property: JsonPropertyName("requerRevisaoManual")] bool RequerRevisaoManual,
    [property: JsonPropertyName("correlacaoId")] string? CorrelacaoId,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("metadados")] IReadOnlyDictionary<string, string>? Metadados);

public sealed record EventoProcessamentoPedidoFalhou(
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("tipoEvento")] string TipoEvento,
    [property: JsonPropertyName("versao")] string Versao,
    [property: JsonPropertyName("publicadoEm")] DateTimeOffset PublicadoEm,
    [property: JsonPropertyName("pedidoId")] Guid PedidoId,
    [property: JsonPropertyName("clienteId")] string ClienteId,
    [property: JsonPropertyName("motivoFalha")] string MotivoFalha,
    [property: JsonPropertyName("codigoErro")] string CodigoErro,
    [property: JsonPropertyName("mensagemErro")] string MensagemErro,
    [property: JsonPropertyName("rastreamentoPilha")] string? RastreamentoPilha,
    [property: JsonPropertyName("retentavel")] bool Retentavel,
    [property: JsonPropertyName("correlacaoId")] string? CorrelacaoId,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("metadados")] IReadOnlyDictionary<string, string>? Metadados);

public sealed record ItemEventoPedido(
    [property: JsonPropertyName("produtoId")] string ProdutoId,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("precoUnitario")] decimal PrecoUnitario,
    [property: JsonPropertyName("valorLinha")] decimal ValorLinha,
    [property: JsonPropertyName("descricao")] string? Descricao);
