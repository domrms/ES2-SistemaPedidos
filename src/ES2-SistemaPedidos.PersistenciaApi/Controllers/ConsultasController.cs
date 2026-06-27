using ES2_SistemaPedidos.PersistenciaApi.Application;
using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.PersistenciaApi.Controllers;

[ApiController]
[Route("api/consultas")]
public sealed class ConsultasController(ConsultaService servico) : ControllerBase
{
    [HttpGet("clientes/{id:int}/existe")]
    public async Task<ActionResult<RespostaExistencia>> ExisteClienteAsync(int id,
        CancellationToken tokenCancelamento)
    {
        return Ok(new RespostaExistencia(await servico.ExisteClienteAsync(id, tokenCancelamento)));
    }

    [HttpGet("produtos/{id:int}/existe")]
    public async Task<ActionResult<RespostaExistencia>> ExisteProdutoAsync(int id,
        CancellationToken tokenCancelamento)
    {
        return Ok(new RespostaExistencia(await servico.ExisteProdutoAsync(id, tokenCancelamento)));
    }

    [HttpGet("eventos")]
    public async Task<ActionResult<RespostaListarEventos>> ListarEventosAsync(
        CancellationToken tokenCancelamento)
    {
        return Ok(new RespostaListarEventos(await servico.ListarEventosAsync(tokenCancelamento)));
    }

    [HttpGet("pedidos/{id:long}/historico")]
    public async Task<ActionResult<HistoricoPedidoDetalhado>> ObterHistoricoAsync(long id,
        CancellationToken tokenCancelamento)
    {
        var historico = await servico.ObterHistoricoAsync(id, tokenCancelamento);
        return historico is null ? NotFound() : Ok(historico);
    }
}