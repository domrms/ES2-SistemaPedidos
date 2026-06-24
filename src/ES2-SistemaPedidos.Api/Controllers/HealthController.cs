using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Route("api/healthcheck")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            estado = "healthy",
            dataHora = DateTime.Now,
            versao = "1.0.0"
        });
    }
}