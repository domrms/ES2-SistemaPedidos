using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            estado = report.Status.ToString().ToLowerInvariant(),
            duracao = report.TotalDuration,
            verificacoes = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    estado = entry.Value.Status.ToString().ToLowerInvariant(),
                    descricao = entry.Value.Description,
                    duracao = entry.Value.Duration
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
