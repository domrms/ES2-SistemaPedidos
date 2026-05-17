using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Setup;
using ES2_SistemaPedidos.E2ETests.Support;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Pedidos;

[Binding]
public class PedidosStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private readonly TestContext _testContext;

    public PedidosStepDefinitions(ApiE2EFixture fixture, TestContext testContext)
    {
        _fixture = fixture;
        _testContext = testContext;
    }

    [Then(@"o corpo da resposta deve conter o clienteId, produtoId e um eventoId não vazio")]
    public void ThenOCorpoDaRespostaDeveConterDados()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        Assert.Equal(TestData.ClienteId, _testContext.SolicitacaoResponse.ClienteId);
        Assert.Equal(TestData.ProdutoId, _testContext.SolicitacaoResponse.ProdutoId);
        Assert.False(string.IsNullOrEmpty(_testContext.SolicitacaoResponse.EventoId));
    }

    [Given(@"que uma solicitação para o cliente (.*) e produto (.*) foi criada com sucesso")]
    public async Task GivenQueUmaSolicitacaoFoiCriadaComSucesso(int clienteId, int produtoId)
    {
        await _fixture.InitializeAsync();
        await _fixture.LimparEventosTeste();
        await CriarSolicitacaoAceita(clienteId, produtoId);
    }

    [When(@"o sistema processa a mensagem da fila")]
    public async Task WhenOSistemaProcessaMensagem()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        await _fixture.AguardarEventoSalvoNoBanco(
            TestData.ClienteId,
            TestData.ProdutoId,
            _testContext.SolicitacaoResponse.EventoId);
    }

    [Then(@"um registro de evento correspondente deve existir no banco de dados")]
    public async Task ThenUmRegistroDeEventoDeveExistir()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        var eventos = await ObterEventosTeste();

        Assert.Contains(eventos, e => e.EventoId == _testContext.SolicitacaoResponse.EventoId);
    }

    [Then(@"a resposta deve ser (.*) OK")]
    public void ThenARespostaDeveSerOk(int statusCode)
    {
        Assert.NotNull(_testContext.Response);
        Assert.Equal(statusCode, (int)_testContext.Response.StatusCode);
    }

    [Then(@"o corpo da resposta deve conter uma lista de eventos")]
    public async Task ThenOCorpoDaRespostaDeveConterUmaListaDeEventos()
    {
        Assert.NotNull(_testContext.Response);
        var content = await _testContext.Response.Content.ReadAsStringAsync();
        var resposta = JsonSerializer.Deserialize<RespostaEventosResponse>(content, JsonDefaults.CaseInsensitive);

        Assert.NotNull(resposta);
        Assert.IsType<List<EventoResponse>>(resposta.Eventos);
    }

    [When(@"(.*) solicitações para o cliente (.*) e produto (.*) são enviadas")]
    public async Task WhenMultiplasSolicitacoesSaoEnviadas(int quantidade, int clienteId, int produtoId)
    {
        for (var i = 0; i < quantidade; i++)
        {
            await CriarSolicitacaoAceita(clienteId, produtoId);
            await Task.Delay(100);
        }
    }

    [Then(@"(.*) eventos distintos devem ser salvos no banco de dados")]
    public async Task ThenEventosDistintosDevemSerSalvos(int quantidade)
    {
        foreach (var eventoId in _testContext.EventoIds)
            await _fixture.AguardarEventoSalvoNoBanco(TestData.ClienteId, TestData.ProdutoId, eventoId);

        var eventos = await ObterEventosTeste();
        Assert.Equal(quantidade, eventos.Count);
        Assert.Equal(quantidade, eventos.Select(e => e.EventoId).Distinct().Count());
    }

    private async Task CriarSolicitacaoAceita(int clienteId, int produtoId)
    {
        _testContext.Response = await _fixture.EnviarSolicitacaoAsync(clienteId, produtoId);
        Assert.Equal(202, (int)_testContext.Response.StatusCode);

        var responseContent = await _testContext.Response.Content.ReadAsStringAsync();
        var solicitacaoResponse = JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(
            responseContent,
            JsonDefaults.CaseInsensitive);

        Assert.NotNull(solicitacaoResponse);
        _testContext.SolicitacaoResponse = solicitacaoResponse;
        _testContext.EventoIds.Add(solicitacaoResponse.EventoId);
    }

    private Task<List<EventoResponse>> ObterEventosTeste()
    {
        return _fixture.ObterEventosPorClienteEProdutoAsync(TestData.ClienteId, TestData.ProdutoId);
    }
}
