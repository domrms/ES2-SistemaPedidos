using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public sealed class FlociHealthCheck(IHttpClientFactory fabricaHttpClient, IConfiguration configuracao) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var endpoint = configuracao["AWS_ENDPOINT_URL"]
                       ?? configuracao["AWS:ServiceUrl"]
                       ?? configuracao["AWS:EndpointUrl"]
                       ?? "http://localhost:4566";

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return HealthCheckResult.Unhealthy("Endpoint do Floci possui formato invalido.");

        try
        {
            using var resposta = await fabricaHttpClient.CreateClient("FlociHealthCheck")
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Qualquer resposta HTTP comprova conectividade com o emulador; a raiz pode responder 404.
            return HealthCheckResult.Healthy($"Floci acessivel (HTTP {(int)resposta.StatusCode}).");
        }
        catch (Exception excecao) when (excecao is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Floci indisponivel.", excecao);
        }
    }
}