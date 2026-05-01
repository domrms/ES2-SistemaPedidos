using ES2_SistemaPedidos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Shared;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrdersDatabase")
            ?? configuration["DATABASE_URL"]
            ?? "Host=localhost;Port=5432;Database=es2_orders;Username=dev;Password=dev";

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
