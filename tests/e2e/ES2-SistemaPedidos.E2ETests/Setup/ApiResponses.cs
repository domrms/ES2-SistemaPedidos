using System.Text.Json.Serialization;

namespace ES2_SistemaPedidos.E2ETests.Setup;

public record RespostaEventosResponse(
    [property: JsonPropertyName("eventos")]
    List<EventoResponse>? Eventos);

public record EventoResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("nomeCliente")]
    string NomeCliente,
    [property: JsonPropertyName("nomeProduto")]
    string NomeProduto,
    [property: JsonPropertyName("eventoId")]
    string EventoId,
    [property: JsonPropertyName("dataHoraEvento")]
    DateTimeOffset DataHoraEvento,
    [property: JsonPropertyName("salvoEm")]
    DateTimeOffset SalvoEm,
    [property: JsonPropertyName("clienteId")]
    int? ClienteId = null,
    [property: JsonPropertyName("produtoId")]
    int? ProdutoId = null);

public record RespostaCriarSolicitacaoResponse(
    [property: JsonPropertyName("clienteId")]
    int ClienteId,
    [property: JsonPropertyName("produtoId")]
    int ProdutoId,
    [property: JsonPropertyName("eventoId")]
    string EventoId,
    [property: JsonPropertyName("dataHoraRequisicao")]
    DateTimeOffset DataHoraRequisicao);