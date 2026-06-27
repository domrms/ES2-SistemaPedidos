using ES2_SistemaPedidos.PersistenciaApi.Data;
using Microsoft.Extensions.Caching.Memory;

namespace ES2_SistemaPedidos.PersistenciaApi.Application;

public sealed class ConsultaService(
    IClienteRepositorio clientes,
    IProdutoRepositorio produtos,
    IEventoRepositorio eventos,
    IPedidoStatusRepositorio status,
    IMemoryCache cache,
    IConfiguration configuracao)
{
    public Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento)
    {
        return ObterOuCriarAsync($"cliente:{clienteId}",
            token => clientes.ExisteAsync(clienteId, token), tokenCancelamento);
    }

    public Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento)
    {
        return ObterOuCriarAsync($"produto:{produtoId}",
            token => produtos.ExisteAsync(produtoId, token), tokenCancelamento);
    }

    public Task<IReadOnlyCollection<EventoDetalhado>> ListarEventosAsync(CancellationToken tokenCancelamento)
    {
        return eventos.ListarTodosAsync(tokenCancelamento);
    }

    public Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
        CancellationToken tokenCancelamento)
    {
        return status.ObterHistoricoAsync(pedidoId, tokenCancelamento);
    }

    private async Task<bool> ObterOuCriarAsync(string chave,
        Func<CancellationToken, Task<bool>> fabrica,
        CancellationToken tokenCancelamento)
    {
        if (cache.TryGetValue(chave, out bool valor)) return valor;

        valor = await fabrica(tokenCancelamento);
        var segundos = configuracao.GetValue("Cache:DuracaoSegundos", 300);
        cache.Set(chave, valor, TimeSpan.FromSeconds(segundos));
        return valor;
    }
}