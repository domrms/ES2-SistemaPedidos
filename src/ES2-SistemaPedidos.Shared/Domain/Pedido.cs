namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class Cliente
{
    private Cliente()
    {
    }

    public Cliente(int id, string nome)
    {
        Id = id;
        Nome = nome;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;
}

public sealed class EventoCliente
{
    private EventoCliente()
    {
    }

    public EventoCliente(long id, int clienteId, int produtoId, string eventoId, DateTimeOffset dataHoraEvento,
        DateTimeOffset salvoEm)
    {
        Id = id;
        ClienteId = clienteId;
        ProdutoId = produtoId;
        EventoId = eventoId;
        DataHoraEvento = dataHoraEvento;
        SalvoEm = salvoEm;
    }

    public long Id { get; private set; }

    public int ClienteId { get; private set; }

    public int ProdutoId { get; private set; }

    public string EventoId { get; private set; } = string.Empty;

    public DateTimeOffset DataHoraEvento { get; private set; }

    public DateTimeOffset SalvoEm { get; private set; }

    public Cliente? Cliente { get; }

    public Produto? Produto { get; }

    public IReadOnlyCollection<PedidoStatus> HistoricoStatus { get; private set; } = new List<PedidoStatus>();
}

public enum EstadoPedido
{
    Recebido,
    Processando,
    Concluido,
    Erro
}

/// <summary>
///     Registro imutavel de uma transicao de estado do pedido.
/// </summary>
public sealed class PedidoStatus
{
    private PedidoStatus()
    {
    }

    public PedidoStatus(long id, long pedidoId, EstadoPedido status, DateTimeOffset registradoEm,
        string? detalhe = null)
    {
        Id = id;
        PedidoId = pedidoId;
        Status = status;
        RegistradoEm = registradoEm;
        Detalhe = detalhe;
    }

    public long Id { get; private set; }

    public long PedidoId { get; private set; }

    public EstadoPedido Status { get; private set; }

    public DateTimeOffset RegistradoEm { get; private set; }

    public string? Detalhe { get; private set; }

    public EventoCliente? Pedido { get; }
}

public sealed class Produto
{
    private Produto()
    {
    }

    public Produto(int id, string nome)
    {
        Id = id;
        Nome = nome;
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;
}
