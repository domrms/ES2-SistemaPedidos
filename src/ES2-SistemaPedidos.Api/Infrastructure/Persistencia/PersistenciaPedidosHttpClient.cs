using System.Net;
using ES2_SistemaPedidos.Api.Application.Abstractions;

namespace ES2_SistemaPedidos.Api.Infrastructure.Persistencia;

public sealed class PersistenciaPedidosHttpClient(HttpClient httpClient) : IPersistenciaPedidosClient
{
    public async Task<bool> ExisteClienteAsync(int clienteId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<RespostaExistencia>(
            $"api/consultas/clientes/{clienteId}/existe", cancellationToken);
        return response?.Existe ?? false;
    }

    public async Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<RespostaExistencia>(
            $"api/consultas/produtos/{produtoId}/existe", cancellationToken);
        return response?.Existe ?? false;
    }

    public async Task<IReadOnlyCollection<RespostaEventoDetalhado>> ListarEventosAsync(
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<RespostaListarEventos>(
            "api/consultas/eventos", cancellationToken);
        return response?.Eventos ?? [];
    }

    public async Task<RespostaHistoricoPedido?> ObterHistoricoAsync(long pedidoId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/consultas/pedidos/{pedidoId}/historico", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RespostaHistoricoPedido>(cancellationToken);
    }

    private sealed record RespostaExistencia(bool Existe);
}
