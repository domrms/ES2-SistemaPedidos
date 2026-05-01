using System.Data.Common;
using Amazon.Runtime;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Route("api/solicitacoes")]
public sealed class PedidosController(ServicoPedido servicoPedido) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CriarSolicitacaoAsync(RequisicaoCriarSolicitacao requisicao, CancellationToken tokenCancelamento)
    {
        Resultado<RespostaCriarSolicitacao> resultado;
        try
        {
            resultado = await servicoPedido.CriarSolicitacaoAsync(requisicao, tokenCancelamento);
        }
        catch (Exception excecao) when (IsFalhaDependencia(excecao))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "Banco de dados ou mensageria temporariamente indisponivel", new { tentarNovamenteApos = 30 }));
        }

        return resultado.Match<IActionResult>(
            sucesso => Accepted(sucesso),
            BadRequest);
    }

    private static bool IsFalhaDependencia(Exception excecao)
    {
        return excecao is DbUpdateException
            or DbException
            or AmazonServiceException
            or HttpRequestException
            || excecao is InvalidOperationException invalidOperationException
            && invalidOperationException.Message.Contains("SQS", StringComparison.OrdinalIgnoreCase);
    }
}
