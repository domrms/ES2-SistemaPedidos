using System.Net;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using ES2_SistemaPedidos.Api.Application.Abstractions;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Api.Controllers;
using ES2_SistemaPedidos.Api.Infrastructure.Health;
using ES2_SistemaPedidos.Api.Infrastructure.Messaging;
using ES2_SistemaPedidos.Api.Infrastructure.Persistencia;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace ES2_SistemaPedidos.Api.UnitTests;

public sealed class InfrastructureAndControllerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Controller_deve_converter_resultados_do_servico_em_respostas_http()
    {
        var persistencia = new PersistenciaFake();
        var controller = CriarController(persistencia);

        Assert.IsType<AcceptedResult>(await controller.CriarSolicitacaoAsync(new(1, 2), default));
        Assert.IsType<BadRequestObjectResult>(await controller.CriarSolicitacaoAsync(new(0, 2), default));
        Assert.IsType<OkObjectResult>(await controller.ListarEventosAsync(default));
        Assert.IsType<BadRequestObjectResult>(await controller.ObterHistoricoAsync(0, default));
        Assert.IsType<NotFoundObjectResult>(await controller.ObterHistoricoAsync(404, default));
        Assert.IsType<OkObjectResult>(await controller.ObterHistoricoAsync(1, default));
    }

    [Theory]
    [InlineData("criar")]
    [InlineData("listar")]
    [InlineData("historico")]
    public async Task Controller_quando_persistencia_falha_retorna_503(string operacao)
    {
        var controller = CriarController(new PersistenciaFake { Falha = new HttpRequestException("offline") });

        IActionResult resultado = operacao switch
        {
            "criar" => await controller.CriarSolicitacaoAsync(new(1, 2), default),
            "listar" => await controller.ListarEventosAsync(default),
            _ => await controller.ObterHistoricoAsync(1, default)
        };

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, Assert.IsType<ObjectResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task Controller_quando_publicacao_SQS_falha_retorna_503()
    {
        var controller = CriarController(new PersistenciaFake(),
            new PublicadorFake(new InvalidOperationException("SQS indisponivel")));

        var resultado = await controller.CriarSolicitacaoAsync(new(1, 2), default);

        Assert.Equal(503, Assert.IsType<ObjectResult>(resultado).StatusCode);
    }

    [Fact]
    public async Task PersistenciaHttpClient_deve_mapear_todas_as_respostas()
    {
        var handler = new HttpHandlerFake(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/consultas/clientes/1/existe" => Json(HttpStatusCode.OK, "{\"existe\":true}"),
            "/api/consultas/produtos/2/existe" => Json(HttpStatusCode.OK, "null"),
            "/api/consultas/eventos" => Json(HttpStatusCode.OK,
                "{\"eventos\":[{\"id\":3,\"nomeCliente\":\"Ana\",\"nomeProduto\":\"Livro\",\"eventoId\":\"evt\",\"dataHoraEvento\":\"2026-06-27T12:00:00Z\",\"salvoEm\":\"2026-06-27T12:00:01Z\"}]}"),
            "/api/consultas/pedidos/404/historico" => new(HttpStatusCode.NotFound),
            _ => Json(HttpStatusCode.OK,
                "{\"pedidoId\":1,\"eventoId\":\"evt\",\"historico\":[]}")
        });
        var client = new PersistenciaPedidosHttpClient(new HttpClient(handler)
            { BaseAddress = new Uri("http://localhost/") });

        Assert.True(await client.ExisteClienteAsync(1, default));
        Assert.False(await client.ExisteProdutoAsync(2, default));
        Assert.Single(await client.ListarEventosAsync(default));
        Assert.Null(await client.ObterHistoricoAsync(404, default));
        Assert.Equal(1, (await client.ObterHistoricoAsync(1, default))!.PedidoId);
    }

    [Fact]
    public async Task PersistenciaHttpClient_quando_corpo_de_eventos_e_nulo_retorna_lista_vazia()
    {
        var client = new PersistenciaPedidosHttpClient(new HttpClient(new HttpHandlerFake(_ => Json(HttpStatusCode.OK,
            "null"))) { BaseAddress = new Uri("http://localhost/") });

        Assert.Empty(await client.ListarEventosAsync(default));
    }

    [Fact]
    public async Task PublicadorSqs_deve_validar_configuracao_e_enviar_payload()
    {
        var sqs = new SqsFake();
        var semFila = new PedidoPublisherEventSqs(sqs, new ConfigurationBuilder().Build(),
            NullLogger<PedidoPublisherEventSqs>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => semFila.PublicarAsync(Evento(), default));

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["SQS_QUEUE_URL"] = "http://fila/pedidos" }).Build();
        var publicador = new PedidoPublisherEventSqs(sqs, configuracao,
            NullLogger<PedidoPublisherEventSqs>.Instance);

        await publicador.PublicarAsync(Evento(), default);

        Assert.Equal("http://fila/pedidos", sqs.Requisicao!.QueueUrl);
        Assert.Contains("\"clienteId\":1", sqs.Requisicao.MessageBody);
        Assert.Equal("SolicitacaoCliente", sqs.Requisicao.MessageAttributes["tipoEvento"].StringValue);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, HealthStatus.Healthy)]
    [InlineData(HttpStatusCode.ServiceUnavailable, HealthStatus.Unhealthy)]
    public async Task PersistenciaHealthCheck_deve_refletir_status_http(HttpStatusCode status,
        HealthStatus esperado)
    {
        var check = new PersistenciaApiHealthCheck(new HttpClientFactoryFake(
            new HttpClient(new HttpHandlerFake(_ => new HttpResponseMessage(status)))
                { BaseAddress = new Uri("http://localhost/") }));

        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(esperado, resultado.Status);
    }

    [Fact]
    public async Task PersistenciaHealthCheck_quando_cliente_lanca_retorna_unhealthy()
    {
        var check = new PersistenciaApiHealthCheck(new HttpClientFactoryFake(
            new HttpClient(new HttpHandlerFake(_ => throw new HttpRequestException("offline")))
                { BaseAddress = new Uri("http://localhost/") }));

        Assert.Equal(HealthStatus.Unhealthy,
            (await check.CheckHealthAsync(new HealthCheckContext())).Status);
    }

    [Fact]
    public async Task FlociHealthCheck_deve_validar_endpoint_sucesso_e_falha()
    {
        var invalido = new FlociHealthCheck(new HttpClientFactoryFake(new HttpClient()),
            Configuracao(("AWS_ENDPOINT_URL", "invalido")));
        Assert.Equal(HealthStatus.Unhealthy,
            (await invalido.CheckHealthAsync(new HealthCheckContext())).Status);

        var saudavel = new FlociHealthCheck(new HttpClientFactoryFake(
                new HttpClient(new HttpHandlerFake(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))),
            Configuracao(("AWS_ENDPOINT_URL", "http://localhost:4566")));
        Assert.Equal(HealthStatus.Healthy,
            (await saudavel.CheckHealthAsync(new HealthCheckContext())).Status);

        var offline = new FlociHealthCheck(new HttpClientFactoryFake(
                new HttpClient(new HttpHandlerFake(_ => throw new HttpRequestException("offline")))),
            Configuracao(("AWS_ENDPOINT_URL", "http://localhost:4566")));
        Assert.Equal(HealthStatus.Unhealthy,
            (await offline.CheckHealthAsync(new HealthCheckContext())).Status);
    }

    [Fact]
    public async Task HealthCheckResponseWriter_deve_escrever_json_com_detalhes()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>
        {
            ["banco"] = new(HealthStatus.Healthy, "disponivel", TimeSpan.FromMilliseconds(4), null, null)
        }, TimeSpan.FromMilliseconds(5));

        await HealthCheckResponseWriter.WriteAsync(context, report);
        context.Response.Body.Position = 0;
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Contains("\"estado\":\"healthy\"", json);
        Assert.Contains("\"banco\"", json);
    }

    private static PedidosController CriarController(PersistenciaFake persistencia,
        IPublicadorEventoSolicitacao? publicador = null)
    {
        return new PedidosController(new PedidoService(persistencia, publicador ?? new PublicadorFake(),
            new TempoFake(Agora)));
    }

    private static EventoSolicitacaoCliente Evento() => new(1, 2, "evt", Agora);

    private static IConfiguration Configuracao(params (string Chave, string Valor)[] valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores.ToDictionary(x => x.Chave, x => (string?)x.Valor))
            .Build();

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class PersistenciaFake : IPersistenciaPedidosClient
    {
        public Exception? Falha { get; init; }

        public Task<bool> ExisteClienteAsync(int clienteId, CancellationToken cancellationToken)
        {
            if (Falha is not null) throw Falha;
            return Task.FromResult(true);
        }

        public Task<bool> ExisteProdutoAsync(int produtoId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<IReadOnlyCollection<RespostaEventoDetalhado>> ListarEventosAsync(
            CancellationToken cancellationToken)
        {
            if (Falha is not null) throw Falha;
            return Task.FromResult<IReadOnlyCollection<RespostaEventoDetalhado>>([]);
        }

        public Task<RespostaHistoricoPedido?> ObterHistoricoAsync(long pedidoId,
            CancellationToken cancellationToken)
        {
            if (Falha is not null) throw Falha;
            return Task.FromResult<RespostaHistoricoPedido?>(pedidoId == 404
                ? null
                : new RespostaHistoricoPedido(pedidoId, "evt",
                    [new RespostaTransicaoPedido(1, EstadoPedido.Recebido, Agora, null)]));
        }
    }

    private sealed class PublicadorFake(Exception? falha = null) : IPublicadorEventoSolicitacao
    {
        public Task PublicarAsync(EventoSolicitacaoCliente evento, CancellationToken cancellationToken) =>
            falha is null ? Task.CompletedTask : Task.FromException(falha);
    }

    private sealed class TempoFake(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private sealed class HttpHandlerFake(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responder(request));
    }

    private sealed class HttpClientFactoryFake(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class SqsFake() : AmazonSQSClient(new AnonymousAWSCredentials(), RegionEndpoint.USEast1)
    {
        public SendMessageRequest? Requisicao { get; private set; }

        public override Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            Requisicao = request;
            return Task.FromResult(new SendMessageResponse { MessageId = "mensagem-1" });
        }
    }
}
