using ES2_SistemaPedidos.Shared.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.PersistenciaApi.Infrastructure;

public sealed class PostgresHealthCheck(ApplicationDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Conexao com PostgreSQL disponivel.")
                : HealthCheckResult.Unhealthy("Nao foi possivel conectar ao PostgreSQL.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Falha ao verificar o PostgreSQL.", exception);
        }
    }
}
