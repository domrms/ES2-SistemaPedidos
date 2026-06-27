using System.Net.Http.Json;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Persistencia;

public sealed class PedidoProcessamentoHttpClient(HttpClient httpClient) : IPedidoProcessamentoClient
{
    public async Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/processamentos/pedidos", CreateRequest(evento), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RegistrarErroAsync(EventoProcessamento evento, string detalhe,
        CancellationToken cancellationToken)
    {
        var requisicao = new RequisicaoErroProcessamentoPedido(CreateRequest(evento), detalhe);
        using var response = await httpClient.PostAsJsonAsync(
            "api/processamentos/pedidos/erro", requisicao, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static RequisicaoProcessamentoPedido CreateRequest(EventoProcessamento evento)
    {
        return new RequisicaoProcessamentoPedido(evento.ClienteId, evento.ProdutoId, evento.EventoId,
            evento.DataHoraEvento, evento.SalvoEm);
    }
}
