namespace ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;

public sealed record EventoProcessamento(
    int ClienteId,
    int ProdutoId,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);
