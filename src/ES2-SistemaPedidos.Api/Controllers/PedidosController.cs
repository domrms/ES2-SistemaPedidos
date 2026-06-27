using System.Data.Common;
using Amazon.Runtime;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Route("api/solicitacoes")]
public sealed class PedidosController(PedidoService pedidoService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CriarSolicitacaoAsync(RequisicaoCriarSolicitacao requisicao,
        CancellationToken tokenCancelamento)
    {
        Resultado<RespostaCriarSolicitacao> resultado;
        try
        {
            resultado = await pedidoService.CriarSolicitacaoAsync(requisicao, tokenCancelamento);
        }
        catch (Exception excecao) when (IsFalhaDependencia(excecao))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "Banco de dados ou mensageria temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }

        return resultado.Match<IActionResult>(
            Accepted,
            BadRequest);
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> ListarEventosAsync(CancellationToken tokenCancelamento)
    {
        try
        {
            var resposta = await pedidoService.ListarEventosAsync(tokenCancelamento);
            return Ok(resposta);
        }
        catch (Exception excecao) when (IsFalhaDependencia(excecao))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "Banco de dados temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }
    }

    [HttpGet("{id:long}/historico")]
    public async Task<IActionResult> ObterHistoricoAsync(long id, CancellationToken tokenCancelamento)
    {
        try
        {
            var resultado = await pedidoService.ObterHistoricoAsync(id, tokenCancelamento);
            return resultado.Tipo switch
            {
                TipoResultadoConsulta.Sucesso => Ok(resultado.Valor),
                TipoResultadoConsulta.RequisicaoInvalida => BadRequest(resultado.Erro),
                TipoResultadoConsulta.NaoEncontrado => NotFound(resultado.Erro),
                _ => throw new InvalidOperationException("Tipo de resultado de consulta desconhecido.")
            };
        }
        catch (Exception excecao) when (IsFalhaDependencia(excecao))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "Banco de dados temporariamente indisponivel",
                    new { tentarNovamenteApos = 30 }));
        }
    }

    private static bool IsFalhaDependencia(Exception excecao)
    {
        return excecao is DbUpdateException
                   or DbException
                   or AmazonServiceException
                   or HttpRequestException
               || (excecao is InvalidOperationException invalidOperationException
                   && invalidOperationException.Message.Contains("SQS", StringComparison.OrdinalIgnoreCase));
    }
}
