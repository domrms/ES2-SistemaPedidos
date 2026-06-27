using System.Text.Json;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;
using ES2_SistemaPedidos.Shared.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.UnitTests;

public sealed class ProcessadorPedidoServiceTests
{
    private static readonly DateTimeOffset AgoraUtc = new(2026, 5, 3, 15, 30, 45, TimeSpan.Zero);

    [Fact]
    public async Task ProcessMessageAsync_quando_payload_valido_registra_evento_e_retorna_true()
    {
        var repository = new FakePedidoProcessamentoClient();
        var servico = CriarServico(repository);
        var evento = new EventoSolicitacaoCliente(
            3,
            4,
            "ES2-12345678-123000",
            new DateTimeOffset(2026, 5, 3, 12, 30, 0, TimeSpan.FromHours(-3)));
        var corpo = JsonSerializer.Serialize(evento, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var processada = await servico.ProcessMessageAsync("mensagem-1", corpo, CancellationToken.None);

        Assert.True(processada);
        var eventoRegistrado = Assert.Single(repository.Eventos);
        Assert.Equal(evento.ClienteId, eventoRegistrado.ClienteId);
        Assert.Equal(evento.ProdutoId, eventoRegistrado.ProdutoId);
        Assert.Equal(evento.EventoId, eventoRegistrado.EventoId);
        Assert.Equal(evento.DataHoraRequisicao, eventoRegistrado.DataHoraEvento);
        Assert.Equal(AgoraUtc, eventoRegistrado.SalvoEm);
    }

    [Theory]
    [InlineData(
        """{"clienteId":0,"produtoId":1,"eventoId":"ES2-12345678-123000","dataHoraRequisicao":"2026-05-03T12:30:00-03:00"}""")]
    [InlineData(
        """{"clienteId":1,"produtoId":0,"eventoId":"ES2-12345678-123000","dataHoraRequisicao":"2026-05-03T12:30:00-03:00"}""")]
    [InlineData("""{"clienteId":1,"produtoId":2,"eventoId":"","dataHoraRequisicao":"2026-05-03T12:30:00-03:00"}""")]
    [InlineData("""{"clienteId":1,"produtoId":2,"eventoId":"   ","dataHoraRequisicao":"2026-05-03T12:30:00-03:00"}""")]
    public async Task ProcessMessageAsync_quando_payload_invalido_retorna_false_sem_registrar_evento(string corpo)
    {
        var repository = new FakePedidoProcessamentoClient();
        var servico = CriarServico(repository);

        var processada = await servico.ProcessMessageAsync("mensagem-2", corpo, CancellationToken.None);

        Assert.False(processada);
        Assert.Empty(repository.Eventos);
    }

    [Fact]
    public async Task ProcessMessageAsync_quando_payload_null_retorna_false_sem_registrar_evento()
    {
        var repository = new FakePedidoProcessamentoClient();
        var servico = CriarServico(repository);

        var processada = await servico.ProcessMessageAsync("mensagem-3", "null", CancellationToken.None);

        Assert.False(processada);
        Assert.Empty(repository.Eventos);
    }

    [Fact]
    public async Task ProcessMessageAsync_quando_processamento_falha_registra_erro_e_preserva_excecao()
    {
        var repository = new FakePedidoProcessamentoClient { FalharAoRegistrar = true };
        var servico = CriarServico(repository);
        var evento = new EventoSolicitacaoCliente(3, 4, "ES2-12345678-123000", AgoraUtc);
        var corpo = JsonSerializer.Serialize(evento, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servico.ProcessMessageAsync("mensagem-4", corpo, CancellationToken.None));

        Assert.Equal("Falha simulada", excecao.Message);
        Assert.Single(repository.Erros);
        Assert.Equal("Falha durante o processamento da solicitacao.", repository.Erros[0].Detalhe);
    }

    private static ProcessadorPedidoService CriarServico(FakePedidoProcessamentoClient repository)
    {
        return new ProcessadorPedidoService(
            repository,
            new FakeTimeProvider(AgoraUtc),
            NullLogger<ProcessadorPedidoService>.Instance);
    }

    private sealed class FakePedidoProcessamentoClient : IPedidoProcessamentoClient
    {
        public bool FalharAoRegistrar { get; init; }

        public List<EventoProcessamento> Eventos { get; } = [];

        public List<(EventoProcessamento Evento, string Detalhe)> Erros { get; } = [];

        public Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken tokenCancelamento)
        {
            if (FalharAoRegistrar) throw new InvalidOperationException("Falha simulada");
            Eventos.Add(evento);
            return Task.CompletedTask;
        }

        public Task RegistrarErroAsync(EventoProcessamento evento, string detalhe,
            CancellationToken tokenCancelamento)
        {
            Erros.Add((evento, detalhe));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset agoraUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return agoraUtc;
        }
    }
}