using ES2_SistemaPedidos.PersistenciaApi.Data;
using Microsoft.Extensions.Caching.Memory;

namespace ES2_SistemaPedidos.PersistenciaApi.Application;

public sealed class ConsultaService(
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IEventoRepositorio eventos,
    IPedidoStatusRepositorio status,
    IMemoryCache cache,
    IConfiguration configuration)
{
    public Task<bool> ExisteClienteAsync(int clienteId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync($"cliente:{clienteId}",
            token => clientes.ExisteAsync(clienteId, token), cancellationToken);
    }

    public Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken cancellationToken)
    {
        return GetOrCreateAsync($"produto:{produtoId}",
            token => produtos.ExisteAsync(produtoId, token), cancellationToken);
    }

    public Task<IReadOnlyCollection<EventoDetalhado>> ListarEventosAsync(CancellationToken cancellationToken)
    {
        return eventos.ListarTodosAsync(cancellationToken);
    }

    public Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
        CancellationToken cancellationToken)
    {
        return status.ObterHistoricoAsync(pedidoId, cancellationToken);
    }

    private async Task<bool> GetOrCreateAsync(string key,
        Func<CancellationToken, Task<bool>> factory,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out bool value)) return value;

        value = await factory(cancellationToken);
        var durationSeconds = configuration.GetValue("Cache:DuracaoSegundos", 300);
        cache.Set(key, value, TimeSpan.FromSeconds(durationSeconds));
        return value;
    }
}
