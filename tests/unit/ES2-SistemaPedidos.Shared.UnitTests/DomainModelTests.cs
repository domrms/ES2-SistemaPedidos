using ES2_SistemaPedidos.Shared.Domain;

namespace ES2_SistemaPedidos.Shared.UnitTests;

public sealed class DomainModelTests
{
    [Fact]
    public void Cliente_deve_expor_valores_informados_no_construtor()
    {
        var cliente = new Cliente(10, "Cliente Teste");

        Assert.Equal(10, cliente.Id);
        Assert.Equal("Cliente Teste", cliente.Nome);
    }

    [Fact]
    public void Produto_deve_expor_valores_informados_no_construtor()
    {
        var produto = new Produto(20, "Produto Teste");

        Assert.Equal(20, produto.Id);
        Assert.Equal("Produto Teste", produto.Nome);
    }

    [Fact]
    public void EventoCliente_deve_expor_valores_informados_no_construtor()
    {
        var dataHoraEvento = new DateTimeOffset(2026, 5, 3, 12, 0, 0, TimeSpan.FromHours(-3));
        var salvoEm = new DateTimeOffset(2026, 5, 3, 15, 0, 1, TimeSpan.Zero);

        var evento = new EventoCliente(1, 10, 20, "ES2-12345678-120000", dataHoraEvento, salvoEm);

        Assert.Equal(1, evento.Id);
        Assert.Equal(10, evento.ClienteId);
        Assert.Equal(20, evento.ProdutoId);
        Assert.Equal("ES2-12345678-120000", evento.EventoId);
        Assert.Equal(dataHoraEvento, evento.DataHoraEvento);
        Assert.Equal(salvoEm, evento.SalvoEm);
        Assert.Null(evento.Cliente);
        Assert.Null(evento.Produto);
    }
}
