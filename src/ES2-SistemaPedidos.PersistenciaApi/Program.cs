using System.Diagnostics.CodeAnalysis;
using ES2_SistemaPedidos.PersistenciaApi.Application;
using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.PersistenciaApi.Infrastructure;
using ES2_SistemaPedidos.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var construtor = WebApplication.CreateBuilder(args);

construtor.Services.AddControllers();
construtor.Services.AddEndpointsApiExplorer();
construtor.Services.AddSwaggerGen();
construtor.Services.AddMemoryCache();
construtor.Services.AddBancoPedidos(construtor.Configuration);
construtor.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
construtor.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
construtor.Services.AddScoped<IEventoRepositorio, EventoRepositorio>();
construtor.Services.AddScoped<IPedidoStatusRepositorio, PedidoStatusRepositorio>();
construtor.Services.AddScoped<IPedidoProcessamentoRepositorio, PedidoProcessamentoRepositorio>();
construtor.Services.AddScoped<ConsultaService>();
construtor.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

var aplicacao = construtor.Build();
aplicacao.UseSwagger();
aplicacao.UseSwaggerUI();
aplicacao.MapControllers();
aplicacao.MapHealthChecks("/api/healthcheck", new HealthCheckOptions());
aplicacao.Run();

[ExcludeFromCodeCoverage]
public partial class Program;
