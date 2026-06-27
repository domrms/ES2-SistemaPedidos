using System.Diagnostics.CodeAnalysis;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Data.Repositorios;
using ES2_SistemaPedidos.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ES2_SistemaPedidos.Shared;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddPersistenciaPedidos(this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        var stringConexao = configuracao.GetConnectionString("BancoPedidos")
                            ?? configuracao["DATABASE_URL"]
                            ?? "Host=localhost;Port=5432;Database=es2_pedidos;Username=dev;Password=dev";

        servicos.AddDbContext<ApplicationDbContext>(opcoes => opcoes.UseNpgsql(stringConexao));
        servicos.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        servicos.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
        servicos.AddScoped<IEventoRepositorio, EventoRepositorio>();
        servicos.AddScoped<IPedidoStatusRepositorio, PedidoStatusRepositorio>();

        return servicos;
    }
}
