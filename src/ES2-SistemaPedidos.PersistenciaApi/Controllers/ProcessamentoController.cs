using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.PersistenciaApi.Controllers;

[ApiController]
[Route("api/processamentos/pedidos")]
public sealed class ProcessamentoController(IPedidoProcessamentoRepositorio repository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegistrarEventoAsync(RequisicaoProcessamentoPedido pedido,
        CancellationToken cancellationToken)
    {
        await repository.RegistrarEventoAsync(pedido, cancellationToken);
        return NoContent();
    }

    [HttpPost("erro")]
    public async Task<IActionResult> RegistrarErroAsync(RequisicaoErroProcessamentoPedido requisicao,
        CancellationToken cancellationToken)
    {
        await repository.RegistrarErroAsync(requisicao.Pedido, requisicao.Detalhe, cancellationToken);
        return NoContent();
    }
}
