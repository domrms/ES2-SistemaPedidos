using ES2_SistemaPedidos.PersistenciaApi.Application;
using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.PersistenciaApi.Infrastructure;
using ES2_SistemaPedidos.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddBancoPedidos(builder.Configuration);
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<IEventoRepositorio, EventoRepositorio>();
builder.Services.AddScoped<IPedidoStatusRepositorio, PedidoStatusRepositorio>();
builder.Services.AddScoped<IPedidoProcessamentoRepositorio, PedidoProcessamentoRepositorio>();
builder.Services.AddScoped<ConsultaService>();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapHealthChecks("/api/healthcheck", new HealthCheckOptions());
await app.RunAsync();
