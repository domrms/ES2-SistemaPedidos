using System.Net;

namespace ES2_SistemaPedidos.E2ETests.Setup;

/// <summary>
/// Contexto para compartilhar estado entre step definitions.
/// </summary>
public class TestContext
{
    public HttpResponseMessage? Response { get; set; }
    public RespostaCriarSolicitacaoResponse? SolicitacaoResponse { get; set; }
    public List<RespostaCriarSolicitacaoResponse> SolicitacaoResponses { get; } = new();
    public List<string> EventoIds { get; } = new();
    public HttpStatusCode ExpectedStatusCode { get; set; }
}
