using System.Net.Http.Json;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.Shared.Contracts;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Persistencia;

public sealed class PedidoProcessamentoHttpClient(HttpClient clienteHttp) : IPedidoProcessamentoClient
{
    public async Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento)
    {
        using var resposta = await clienteHttp.PostAsJsonAsync(
            "api/processamentos/pedidos", CriarRequisicao(evento), tokenCancelamento);
        resposta.EnsureSuccessStatusCode();
    }

    public async Task RegistrarErroAsync(EventoProcessamento evento, string detalhe,
        CancellationToken tokenCancelamento)
    {
        var requisicao = new RequisicaoErroProcessamentoPedido(CriarRequisicao(evento), detalhe);
        using var resposta = await clienteHttp.PostAsJsonAsync(
            "api/processamentos/pedidos/erro", requisicao, tokenCancelamento);
        resposta.EnsureSuccessStatusCode();
    }

    private static RequisicaoProcessamentoPedido CriarRequisicao(EventoProcessamento evento)
    {
        return new RequisicaoProcessamentoPedido(evento.ClienteId, evento.ProdutoId, evento.EventoId,
            evento.DataHoraEvento, evento.SalvoEm);
    }
}