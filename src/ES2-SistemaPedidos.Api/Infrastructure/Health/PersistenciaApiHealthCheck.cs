using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public sealed class PersistenciaApiHealthCheck(IHttpClientFactory fabricaClientes) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cliente = fabricaClientes.CreateClient(nameof(PersistenciaApiHealthCheck));
            var resposta = await cliente.GetAsync("api/healthcheck", cancellationToken);
            return resposta.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API de persistencia disponivel.")
                : HealthCheckResult.Unhealthy($"API de persistencia respondeu HTTP {(int)resposta.StatusCode}.");
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Unhealthy("Falha ao acessar a API de persistencia.", excecao);
        }
    }
}
