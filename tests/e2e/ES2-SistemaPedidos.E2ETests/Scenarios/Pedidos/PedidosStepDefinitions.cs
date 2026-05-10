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

    [Given(@"que um evento para o cliente (.*) e produto (.*) foi salvo no banco")]
    public async Task GivenQueUmEventoFoiSalvoNoBanco(int clienteId, int produtoId)
    {
        await GivenQueUmaSolicitacaoFoiCriadaComSucesso(clienteId, produtoId);
        await WhenOSistemaProcessaMensagem();
    }

    [When(@"os dados desse evento são consultados")]
    public void WhenOsDadosDesseEventoSaoConsultados()
    {
    }

    [Then(@"os campos do evento devem corresponder aos dados de teste")]
    public async Task ThenOsCamposDoEventoDevemCorresponder()
    {
        var evento = await ObterEventoCriado();

        Assert.Equal(TestData.ClienteId, evento.ClienteId ?? -1);
        Assert.Equal(TestData.ProdutoId, evento.ProdutoId ?? -1);
        Assert.Equal(TestData.NomeCliente, evento.NomeCliente);
        Assert.Equal(TestData.NomeProduto, evento.NomeProduto);
    }

    [Then(@"o timestamp salvoEm deve ser válido")]
    public async Task ThenOTimestampSalvoEmDeveSerValido()
    {
        var evento = await ObterEventoCriado();

        Assert.True(evento.SalvoEm >= evento.DataHoraEvento);
    }

    [Given(@"que o sistema pode ou não conter eventos")]
    public async Task GivenQueOSistemaPodeOuNaoConterEventos()
    {
        await _fixture.InitializeAsync();
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
    }

    [Given(@"que um evento de teste é criado e salvo")]
    public async Task GivenQueUmEventoDeTesteECriadoESalvo()
    {
        await GivenQueUmaSolicitacaoFoiCriadaComSucesso(TestData.ClienteId, TestData.ProdutoId);
        await WhenOSistemaProcessaMensagem();
    }

    [When(@"o método de limpeza de eventos de teste é invocado")]
    public async Task WhenOMetodoDeLimpezaEInvocado()
    {
        await _fixture.LimparEventosTeste();
    }

    [When(@"um novo evento é criado")]
    public async Task WhenUmNovoEventoECriado()
    {
        _testContext.EventoIds.Clear();

        await CriarSolicitacaoAceita(TestData.ClienteId, TestData.ProdutoId);
        await WhenOSistemaProcessaMensagem();
    }

    [Then(@"apenas o segundo evento deve existir no banco de dados")]
    public async Task ThenApenasOSegundoEventoDeveExistir()
    {
        var eventos = await ObterEventosTeste();

        Assert.Single(eventos);
        Assert.Contains(eventos, e => e.EventoId == _testContext.EventoIds.Last());
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

    private async Task<EventoResponse> ObterEventoCriado()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        var eventos = await ObterEventosTeste();
        var evento = eventos.FirstOrDefault(e => e.EventoId == _testContext.SolicitacaoResponse.EventoId);

        Assert.NotNull(evento);
        return evento;
    }
}