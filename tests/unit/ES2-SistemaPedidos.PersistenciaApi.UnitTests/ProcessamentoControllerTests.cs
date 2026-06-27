using ES2_SistemaPedidos.PersistenciaApi.Controllers;
using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.PersistenciaApi.UnitTests;

public sealed class ProcessamentoControllerTests
{
    private static readonly RequisicaoProcessamentoPedido Pedido = new(
        10, 20, "ES2-12345678-120000", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(1));

    [Fact]
    public async Task RegistrarEventoAsync_delega_ao_repositorio_e_retorna_no_content()
    {
        var repositorio = new FakeRepositorio();
        var controller = new ProcessamentoController(repositorio);

        var resultado = await controller.RegistrarEventoAsync(Pedido, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(Pedido, Assert.Single(repositorio.Eventos));
    }

    [Fact]
    public async Task RegistrarErroAsync_delega_pedido_e_detalhe_ao_repositorio()
    {
        var repositorio = new FakeRepositorio();
        var controller = new ProcessamentoController(repositorio);

        var resultado = await controller.RegistrarErroAsync(
            new RequisicaoErroProcessamentoPedido(Pedido, "Falha controlada"), CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
        var erro = Assert.Single(repositorio.Erros);
        Assert.Equal(Pedido, erro.Pedido);
        Assert.Equal("Falha controlada", erro.Detalhe);
    }

    private sealed class FakeRepositorio : IPedidoProcessamentoRepositorio
    {
        public List<RequisicaoProcessamentoPedido> Eventos { get; } = [];
        public List<(RequisicaoProcessamentoPedido Pedido, string Detalhe)> Erros { get; } = [];

        public Task RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido,
            CancellationToken tokenCancelamento)
        {
            Eventos.Add(pedido);
            return Task.CompletedTask;
        }

        public Task RegistrarErroAsync(RequisicaoProcessamentoPedido pedido, string detalhe,
            CancellationToken tokenCancelamento)
        {
            Erros.Add((pedido, detalhe));
            return Task.CompletedTask;
        }
    }
}
