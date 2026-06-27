using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public sealed class PersistenciaApiHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(nameof(PersistenciaApiHealthCheck));
            var response = await client.GetAsync("api/healthcheck", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API de persistencia disponivel.")
                : HealthCheckResult.Unhealthy($"API de persistencia respondeu HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Falha ao acessar a API de persistencia.", exception);
        }
    }
}
