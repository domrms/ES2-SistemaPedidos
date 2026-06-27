using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.Api.Infrastructure.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public static Task EscreverAsync(HttpContext contexto, HealthReport relatorio)
    {
        contexto.Response.ContentType = "application/json; charset=utf-8";

        var resposta = new
        {
            estado = relatorio.Status.ToString().ToLowerInvariant(),
            duracao = relatorio.TotalDuration,
            verificacoes = relatorio.Entries.ToDictionary(
                entrada => entrada.Key,
                entrada => new
                {
                    estado = entrada.Value.Status.ToString().ToLowerInvariant(),
                    descricao = entrada.Value.Description,
                    duracao = entrada.Value.Duration
                })
        };

        return contexto.Response.WriteAsync(JsonSerializer.Serialize(resposta, OpcoesJson));
    }
}
