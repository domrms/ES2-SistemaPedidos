using System.Reflection;
using Amazon.Lambda.SQSEvents;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Abstractions;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Models;
using ES2_SistemaPedidos.LambdaConsumerSQS.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.LambdaConsumerSQS.UnitTests;

public sealed class FunctionTests
{
    [Fact]
    public async Task FunctionHandler_deve_retornar_apenas_mensagens_invalidas_ou_com_falha()
    {
        var function = CriarFunction(new ClienteFake());
        var evento = new SQSEvent
        {
            Records =
            [
                Mensagem("ok", """{"clienteId":1,"produtoId":2,"eventoId":"evt-ok","dataHoraRequisicao":"2026-06-27T12:00:00Z"}"""),
                Mensagem("invalida", "null"),
                Mensagem("erro", """{"clienteId":1,"produtoId":2,"eventoId":"evt-erro","dataHoraRequisicao":"2026-06-27T12:00:00Z"}""")
            ]
        };

        var resposta = await function.FunctionHandler(evento, null!);

        Assert.Equal(["invalida", "erro"], resposta.BatchItemFailures.Select(x => x.ItemIdentifier));
    }

    [Fact]
    public async Task Processador_quando_registro_do_erro_tambem_falha_preserva_falha_original()
    {
        var cliente = new ClienteFake { FalharErro = true };
        var servico = new ProcessadorPedidoService(cliente, TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProcessadorPedidoService>.Instance);
        var corpo =
            """{"clienteId":1,"produtoId":2,"eventoId":"evt-erro","dataHoraRequisicao":"2026-06-27T12:00:00Z"}""";

        var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servico.ProcessMessageAsync("mensagem", corpo, default));

        Assert.Equal("Falha de persistencia", excecao.InnerException!.Message);
    }

    private static Function CriarFunction(IPedidoProcessamentoClient client)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddSingleton<IPedidoProcessamentoClient>(client);
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();
        services.AddScoped<ProcessadorPedidoService>();
        var provider = services.BuildServiceProvider();

        return (Function)Activator.CreateInstance(typeof(Function),
            BindingFlags.Instance | BindingFlags.NonPublic, null, [provider], null)!;
    }

    private static SQSEvent.SQSMessage Mensagem(string id, string corpo) => new()
    {
        MessageId = id,
        Body = corpo
    };

    private sealed class ClienteFake : IPedidoProcessamentoClient
    {
        public bool FalharErro { get; init; }

        public Task RegistrarEventoAsync(EventoProcessamento evento, CancellationToken cancellationToken) =>
            evento.EventoId == "evt-erro"
                ? Task.FromException(new InvalidOperationException("Falha de persistencia"))
                : Task.CompletedTask;

        public Task RegistrarErroAsync(EventoProcessamento evento, string detalhe,
            CancellationToken cancellationToken) => FalharErro
            ? Task.FromException(new InvalidOperationException("Falha ao registrar erro"))
            : Task.CompletedTask;
    }
}
