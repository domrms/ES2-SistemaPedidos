using System.Net;
using System.Net.Http.Json;
using ES2_SistemaPedidos.Api.Application.Abstractions;

namespace ES2_SistemaPedidos.Api.Infrastructure.Persistencia;

public sealed class PersistenciaPedidosHttpClient(HttpClient clienteHttp) : IPersistenciaPedidosClient
{
    public async Task<bool> ExisteClienteAsync(int clienteId, CancellationToken tokenCancelamento)
    {
        var resposta = await clienteHttp.GetFromJsonAsync<RespostaExistencia>(
            $"api/consultas/clientes/{clienteId}/existe", tokenCancelamento);
        return resposta?.Existe ?? false;
    }

    public async Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken tokenCancelamento)
    {
        var resposta = await clienteHttp.GetFromJsonAsync<RespostaExistencia>(
            $"api/consultas/produtos/{produtoId}/existe", tokenCancelamento);
        return resposta?.Existe ?? false;
    }

    public async Task<IReadOnlyCollection<RespostaEventoDetalhado>> ListarEventosAsync(
        CancellationToken tokenCancelamento)
    {
        var resposta = await clienteHttp.GetFromJsonAsync<RespostaListarEventos>(
            "api/consultas/eventos", tokenCancelamento);
        return resposta?.Eventos ?? [];
    }

    public async Task<RespostaHistoricoPedido?> ObterHistoricoAsync(long pedidoId,
        CancellationToken tokenCancelamento)
    {
        using var resposta = await clienteHttp.GetAsync(
            $"api/consultas/pedidos/{pedidoId}/historico", tokenCancelamento);

        if (resposta.StatusCode == HttpStatusCode.NotFound) return null;

        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadFromJsonAsync<RespostaHistoricoPedido>(tokenCancelamento);
    }

    private sealed record RespostaExistencia(bool Existe);
}
