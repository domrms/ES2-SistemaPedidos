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

    public EventoCliente(long id, int clienteId, int produtoId, string eventoId, DateTimeOffset dataHoraEvento, DateTimeOffset salvoEm)
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

    public Cliente? Cliente { get; private set; }

    public Produto? Produto { get; private set; }
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
