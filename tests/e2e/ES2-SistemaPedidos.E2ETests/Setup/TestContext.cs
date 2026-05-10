namespace ES2_SistemaPedidos.E2ETests.Setup;

public class TestContext
{
    public HttpResponseMessage? Response { get; set; }
    public RespostaCriarSolicitacaoResponse? SolicitacaoResponse { get; set; }
    public List<string> EventoIds { get; } = new();
}
