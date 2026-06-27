using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public sealed class FlociHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["AWS_ENDPOINT_URL"]
                       ?? configuration["AWS:ServiceUrl"]
                       ?? configuration["AWS:EndpointUrl"];

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return HealthCheckResult.Unhealthy("Endpoint do Floci possui formato invalido.");

        try
        {
            using var response = await httpClientFactory.CreateClient("FlociHealthCheck")
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Qualquer resposta HTTP comprova conectividade com o emulador; a raiz pode responder 404.
            return HealthCheckResult.Healthy($"Floci acessivel (HTTP {(int)response.StatusCode}).");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Floci indisponivel.", exception);
        }
    }
}
