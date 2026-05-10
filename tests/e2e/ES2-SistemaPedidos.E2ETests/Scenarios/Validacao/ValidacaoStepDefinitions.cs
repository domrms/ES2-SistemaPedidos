using System.Text;
using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Setup;
using ES2_SistemaPedidos.E2ETests.Support;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Validacao;

[Binding]
public class ValidacaoStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private readonly List<RespostaCriarSolicitacaoResponse> _solicitacaoResponses = new();
    private readonly TestContext _testContext;
    private List<EventoResponse>? _eventosFiltrados;
    private HttpResponseMessage? _response;

    public ValidacaoStepDefinitions(ApiE2EFixture fixture, TestContext testContext)
    {
        _fixture = fixture;
        _testContext = testContext;
    }

    [When(@"duas solicitações são feitas")]
    public async Task WhenDuasSolicitacoesSaoFeitas()
    {
        var r1 = await _fixture.CriarSolicitacaoAsync(TestData.ClienteId, TestData.ProdutoId);
        if (r1 != null) _solicitacaoResponses.Add(r1);

        await Task.Delay(100);

        var r2 = await _fixture.CriarSolicitacaoAsync(TestData.ClienteId, TestData.ProdutoId);
        if (r2 != null) _solicitacaoResponses.Add(r2);
    }

    [Then(@"os eventoIds retornados devem ser diferentes")]
    public void ThenOsEventoIdsDevemSerDiferentes()
    {
        Assert.Equal(2, _solicitacaoResponses.Count);
        Assert.NotEqual(_solicitacaoResponses[0].EventoId, _solicitacaoResponses[1].EventoId);
    }

    [Given(@"que a tabela de eventos de teste está limpa")]
    public async Task GivenQueATabelaDeEventosEstaLimpa()
    {
        await _fixture.InitializeAsync();
        await _fixture.LimparEventosTeste();
    }

    [Then(@"a resposta deve ser (.*) OK e não conter eventos de teste")]
    public async Task ThenARespostaDeveSerOkENaoConterEventosDeTeste(int statusCode)
    {
        Assert.NotNull(_testContext.Response);
        Assert.Equal(statusCode, (int)_testContext.Response.StatusCode);

        var content = await _testContext.Response.Content.ReadAsStringAsync();
        var resposta = JsonSerializer.Deserialize<RespostaEventosResponse>(content, JsonDefaults.CaseInsensitive);

        Assert.NotNull(resposta);
        Assert.DoesNotContain(resposta.Eventos ?? new List<EventoResponse>(), e =>
            e.NomeCliente == TestData.NomeCliente && e.NomeProduto == TestData.NomeProduto);
    }

    [Given(@"que uma solicitação é criada")]
    public async Task GivenQueUmaSolicitacaoECriada()
    {
        await GivenQueATabelaDeEventosEstaLimpa();

        var response = await _fixture.CriarSolicitacaoAsync(TestData.ClienteId, TestData.ProdutoId);
        Assert.NotNull(response);
        _solicitacaoResponses.Add(response);
    }

    [When(@"o evento correspondente é salvo no banco de dados")]
    public async Task WhenOEventoCorrespondenteESalvoNoBanco()
    {
        Assert.Single(_solicitacaoResponses);
        await _fixture.AguardarEventoSalvoNoBanco(
            TestData.ClienteId,
            TestData.ProdutoId,
            _solicitacaoResponses.First().EventoId);
    }

    [Then(@"o timestamp salvoEm deve ser maior ou igual ao dataHoraEvento")]
    public async Task ThenOTimestampSalvoEmDeveSerValido()
    {
        var evento = await ObterEventoCriado();

        Assert.True(evento.SalvoEm >= evento.DataHoraEvento);
    }

    [When(@"uma solicitação POST é enviada com um payload malformado")]
    public async Task WhenUmaSolicitacaoPostEnviadaComPayloadMalformado()
    {
        var content = new StringContent(
            "{\"clienteId\": \"abc\", \"produtoId\": \"xyz\"}",
            Encoding.UTF8,
            "application/json");

        _response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [Then(@"a resposta deve indicar um erro")]
    public void ThenARespostaDeveIndicarUmErro()
    {
        Assert.NotNull(_response);
        Assert.False(_response.IsSuccessStatusCode);
    }

    [When(@"uma solicitação POST é enviada com um JSON vazio")]
    public async Task WhenUmaSolicitacaoPostEnviadaComJsonVazio()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        _response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [When(@"uma solicitação POST é enviada com Content-Type incorreto")]
    public async Task WhenUmaSolicitacaoPostEnviadaComContentTypeIncorreto()
    {
        var content = new StringContent(
            $"clienteId={TestData.ClienteId}&produtoId={TestData.ProdutoId}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        _response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [Then(@"a resposta deve ser (.*) Unsupported Media Type")]
    public void ThenARespostaDeveSerUnsupportedMediaType(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal(statusCode, (int)_response.StatusCode);
    }

    [Given(@"que um evento para o cliente (.*) e produto (.*) é criado")]
    public async Task GivenQueUmEventoEcriado(int clienteId, int produtoId)
    {
        await GivenQueATabelaDeEventosEstaLimpa();

        var response = await _fixture.CriarSolicitacaoAsync(clienteId, produtoId);
        Assert.NotNull(response);
        _solicitacaoResponses.Add(response);

        await _fixture.AguardarEventoSalvoNoBanco(clienteId, produtoId, response.EventoId);
    }

    [When(@"a função de filtragem de eventos é chamada")]
    public async Task WhenAFuncaoDeFiltragemEChamada()
    {
        _eventosFiltrados = await _fixture.ObterEventosPorClienteEProdutoAsync(TestData.ClienteId, TestData.ProdutoId);
    }

    [Then(@"a lista retornada deve conter apenas eventos correspondentes")]
    public void ThenAListaRetornadaDeveConterApenasEventosCorrespondentes()
    {
        Assert.NotNull(_eventosFiltrados);
        Assert.All(_eventosFiltrados, e =>
        {
            Assert.Equal(TestData.ClienteId, e.ClienteId ?? -1);
            Assert.Equal(TestData.ProdutoId, e.ProdutoId ?? -1);
        });
    }

    private async Task<EventoResponse> ObterEventoCriado()
    {
        Assert.NotEmpty(_solicitacaoResponses);

        var eventoId = _solicitacaoResponses.First().EventoId;
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(TestData.ClienteId, TestData.ProdutoId);
        var evento = eventos.FirstOrDefault(e => e.EventoId == eventoId);

        Assert.NotNull(evento);
        return evento;
    }
}