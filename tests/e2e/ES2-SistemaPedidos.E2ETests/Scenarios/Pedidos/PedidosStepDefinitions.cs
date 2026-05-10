using System.Text.Json;
using ES2_SistemaPedidos.E2ETests.Setup;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Pedidos;

[Binding]
public class PedidosStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private readonly TestContext _testContext;
    private const int ClienteId = 9999;
    private const int ProdutoId = 9999;

    public PedidosStepDefinitions(ApiE2EFixture fixture, TestContext testContext)
    {
        _fixture = fixture;
        _testContext = testContext;
    }

    [Then(@"o corpo da resposta deve conter o clienteId, produtoId e um eventoId não vazio")]
    public void ThenOCorpoDaRespostaDeveConterDados()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        Assert.Equal(ClienteId, _testContext.SolicitacaoResponse.ClienteId);
        Assert.Equal(ProdutoId, _testContext.SolicitacaoResponse.ProdutoId);
        Assert.False(string.IsNullOrEmpty(_testContext.SolicitacaoResponse.EventoId));
    }
    
    [Given(@"que uma solicitação para o cliente (.*) e produto (.*) foi criada com sucesso")]
    public async Task GivenQueUmaSolicitacaoFoiCriadaComSucesso(int clienteId, int produtoId)
    {
        await _fixture.InitializeAsync();
        await _fixture.LimparEventosTeste();
        
        var payload = new { clienteId, produtoId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _testContext.Response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);

        Assert.Equal(202, (int)_testContext.Response.StatusCode);
        
        var responseContent = await _testContext.Response.Content.ReadAsStringAsync();
        _testContext.SolicitacaoResponse = JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    [When(@"o sistema processa a mensagem da fila")]
    public async Task WhenOSistemaProcessaMensagem()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        await _fixture.AguardarEventoSalvoNoBanco(ClienteId, ProdutoId, _testContext.SolicitacaoResponse.EventoId);
    }

    [Then(@"um registro de evento correspondente deve existir no banco de dados")]
    public async Task ThenUmRegistroDeEventoDeveExistir()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(ClienteId, ProdutoId);
        Assert.NotNull(eventos);
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
        // A consulta já foi feita no step anterior ao aguardar o evento
    }

    [Then(@"os campos do evento devem corresponder aos dados de teste")]
    public async Task ThenOsCamposDoEventoDevemCorresponder()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(ClienteId, ProdutoId);
        var evento = eventos?.FirstOrDefault(e => e.EventoId == _testContext.SolicitacaoResponse.EventoId);

        Assert.NotNull(evento);
        Assert.Equal(ClienteId, evento.ClienteId ?? -1);
        Assert.Equal(ProdutoId, evento.ProdutoId ?? -1);
        Assert.Equal("Cliente E2E Test", evento.NomeCliente);
        Assert.Equal("Produto E2E Test", evento.NomeProduto);
    }

    [Then(@"o timestamp salvoEm deve ser válido")]
    public async Task ThenOTimestampSalvoEmDeveSerValido()
    {
        Assert.NotNull(_testContext.SolicitacaoResponse);
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(ClienteId, ProdutoId);
        var evento = eventos?.FirstOrDefault(e => e.EventoId == _testContext.SolicitacaoResponse.EventoId);

        Assert.NotNull(evento);
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
        var resposta = JsonSerializer.Deserialize<RespostaEventosResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(resposta);
        Assert.IsType<List<EventoResponse>>(resposta.Eventos);
    }

    [When(@"(.*) solicitações para o cliente (.*) e produto (.*) são enviadas")]
    public async Task WhenMultiplasSolicitacoesSaoEnviadas(int quantidade, int clienteId, int produtoId)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var payload = new { clienteId, produtoId };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            _testContext.Response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
            
            var responseContent = await _testContext.Response.Content.ReadAsStringAsync();
            var solicitacaoResponse = JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if(solicitacaoResponse != null)
                _testContext.EventoIds.Add(solicitacaoResponse.EventoId);

            await Task.Delay(100);
        }
    }

    [Then(@"(.*) eventos distintos devem ser salvos no banco de dados")]
    public async Task ThenEventosDistintosDevemSerSalvos(int quantidade)
    {
        foreach (var eventoId in _testContext.EventoIds)
        {
            await _fixture.AguardarEventoSalvoNoBanco(ClienteId, ProdutoId, eventoId);
        }

        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(ClienteId, ProdutoId);
        Assert.NotNull(eventos);
        Assert.Equal(quantidade, eventos.Count);
    }
    
    [Given(@"que um evento de teste é criado e salvo")]
    public async Task GivenQueUmEventoDeTesteECriadoESalvo()
    {
        await GivenQueUmaSolicitacaoFoiCriadaComSucesso(ClienteId, ProdutoId);
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
        
        var payload = new { ClienteId, ProdutoId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        _testContext.Response = await _fixture.HttpClient.PostAsync("/api/solicitacoes", content);
        Assert.Equal(202, (int)_testContext.Response.StatusCode);
        
        var responseContent = await _testContext.Response.Content.ReadAsStringAsync();
        var solicitacaoResponse = JsonSerializer.Deserialize<RespostaCriarSolicitacaoResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(solicitacaoResponse);
        _testContext.SolicitacaoResponse = solicitacaoResponse;
        _testContext.EventoIds.Add(solicitacaoResponse.EventoId);

        await WhenOSistemaProcessaMensagem();
    }

    [Then(@"apenas o segundo evento deve existir no banco de dados")]
    public async Task ThenApenasOSegundoEventoDeveExistir()
    {
        var eventos = await _fixture.ObterEventosPorClienteEProdutoAsync(ClienteId, ProdutoId);
        Assert.NotNull(eventos);
        Assert.Single(eventos);
        Assert.Contains(eventos, e => e.EventoId == _testContext.EventoIds.Last());
    }
}
