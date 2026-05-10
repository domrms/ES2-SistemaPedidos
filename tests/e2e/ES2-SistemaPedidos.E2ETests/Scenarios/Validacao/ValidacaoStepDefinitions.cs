using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Setup;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Validacao;

[Binding]
public class ValidacaoStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private HttpResponseMessage? _response;
    private readonly List<RespostaCriarSolicitacaoResponse> _solicitacaoResponses = new();
    private List<EventoResponse>? _eventosFiltrados;

    public ValidacaoStepDefinitions(ApiE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Given(@"que o sistema está pronto")]
    public async Task GivenQueOSistemaEstaPronto()
    {
        await _fixture.InitializeAsync();
    }

    [When(@"uma solicitação POST é enviada com o cliente (.*) e produto (.*)")]
    public async Task WhenUmaSolicitacaoPostEnviada(int clienteId, int produtoId)
    {
        var payload = new { clienteId, produtoId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
    }

    [Then(@"a resposta deve ser (.*) Bad Request")]
    public void ThenARespostaDeveSerBadRequest(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal(statusCode, (int)_response.StatusCode);
    }

    [Given(@"que o sistema está pronto e os dados de teste existem")]
    public async Task GivenQueOSistemaEstaProntoEDadosExistem()
    {
        await _fixture.InitializeAsync();
    }

    [Then(@"a resposta deve ser (.*) Accepted")]
    public void ThenARespostaDeveSerAccepted(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal(statusCode, (int)_response.StatusCode);
    }

    [When(@"duas solicitações são feitas")]
    public async Task WhenDuasSolicitacoesSaoFeitas()
    {
        var r1 = await _fixture.CriarSolicitacaoAsync<RespostaCriarSolicitacaoResponse>(9999, 9999);
        if (r1 != null) _solicitacaoResponses.Add(r1);
        await Task.Delay(100);
        var r2 = await _fixture.CriarSolicitacaoAsync<RespostaCriarSolicitacaoResponse>(9999, 9999);
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

    [Then(@"a resposta deve ser (.*) OK e conter uma lista de eventos vazia")]
    public async Task ThenARespostaDeveSerOkEConterListaVazia(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal(statusCode, (int)_response.StatusCode);
        var content = await _response.Content.ReadAsStringAsync();
        var resposta = JsonSerializer.Deserialize<RespostaEventosResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resposta);
        Assert.Empty(resposta.Eventos ?? new List<EventoResponse>());
    }

    [Given(@"que uma solicitação é criada")]
    public async Task GivenQueUmaSolicitacaoECriada()
    {
        await GivenQueATabelaDeEventosEstaLimpa();
        var r = await _fixture.CriarSolicitacaoAsync<RespostaCriarSolicitacaoResponse>(9999, 9999);
        Assert.NotNull(r);
        _solicitacaoResponses.Add(r);
    }

    [When(@"o evento correspondente é salvo no banco de dados")]
    public async Task WhenOEventoCorrespondenteESalvoNoBanco()
    {
        Assert.Single(_solicitacaoResponses);
        await _fixture.AguardarEventoSalvoNoBanco(9999, 9999, _solicitacaoResponses.First().EventoId);
    }

    [Then(@"o timestamp salvoEm deve ser maior ou igual ao dataHoraEvento")]
    public async Task ThenOTimestampSalvoEmDeveSerValido()
    {
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(9999, 9999);
        var evento = eventos?.FirstOrDefault(e => e.EventoId == _solicitacaoResponses.First().EventoId);
        Assert.NotNull(evento);
        Assert.True(evento.SalvoEm >= evento.DataHoraEvento);
    }

    [When(@"uma solicitação POST é enviada com um payload malformado")]
    public async Task WhenUmaSolicitacaoPostEnviadaComPayloadMalformado()
    {
        var json = "{\"clienteId\": \"abc\", \"produtoId\": \"xyz\"}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
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
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        _response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
    }

    [When(@"uma solicitação POST é enviada com Content-Type incorreto")]
    public async Task WhenUmaSolicitacaoPostEnviadaComContentTypeIncorreto()
    {
        var content = new StringContent("clienteId=9999&produtoId=9999", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
        _response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
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
        var r = await _fixture.CriarSolicitacaoAsync<RespostaCriarSolicitacaoResponse>(clienteId, produtoId);
        Assert.NotNull(r);
        _solicitacaoResponses.Add(r);
        await _fixture.AguardarEventoSalvoNoBanco(clienteId, produtoId, r.EventoId);
    }

    [When(@"a função de filtragem de eventos é chamada")]
    public async Task WhenAFuncaoDeFiltragemEChamada()
    {
        _eventosFiltrados = await _fixture.ObterEventosPorClienteEProdutoAsync(9999, 9999);
    }

    [Then(@"a lista retornada deve conter apenas eventos correspondentes")]
    public void ThenAListaRetornadaDeveConterApenasEventosCorrespondentes()
    {
        Assert.NotNull(_eventosFiltrados);
        Assert.All(_eventosFiltrados, e =>
        {
            Assert.Equal(9999, e.ClienteId ?? -1);
            Assert.Equal(9999, e.ProdutoId ?? -1);
        });
    }
}
