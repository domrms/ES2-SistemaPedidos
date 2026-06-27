using System.Net;
using System.Text.Json;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.LambdaConsumerSQS.Infrastructure.Persistencia;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.UnitTests;

public sealed class PedidoProcessamentoHttpClientTests
{
    private static readonly EventoProcessamento Evento = new(
        10, 20, "ES2-12345678-120000", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(1));

    [Fact]
    public async Task RegistrarEventoAsync_envia_post_para_api_de_persistencia()
    {
        var manipulador = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
        var cliente = CriarCliente(manipulador);

        await cliente.RegistrarEventoAsync(Evento, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, manipulador.Metodo);
        Assert.Equal("http://persistencia/api/processamentos/pedidos", manipulador.Url?.ToString());
        using var json = JsonDocument.Parse(manipulador.Conteudo!);
        Assert.Equal(Evento.EventoId, json.RootElement.GetProperty("eventoId").GetString());
    }

    [Fact]
    public async Task RegistrarErroAsync_envia_detalhe_para_endpoint_de_erro()
    {
        var manipulador = new FakeHttpMessageHandler(HttpStatusCode.NoContent);
        var cliente = CriarCliente(manipulador);

        await cliente.RegistrarErroAsync(Evento, "Falha controlada", CancellationToken.None);

        Assert.Equal("http://persistencia/api/processamentos/pedidos/erro", manipulador.Url?.ToString());
        using var json = JsonDocument.Parse(manipulador.Conteudo!);
        Assert.Equal("Falha controlada", json.RootElement.GetProperty("detalhe").GetString());
    }

    [Fact]
    public async Task RegistrarEventoAsync_quando_api_falha_lanca_http_request_exception()
    {
        var cliente = CriarCliente(new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            cliente.RegistrarEventoAsync(Evento, CancellationToken.None));
    }

    private static PedidoProcessamentoHttpClient CriarCliente(HttpMessageHandler manipulador)
    {
        return new PedidoProcessamentoHttpClient(new HttpClient(manipulador)
            { BaseAddress = new Uri("http://persistencia/") });
    }

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpMethod? Metodo { get; private set; }
        public Uri? Url { get; private set; }
        public string? Conteudo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Metodo = request.Method;
            Url = request.RequestUri;
            Conteudo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode);
        }
    }
}