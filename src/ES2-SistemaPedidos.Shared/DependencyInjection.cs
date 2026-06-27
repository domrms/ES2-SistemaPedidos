using System.Diagnostics.CodeAnalysis;
using ES2_SistemaPedidos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Shared;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddBancoPedidos(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BancoPedidos");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "String de conexao nao configurada. Defina ConnectionStrings:BancoPedidos ou DATABASE_URL.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
