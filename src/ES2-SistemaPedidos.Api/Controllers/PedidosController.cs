using Amazon.Runtime;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Route("api/solicitacoes")]
public sealed class PedidosController(PedidoService pedidoService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CriarSolicitacaoAsync(RequisicaoCriarSolicitacao requisicao,
        CancellationToken cancellationToken)
    {
        Resultado<RespostaCriarSolicitacao> result;
        try
        {
            result = await pedidoService.CriarSolicitacaoAsync(requisicao, cancellationToken);
        }
        catch (Exception exception) when (IsDependencyFailure(exception))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel",
                    "API de persistencia ou mensageria temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }

        return result.Match<IActionResult>(Accepted, BadRequest);
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> ListarEventosAsync(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await pedidoService.ListarEventosAsync(cancellationToken));
        }
        catch (Exception exception) when (IsDependencyFailure(exception))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "API de persistencia temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }
    }

    [HttpGet("{id:long}/historico")]
    public async Task<IActionResult> ObterHistoricoAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await pedidoService.ObterHistoricoAsync(id, cancellationToken);
            return result.Tipo switch
            {
                TipoResultadoConsulta.Sucesso => Ok(result.Valor),
                TipoResultadoConsulta.RequisicaoInvalida => BadRequest(result.Erro),
                TipoResultadoConsulta.NaoEncontrado => NotFound(result.Erro),
                _ => throw new InvalidOperationException("Tipo de resultado de consulta desconhecido.")
            };
        }
        catch (Exception exception) when (IsDependencyFailure(exception))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "API de persistencia temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }
    }

    private static bool IsDependencyFailure(Exception exception)
    {
        return exception is AmazonServiceException or HttpRequestException
               || (exception is InvalidOperationException invalidOperationException
                   && invalidOperationException.Message.Contains("SQS", StringComparison.OrdinalIgnoreCase));
    }
}
