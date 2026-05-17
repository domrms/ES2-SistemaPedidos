using System.Text;
using ES2_SistemaPedidos.E2ETests.Setup;
using ES2_SistemaPedidos.E2ETests.Support;
using Reqnroll;
using Xunit;

namespace ES2_SistemaPedidos.E2ETests.Scenarios.Validacao;

[Binding]
public class ValidacaoStepDefinitions
{
    private readonly ApiE2EFixture _fixture;
    private readonly TestContext _testContext;

    public ValidacaoStepDefinitions(ApiE2EFixture fixture, TestContext testContext)
    {
        _fixture = fixture;
        _testContext = testContext;
    }

    [When(@"uma solicitação POST é enviada com tipos inválidos")]
    public async Task WhenUmaSolicitacaoPostEnviadaComTiposInvalidos()
    {
        var content = new StringContent(
            "{\"clienteId\": \"abc\", \"produtoId\": \"xyz\"}",
            Encoding.UTF8,
            "application/json");

        _testContext.Response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [When(@"uma solicitação POST é enviada com um JSON vazio")]
    public async Task WhenUmaSolicitacaoPostEnviadaComJsonVazio()
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        _testContext.Response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [When(@"uma solicitação POST é enviada com Content-Type incorreto")]
    public async Task WhenUmaSolicitacaoPostEnviadaComContentTypeIncorreto()
    {
        var content = new StringContent(
            "clienteId=9999&produtoId=9999",
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        _testContext.Response = await _fixture.HttpClient.PostAsync(ApiRoutes.Solicitacoes, content);
    }

    [Then(@"a resposta deve ser (.*) Unsupported Media Type")]
    public void ThenARespostaDeveSerUnsupportedMediaType(int statusCode)
    {
        Assert.NotNull(_testContext.Response);
        Assert.Equal(statusCode, (int)_testContext.Response.StatusCode);
    }
}
