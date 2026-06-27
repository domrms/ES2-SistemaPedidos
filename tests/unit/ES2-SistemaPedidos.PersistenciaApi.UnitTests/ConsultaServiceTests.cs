using ES2_SistemaPedidos.PersistenciaApi;
using ES2_SistemaPedidos.PersistenciaApi.Application;
using ES2_SistemaPedidos.PersistenciaApi.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ES2_SistemaPedidos.PersistenciaApi.UnitTests;

public sealed class ConsultaServiceTests
{
    [Fact]
    public async Task ExisteClienteAsync_quando_repetido_consulta_repositorio_uma_vez()
    {
        var cliente = new FakeClienteRepositorio(true);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servico = CriarServico(cliente, new FakeProdutoRepositorio(true), cache);

        Assert.True(await servico.ExisteClienteAsync(10, CancellationToken.None));
        Assert.True(await servico.ExisteClienteAsync(10, CancellationToken.None));

        Assert.Equal(1, cliente.Consultas);
    }

    [Fact]
    public async Task ExisteProdutoAsync_quando_repetido_consulta_repositorio_uma_vez()
    {
        var produto = new FakeProdutoRepositorio(false);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servico = CriarServico(new FakeClienteRepositorio(true), produto, cache);

        Assert.False(await servico.ExisteProdutoAsync(20, CancellationToken.None));
        Assert.False(await servico.ExisteProdutoAsync(20, CancellationToken.None));

        Assert.Equal(1, produto.Consultas);
    }

    [Fact]
    public async Task ExisteClienteAsync_para_ids_distintos_mantem_entradas_independentes()
    {
        var cliente = new FakeClienteRepositorio(true);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var servico = CriarServico(cliente, new FakeProdutoRepositorio(true), cache);

        await servico.ExisteClienteAsync(10, CancellationToken.None);
        await servico.ExisteClienteAsync(11, CancellationToken.None);

        Assert.Equal(2, cliente.Consultas);
    }

    private static ConsultaService CriarServico(IClienteRepositorio cliente, IProdutoRepositorio produto,
        IMemoryCache cache)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cache:DuracaoSegundos"] = "300" })
            .Build();
        return new ConsultaService(cliente, produto, new FakeEventoRepositorio(),
            new FakeStatusRepositorio(), cache, configuracao);
    }

    private sealed class FakeClienteRepositorio(bool resultado) : IClienteRepositorio
    {
        public int Consultas { get; private set; }

        public Task<bool> ExisteAsync(int clienteId, CancellationToken tokenCancelamento)
        {
            Consultas++;
            return Task.FromResult(resultado);
        }
    }

    private sealed class FakeProdutoRepositorio(bool resultado) : IProdutoRepositorio
    {
        public int Consultas { get; private set; }

        public Task<bool> ExisteAsync(int produtoId, CancellationToken tokenCancelamento)
        {
            Consultas++;
            return Task.FromResult(resultado);
        }
    }

    private sealed class FakeEventoRepositorio : IEventoRepositorio
    {
        public Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken tokenCancelamento) =>
            Task.FromResult<IReadOnlyCollection<EventoDetalhado>>([]);
    }

    private sealed class FakeStatusRepositorio : IPedidoStatusRepositorio
    {
        public Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
            CancellationToken tokenCancelamento) => Task.FromResult<HistoricoPedidoDetalhado?>(null);
    }
}
