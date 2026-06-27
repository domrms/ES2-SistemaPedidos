using ES2_SistemaPedidos.Shared.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public sealed class PostgresHealthCheck(IServiceScopeFactory fabricaEscopos) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var escopo = fabricaEscopos.CreateAsyncScope();
            var contexto = escopo.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await contexto.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Conexao com PostgreSQL disponivel.")
                : HealthCheckResult.Unhealthy("Nao foi possivel conectar ao PostgreSQL.");
        }
        catch (Exception excecao)
        {
            return HealthCheckResult.Unhealthy("Falha ao verificar o PostgreSQL.", excecao);
        }
    }
}
