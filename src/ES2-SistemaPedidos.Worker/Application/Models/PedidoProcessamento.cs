namespace ES2_SistemaPedidos.Worker.Application.Models;

public sealed record EventoProcessamento(
    int ClienteId,
    int ProdutoId,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);
