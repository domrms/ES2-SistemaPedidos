using Microsoft.AspNetCore.Mvc;

namespace ES2_SistemaPedidos.Api.Controllers;

[ApiController]
[Route("api/saude")]
public sealed class SaudeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            estado = "saudavel",
            dataHora = DateTimeOffset.UtcNow,
            versao = "1.0.0"
        });
    }
}