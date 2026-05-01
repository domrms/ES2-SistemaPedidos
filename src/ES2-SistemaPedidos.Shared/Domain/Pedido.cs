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

    public EventoCliente(long id, int clienteId, Guid eventoId, DateTimeOffset dataHoraEvento, DateTimeOffset salvoEm)
    {
        Id = id;
        ClienteId = clienteId;
        EventoId = eventoId;
        DataHoraEvento = dataHoraEvento;
        SalvoEm = salvoEm;
    }

    public long Id { get; private set; }

    public int ClienteId { get; private set; }

    public Guid EventoId { get; private set; }

    public DateTimeOffset DataHoraEvento { get; private set; }

    public DateTimeOffset SalvoEm { get; private set; }

    public Cliente? Cliente { get; private set; }
}