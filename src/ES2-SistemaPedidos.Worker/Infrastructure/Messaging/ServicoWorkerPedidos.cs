using Amazon.SQS;
using Amazon.SQS.Model;
using ES2_SistemaPedidos.Worker.Application.Services;
using ES2_SistemaPedidos.Worker.Configuracoes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.Worker.Infrastructure.Messaging;

public sealed class ServicoWorkerPedidos(
    IAmazonSQS sqs,
    IServiceScopeFactory fabricaEscopo,
    ProcessamentoPedidosOptions opcoes,
    ILogger<ServicoWorkerPedidos> registrador)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        registrador.LogInformation("Worker de pedidos iniciado. Fila: {FilaUrl}", opcoes.FilaUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var resposta = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = opcoes.FilaUrl,
                    MaxNumberOfMessages = opcoes.QuantidadeMaximaMensagens,
                    WaitTimeSeconds = opcoes.TempoEsperaSegundos,
                    VisibilityTimeout = opcoes.TempoVisibilidadeSegundos
                }, stoppingToken);

                foreach (var mensagem in resposta.Messages)
                {
                    await ProcessMessageAsync(mensagem, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception excecao)
            {
                registrador.LogError(excecao, "Falha no ciclo de leitura da fila SQS");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message mensagem, CancellationToken tokenCancelamento)
    {
        using var escopo = fabricaEscopo.CreateScope();
        var processador = escopo.ServiceProvider.GetRequiredService<ProcessadorPedidoService>();

        try
        {
            var processada = await processador.ProcessMessageAsync(mensagem.MessageId, mensagem.Body, tokenCancelamento);
            if (!processada)
            {
                registrador.LogWarning("Mensagem {MensagemId} nao foi processada e permanecera na fila", mensagem.MessageId);
                return;
            }

            await sqs.DeleteMessageAsync(opcoes.FilaUrl, mensagem.ReceiptHandle, tokenCancelamento);
            registrador.LogInformation("Mensagem {MensagemId} processada e removida da fila", mensagem.MessageId);
        }
        catch (Exception excecao)
        {
            registrador.LogError(excecao, "Erro ao processar mensagem {MensagemId}; a mensagem nao sera removida", mensagem.MessageId);
        }
    }
}
