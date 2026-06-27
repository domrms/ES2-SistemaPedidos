using ES2_SistemaPedidos.PersistenciaApi.Application;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.PersistenciaApi.Controllers;

[ApiController]
[Route("api/consultas")]
public sealed class ConsultasController(ConsultaService service) : ControllerBase
{
    [HttpGet("clientes/{id:int}/existe")]
    public async Task<ActionResult<RespostaExistencia>> ExisteClienteAsync(int id,
        CancellationToken cancellationToken)
    {
        return Ok(new RespostaExistencia(await service.ExisteClienteAsync(id, cancellationToken)));
    }

    [HttpGet("produtos/{id:int}/existe")]
    public async Task<ActionResult<RespostaExistencia>> ExisteProdutoAsync(int id,
        CancellationToken cancellationToken)
    {
        return Ok(new RespostaExistencia(await service.ExisteProdutoAsync(id, cancellationToken)));
    }

    [HttpGet("eventos")]
    public async Task<ActionResult<RespostaListarEventos>> ListarEventosAsync(
        CancellationToken cancellationToken)
    {
        return Ok(new RespostaListarEventos(await service.ListarEventosAsync(cancellationToken)));
    }

    [HttpGet("pedidos/{id:long}/historico")]
    public async Task<ActionResult<HistoricoPedidoDetalhado>> ObterHistoricoAsync(long id,
        CancellationToken cancellationToken)
    {
        var historico = await service.ObterHistoricoAsync(id, cancellationToken);
        return historico is null ? NotFound() : Ok(historico);
    }
}
