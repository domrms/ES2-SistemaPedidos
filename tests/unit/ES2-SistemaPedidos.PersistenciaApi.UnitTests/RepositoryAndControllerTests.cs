using ES2_SistemaPedidos.PersistenciaApi.Application;
using ES2_SistemaPedidos.PersistenciaApi.Controllers;
using ES2_SistemaPedidos.PersistenciaApi.Data;
using ES2_SistemaPedidos.PersistenciaApi.Infrastructure;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Shared.Data;
using ES2_SistemaPedidos.Shared.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ES2_SistemaPedidos.PersistenciaApi.UnitTests;

public sealed class RepositoryAndControllerTests
{
    private static readonly DateTimeOffset Agora = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Repositorios_de_cliente_e_produto_devem_consultar_existencia()
    {
        await using var db = await CriarDbAsync();
        db.Clientes.Add(new Cliente(1, "Ana"));
        db.Produtos.Add(new Produto(2, "Livro"));
        await db.SaveChangesAsync();

        var clientes = new ClienteRepositorio(db);
        var produtos = new ProdutoRepositorio(db);

        Assert.True(await clientes.ExisteAsync(1, default));
        Assert.False(await clientes.ExisteAsync(404, default));
        Assert.True(await produtos.ExisteAsync(2, default));
        Assert.False(await produtos.ExisteAsync(404, default));
    }

    [Fact]
    public async Task EventoRepositorio_deve_projetar_nomes_e_ordenar_por_data()
    {
        await using var db = await CriarDbAsync();
        await SemearReferenciasAsync(db);
        db.Eventos.AddRange(
            new EventoCliente(1, 1, 2, "segundo", Agora, Agora.AddMinutes(2)),
            new EventoCliente(2, 1, 2, "primeiro", Agora, Agora.AddMinutes(1)));
        await db.SaveChangesAsync();

        var eventos = await new EventoRepositorio(db).ListarTodosAsync(default);

        Assert.Equal(["primeiro", "segundo"], eventos.Select(x => x.EventoId));
        Assert.All(eventos, x =>
        {
            Assert.Equal("Ana", x.NomeCliente);
            Assert.Equal("Livro", x.NomeProduto);
        });
    }

    [Fact]
    public async Task PedidoStatusRepositorio_deve_retornar_historico_ordenado_ou_nulo()
    {
        await using var db = await CriarDbAsync();
        await SemearReferenciasAsync(db);
        db.Eventos.Add(new EventoCliente(1, 1, 2, "evt", Agora, Agora));
        db.PedidoStatus.AddRange(
            new PedidoStatus(2, 1, EstadoPedido.Concluido, Agora.AddSeconds(2)),
            new PedidoStatus(1, 1, EstadoPedido.Recebido, Agora, "recebido"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repositorio = new PedidoStatusRepositorio(db);

        var historico = await repositorio.ObterHistoricoAsync(1, default);

        Assert.Equal("evt", historico!.EventoId);
        Assert.Equal([1L, 2L], historico.Historico.Select(x => x.Id));
        Assert.Null(await repositorio.ObterHistoricoAsync(404, default));
    }

    [Fact]
    public async Task ProcessamentoRepositorio_deve_ser_idempotente_e_registrar_estados()
    {
        await using var db = await CriarDbAsync();
        await SemearReferenciasAsync(db);
        var repositorio = new PedidoProcessamentoRepositorio(db);
        var requisicao = new RequisicaoProcessamentoPedido(1, 2, "evt-concluido", Agora, Agora.AddSeconds(1));

        await repositorio.RegistrarEventoAsync(requisicao, default);
        await repositorio.RegistrarEventoAsync(requisicao, default);

        Assert.Equal(3, await db.PedidoStatus.CountAsync());
        Assert.Equal([EstadoPedido.Recebido, EstadoPedido.Processando, EstadoPedido.Concluido],
            await db.PedidoStatus.OrderBy(x => x.Id).Select(x => x.Status).ToListAsync());
    }

    [Fact]
    public async Task ProcessamentoRepositorio_deve_registrar_detalhe_do_erro()
    {
        await using var db = await CriarDbAsync();
        await SemearReferenciasAsync(db);
        var repositorio = new PedidoProcessamentoRepositorio(db);
        var requisicao = new RequisicaoProcessamentoPedido(1, 2, "evt-erro", Agora, Agora.AddSeconds(1));

        await repositorio.RegistrarErroAsync(requisicao, "falha controlada", default);

        var erro = await db.PedidoStatus.SingleAsync(x => x.Status == EstadoPedido.Erro);
        Assert.Equal("falha controlada", erro.Detalhe);
    }

    [Fact]
    public async Task ConsultasController_deve_mapear_as_quatro_operacoes()
    {
        var eventos = new EventoRepositorioFake();
        var status = new StatusRepositorioFake();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ConsultaService(new ClienteRepositorioFake(), new ProdutoRepositorioFake(), eventos,
            status, cache, new ConfigurationBuilder().Build());
        var controller = new ConsultasController(service);

        Assert.True(Assert.IsType<RespostaExistencia>(Assert.IsType<OkObjectResult>(
            (await controller.ExisteClienteAsync(1, default)).Result).Value).Existe);
        Assert.False(Assert.IsType<RespostaExistencia>(Assert.IsType<OkObjectResult>(
            (await controller.ExisteProdutoAsync(2, default)).Result).Value).Existe);
        Assert.Single(Assert.IsType<RespostaListarEventos>(Assert.IsType<OkObjectResult>(
            (await controller.ListarEventosAsync(default)).Result).Value).Eventos);
        Assert.IsType<NotFoundResult>((await controller.ObterHistoricoAsync(404, default)).Result);
        Assert.IsType<OkObjectResult>((await controller.ObterHistoricoAsync(1, default)).Result);
    }

    [Fact]
    public async Task PostgresHealthCheck_deve_reportar_banco_disponivel_e_contexto_descartado()
    {
        var db = await CriarDbAsync();
        var check = new PostgresHealthCheck(db);
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext())).Status);

        await db.DisposeAsync();
        var falha = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Unhealthy, falha.Status);
        Assert.NotNull(falha.Exception);
    }

    private static async Task<ApplicationDbContext> CriarDbAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new ApplicationDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task SemearReferenciasAsync(ApplicationDbContext db)
    {
        db.Clientes.Add(new Cliente(1, "Ana"));
        db.Produtos.Add(new Produto(2, "Livro"));
        await db.SaveChangesAsync();
    }

    private sealed class ClienteRepositorioFake : IClienteRepositorio
    {
        public Task<bool> ExisteAsync(int clienteId, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class ProdutoRepositorioFake : IProdutoRepositorio
    {
        public Task<bool> ExisteAsync(int produtoId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class EventoRepositorioFake : IEventoRepositorio
    {
        public Task<IReadOnlyCollection<EventoDetalhado>> ListarTodosAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<EventoDetalhado>>(
                [new EventoDetalhado(1, "Ana", "Livro", "evt", Agora, Agora)]);
    }

    private sealed class StatusRepositorioFake : IPedidoStatusRepositorio
    {
        public Task<HistoricoPedidoDetalhado?> ObterHistoricoAsync(long pedidoId,
            CancellationToken cancellationToken) => Task.FromResult<HistoricoPedidoDetalhado?>(pedidoId == 404
            ? null
            : new HistoricoPedidoDetalhado(pedidoId, "evt",
                [new TransicaoPedidoDetalhada(1, EstadoPedido.Recebido, Agora, null)]));
    }
}
