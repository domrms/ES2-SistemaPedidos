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
        var connectionString = configuration.GetConnectionString("BancoPedidos")
                               ?? configuration["DATABASE_URL"]
                            ?? "Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev";

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
