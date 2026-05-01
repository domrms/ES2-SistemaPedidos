using Amazon.Runtime;
using ES2_SistemaPedidos.Api.Application.Pedidos;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pedidos")]
public sealed class PedidosController(ServicoPedido servicoPedido) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreatePedidoAsync(RequisicaoCriarPedido requisicao, CancellationToken tokenCancelamento)
    {
        Resultado<RespostaCriarPedido> resultado;
        try
        {
            resultado = await servicoPedido.CreatePedidoAsync(requisicao, HttpContext.TraceIdentifier, tokenCancelamento);
        }
        catch (Exception excecao) when (IsFalhaDependencia(excecao))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new RespostaErro("ServicoIndisponivel", "Banco de dados ou mensageria temporariamente indisponivel", new { tentarNovamenteApos = 30 }));
        }

        return resultado.Match<IActionResult>(
            sucesso => Created($"/api/pedidos/{sucesso.PedidoId}", sucesso),
            BadRequest);
    }

    [HttpGet("{pedidoId:guid}")]
    public async Task<IActionResult> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken tokenCancelamento)
    {
        var pedido = await servicoPedido.GetPedidoPorIdAsync(pedidoId, tokenCancelamento);

        return pedido is null
            ? NotFound(new RespostaErro("PedidoNaoEncontrado", $"Pedido com ID {pedidoId} nao encontrado"))
            : Ok(pedido);
    }

    [HttpGet]
    public async Task<IActionResult> ListPedidosAsync(
        [FromQuery] string clienteId,
        [FromQuery] StatusPedido? status,
        [FromQuery] int? pular,
        [FromQuery] int? quantidade,
        [FromQuery] DateOnly? dataDe,
        [FromQuery] DateOnly? dataAte,
        CancellationToken tokenCancelamento)
    {
        var resultado = await servicoPedido.ListPedidosAsync(clienteId, status, pular ?? 0, quantidade ?? 20, dataDe, dataAte, tokenCancelamento);

        return resultado.Match<IActionResult>(
            Ok,
            BadRequest);
    }

    private static bool IsFalhaDependencia(Exception excecao)
    {
        return excecao is DbUpdateException
            or AmazonServiceException
            or HttpRequestException
            || excecao is InvalidOperationException invalidOperationException
            && invalidOperationException.Message.Contains("SQS", StringComparison.OrdinalIgnoreCase);
    }
}
