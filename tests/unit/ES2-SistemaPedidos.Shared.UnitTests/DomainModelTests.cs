using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public void PedidoStatus_deve_expor_transicao_imutavel()
    {
        var registradoEm = new DateTimeOffset(2026, 5, 3, 15, 0, 1, TimeSpan.Zero);

        var status = new PedidoStatus(2, 1, EstadoPedido.Processando, registradoEm, "Em processamento");

        Assert.Equal(2, status.Id);
        Assert.Equal(1, status.PedidoId);
        Assert.Equal(EstadoPedido.Processando, status.Status);
        Assert.Equal(registradoEm, status.RegistradoEm);
        Assert.Equal("Em processamento", status.Detalhe);
        Assert.Null(status.Pedido);
    }

    [Fact]
    public void ApplicationDbContext_deve_mapear_historico_e_rejeitar_alteracoes()
    {
        var opcoes = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=modelo;Username=dev;Password=dev")
            .Options;
        using var contexto = new ApplicationDbContext(opcoes);
        var status = new PedidoStatus(2, 1, EstadoPedido.Recebido, DateTimeOffset.UtcNow);

        var tipoEntidade = contexto.Model.FindEntityType(typeof(PedidoStatus));
        Assert.NotNull(tipoEntidade);
        Assert.Equal("pedido_status", tipoEntidade.GetTableName());

        contexto.Attach(status);
        contexto.Entry(status).State = EntityState.Modified;

        var excecao = Assert.Throws<InvalidOperationException>(() => contexto.SaveChanges());
        Assert.Equal("O historico de status do pedido e imutavel.", excecao.Message);
    }
}