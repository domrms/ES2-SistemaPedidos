namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    private Pedido()
    {
    }

    public Pedido(Guid id, string clienteId, decimal valorTotal, DateTimeOffset criadoEm)
    {
        Id = id;
        ClienteId = clienteId;
        ValorTotal = valorTotal;
        Status = StatusPedido.Pendente;
        CriadoEm = criadoEm;
        AtualizadoEm = criadoEm;
    }

    public Guid Id { get; private set; }

    public string ClienteId { get; private set; } = string.Empty;

    public decimal ValorTotal { get; private set; }

    public StatusPedido Status { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public DateTimeOffset? ProcessamentoIniciadoEm { get; private set; }

    public DateTimeOffset? ConcluidoEm { get; private set; }

    public string? MensagemErro { get; private set; }

    public string? MotivoAprovacao { get; private set; }

    public string? MotivoRejeicao { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens;

    public void AddItemPedido(Guid itemPedidoId, string produtoId, int quantidade, decimal precoUnitario, string? descricao)
    {
        _itens.Add(new ItemPedido(itemPedidoId, Id, produtoId, quantidade, precoUnitario, descricao));
    }

    public void MarkAsProcessando(DateTimeOffset agora)
    {
        TransitionTo(StatusPedido.Processando);
        ProcessamentoIniciadoEm = agora;
        AtualizadoEm = agora;
    }

    public void MarkAsAprovado(string motivo, DateTimeOffset agora)
    {
        TransitionTo(StatusPedido.Aprovado);
        MotivoAprovacao = motivo;
        ConcluidoEm = agora;
        AtualizadoEm = agora;
    }

    public void MarkAsRejeitado(string motivo, DateTimeOffset agora)
    {
        TransitionTo(StatusPedido.Rejeitado);
        MotivoRejeicao = motivo;
        ConcluidoEm = agora;
        AtualizadoEm = agora;
    }

    public void MarkAsFalhou(string mensagemErro, DateTimeOffset agora)
    {
        if (Status == StatusPedido.Falhou)
        {
            return;
        }

        Status = StatusPedido.Falhou;
        MensagemErro = mensagemErro;
        ConcluidoEm = agora;
        AtualizadoEm = agora;
    }

    private void TransitionTo(StatusPedido proximoStatus)
    {
        if (!CanTransitionTo(proximoStatus))
        {
            throw new InvalidOperationException($"Transicao invalida de status do pedido: {Status} para {proximoStatus}.");
        }

        Status = proximoStatus;
    }

    private bool CanTransitionTo(StatusPedido proximoStatus)
    {
        return (Status, proximoStatus) switch
        {
            (StatusPedido.Pendente, StatusPedido.Processando) => true,
            (StatusPedido.Processando, StatusPedido.Aprovado) => true,
            (StatusPedido.Processando, StatusPedido.Rejeitado) => true,
            (_, StatusPedido.Falhou) => true,
            _ => false
        };
    }
}
