using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.PersistenciaApi;

public sealed record RespostaExistencia(bool Existe);

public sealed record EventoDetalhado(
    long Id,
    string NomeCliente,
    string NomeProduto,
    string EventoId,
    DateTimeOffset DataHoraEvento,
    DateTimeOffset SalvoEm);

public sealed record RespostaListarEventos(IReadOnlyCollection<EventoDetalhado> Eventos);

public sealed record HistoricoPedidoDetalhado(
    long PedidoId,
    string EventoId,
    IReadOnlyCollection<TransicaoPedidoDetalhada> Historico);

public sealed record TransicaoPedidoDetalhada(
    long Id,
    EstadoPedido Status,
    DateTimeOffset RegistradoEm,
    string? Detalhe);