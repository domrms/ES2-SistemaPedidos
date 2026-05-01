using System.Text.Json.Serialization;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using ES2_SistemaPedidos.Api;
using ES2_SistemaPedidos.Api.Security;
using ES2_SistemaPedidos.Api.Services;
using ES2_SistemaPedidos.Shared;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;

var construtorAplicacao = WebApplication.CreateBuilder(args);

construtorAplicacao.Services.Configure<JsonOptions>(opcoes =>
{
    opcoes.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

construtorAplicacao.Services.AddPersistenciaPedidos(construtorAplicacao.Configuration);
construtorAplicacao.Services.AddScoped<ServicoPedido>();
construtorAplicacao.Services.AddSingleton(TimeProvider.System);
construtorAplicacao.Services.AddSingleton<IPublicadorEventoPedido, PublicadorEventoPedidoSns>();
construtorAplicacao.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
{
    var nomeRegiao = construtorAplicacao.Configuration["AWS_REGIAO"]
        ?? construtorAplicacao.Configuration["AWS_REGION"]
        ?? construtorAplicacao.Configuration["AWS:Regiao"]
        ?? construtorAplicacao.Configuration["AWS:Region"]
        ?? "us-east-1";
    var configuracaoSns = new AmazonSimpleNotificationServiceConfig
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(nomeRegiao)
    };

    var urlServico = construtorAplicacao.Configuration["AWS_URL_SERVICO"]
        ?? construtorAplicacao.Configuration["AWS_ENDPOINT_URL"]
        ?? construtorAplicacao.Configuration["AWS:UrlServico"]
        ?? construtorAplicacao.Configuration["AWS:ServiceUrl"];
    if (!string.IsNullOrWhiteSpace(urlServico))
    {
        configuracaoSns.ServiceURL = urlServico;
        configuracaoSns.AuthenticationRegion = nomeRegiao;
        return new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("test", "test"), configuracaoSns);
    }

    return new AmazonSimpleNotificationServiceClient(configuracaoSns);
});

construtorAplicacao.Services
    .AddAuthentication(PadroesAutenticacaoBearerSimples.EsquemaAutenticacao)
    .AddScheme<AuthenticationSchemeOptions, ManipuladorAutenticacaoBearerSimples>(
        PadroesAutenticacaoBearerSimples.EsquemaAutenticacao,
        opcoes => { });
construtorAplicacao.Services.AddAuthorization();

var aplicacao = construtorAplicacao.Build();

aplicacao.UseAuthentication();
aplicacao.UseAuthorization();

aplicacao.MapGet("/api/saude", () => Results.Ok(new
{
    estado = "saudavel",
    dataHora = DateTimeOffset.UtcNow,
    versao = "1.0.0"
}));

var pedidos = aplicacao.MapGroup("/api/pedidos")
    .RequireAuthorization();

pedidos.MapPost("/", async (RequisicaoCriarPedido requisicao, ServicoPedido servicoPedido, HttpContext contextoHttp, CancellationToken tokenCancelamento) =>
{
    Resultado<RespostaCriarPedido> resultado;
    try
    {
        resultado = await servicoPedido.CreatePedidoAsync(requisicao, contextoHttp.TraceIdentifier, tokenCancelamento);
    }
    catch (Exception excecao) when (IsFalhaDependencia(excecao))
    {
        return Results.Json(
            new RespostaErro("ServicoIndisponivel", "Banco de dados ou mensageria temporariamente indisponivel", new { tentarNovamenteApos = 30 }),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return resultado.Match<IResult>(
        sucesso => Results.Created($"/api/pedidos/{sucesso.PedidoId}", sucesso),
        validacao => Results.BadRequest(validacao));
});

pedidos.MapGet("/{pedidoId:guid}", async (Guid pedidoId, ServicoPedido servicoPedido, CancellationToken tokenCancelamento) =>
{
    var pedido = await servicoPedido.GetPedidoPorIdAsync(pedidoId, tokenCancelamento);

    return pedido is null
        ? Results.NotFound(new RespostaErro("PedidoNaoEncontrado", $"Pedido com ID {pedidoId} nao encontrado"))
        : Results.Ok(pedido);
});

pedidos.MapGet("/", async (
    string clienteId,
    StatusPedido? status,
    int? pular,
    int? quantidade,
    DateOnly? dataDe,
    DateOnly? dataAte,
    ServicoPedido servicoPedido,
    CancellationToken tokenCancelamento) =>
{
    var resultado = await servicoPedido.ListPedidosAsync(clienteId, status, pular ?? 0, quantidade ?? 20, dataDe, dataAte, tokenCancelamento);

    return resultado.Match<IResult>(
        sucesso => Results.Ok(sucesso),
        validacao => Results.BadRequest(validacao));
});

aplicacao.Run();

static bool IsFalhaDependencia(Exception excecao)
{
    return excecao is DbUpdateException
        or AmazonServiceException
        or HttpRequestException
        || excecao is InvalidOperationException invalidOperationException
        && invalidOperationException.Message.Contains("SNS", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
