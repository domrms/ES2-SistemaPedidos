using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Setup;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Common;

[Binding]
public class CommonStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private readonly TestContext _testContext;

    public CommonStepDefinitions(ApiE2EFixture fixture, TestContext testContext)
    {
        _fixture = fixture;
        _testContext = testContext;
    }

    [Given(@"que o sistema está pronto")]
    public async Task GivenQueOSistemaEstaPronto()
    {
        await _fixture.InitializeAsync();
    }

    [Given(@"que o sistema está pronto e os dados de teste foram inicializados")]
    public async Task GivenQueOSistemaEstaProntoEDadosForamInicializados()
    {
        await _fixture.InitializeAsync();
    }
    
    [Given(@"que o sistema está pronto e os dados de teste existem")]
    public async Task GivenQueOSistemaEstaProntoEDadosExistem()
    {
        await _fixture.InitializeAsync();
    }

    [Given(@"que não há eventos de teste anteriores")]
    public async Task GivenQueNaoHaEventosAnteriores()
    {
        await _fixture.LimparEventosTeste();
    }

    [When(@"uma solicitação POST é enviada para o endpoint de solicitações com cliente (.*) e produto (.*)")]
    public async Task WhenUmaSolicitacaoPostEnviada(int clienteId, int produtoId)
    {
        var payload = new { clienteId, produtoId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _testContext.Response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);

        if (_testContext.Response.IsSuccessStatusCode)
        {
            var responseContent = await _testContext.Response.Content.ReadAsStringAsync();
            _testContext.SolicitacaoResponse = JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (_testContext.SolicitacaoResponse?.EventoId != null)
            {
                _testContext.EventoIds.Add(_testContext.SolicitacaoResponse.EventoId);
            }
        }
    }
    
    [When(@"uma solicitação POST é enviada com o cliente (.*) e produto (.*)")]
    public async Task WhenUmaSolicitacaoPostEnviadaComClienteEProduto(int clienteId, int produtoId)
    {
        await WhenUmaSolicitacaoPostEnviada(clienteId, produtoId);
    }

    [When(@"uma requisição GET é feita para o endpoint de eventos")]
    public async Task WhenUmaRequisicaoGetEFeitaParaOEndpointDeEventos()
    {
        _testContext.Response = await _fixture.HttpClient.GetAsync("/api/solicitacoes/eventos");
    }

    [Then(@"a resposta deve ser (.*) Accepted")]
    public void ThenARespostaDeveSerAccepted(int statusCode)
    {
        Assert.NotNull(_testContext.Response);
        Assert.Equal(statusCode, (int)_testContext.Response.StatusCode);
    }

    [Then(@"a resposta deve ser (.*) Bad Request")]
    public void ThenARespostaDeveSerBadRequest(int statusCode)
    {
        Assert.NotNull(_testContext.Response);
        Assert.Equal(statusCode, (int)_testContext.Response.StatusCode);
    }
}
