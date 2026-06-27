using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.PersistenciaApi.Controllers;

[ApiController]
[Route("api/processamentos/pedidos")]
public sealed class ProcessamentoController(IPedidoProcessamentoRepositorio repositorio) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido,
        CancellationToken tokenCancelamento)
    {
        await repositorio.RegistrarEventoAsync(pedido, tokenCancelamento);
        return NoContent();
    }

    [HttpPost("erro")]
    public async Task<IActionResult> RegistrarErroAsync(RequisicaoErroProcessamentoPedido requisicao,
        CancellationToken tokenCancelamento)
    {
        await repositorio.RegistrarErroAsync(requisicao.Pedido, requisicao.Detalhe, tokenCancelamento);
        return NoContent();
    }
}
