using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.Shared.Contracts;

public sealed record EventoSolicitacaoCliente(
    [property: JsonPropertyName("clienteId")] int ClienteId,
    [property: JsonPropertyName("produtoId")] int ProdutoId,
    [property: JsonPropertyName("eventoId")] string EventoId,
    [property: JsonPropertyName("dataHoraRequisicao")] DateTimeOffset DataHoraRequisicao);
