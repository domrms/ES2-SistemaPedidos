namespace ES2_SistemaPedidos.Worker.Application.Models;

public sealed record EventoProcessamento(
    int ClienteId,
    Guid EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);