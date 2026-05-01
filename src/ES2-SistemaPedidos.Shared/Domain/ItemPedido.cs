namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class ItemPedido
{
    private ItemPedido()
    {
    }

    public ItemPedido(Guid id, Guid pedidoId, string produtoId, int quantidade, decimal precoUnitario, string? descricao)
    {
        Id = id;
        PedidoId = pedidoId;
        ProdutoId = produtoId;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
        ValorLinha = decimal.Round(quantidade * precoUnitario, 2, MidpointRounding.AwayFromZero);
        Descricao = descricao;
    }

    public Guid Id { get; private set; }

    public Guid PedidoId { get; private set; }

    public string ProdutoId { get; private set; } = string.Empty;

    public int Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    public decimal ValorLinha { get; private set; }

    public string? Descricao { get; private set; }

    public Pedido? Pedido { get; private set; }
}
