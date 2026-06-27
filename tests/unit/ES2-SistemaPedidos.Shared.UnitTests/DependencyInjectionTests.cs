using ES2_SistemaPedidos.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Shared.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddBancoPedidos_quando_conexao_nao_foi_configurada_deve_falhar()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddBancoPedidos(configuration);
        });

        Assert.Contains("ConnectionStrings:BancoPedidos ou DATABASE_URL", exception.Message);
    }

    [Theory]
    [InlineData("ConnectionStrings:BancoPedidos")]
    [InlineData("DATABASE_URL")]
    public void AddBancoPedidos_deve_usar_conexao_fornecida_externamente(string configurationKey)
    {
        const string connectionString = "Host=banco-interno;Port=5432;Database=pedidos";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [configurationKey] = connectionString
            })
            .Build();
        var services = new ServiceCollection();

        services.AddBancoPedidos(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(connectionString, context.Database.GetConnectionString());
    }
}
