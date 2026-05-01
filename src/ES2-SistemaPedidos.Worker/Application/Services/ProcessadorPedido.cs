using System.Text.Json;
using ES2_SistemaPedidos.Shared.Contracts;
using ES2_SistemaPedidos.Worker.Application.Abstractions;
using ES2_SistemaPedidos.Worker.Application.Models;
using Microsoft.Extensions.Logging;

namespace ES2_SistemaPedidos.Worker.Application.Services;

public sealed class ProcessadorPedido(
    IPedidoProcessamentoRepositorio repositorio,
    TimeProvider provedorTempo,
    ILogger<ProcessadorPedido> registrador)
{
    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    public async Task<bool> ProcessMessageAsync(string mensagemSqsId, string corpoMensagem, CancellationToken tokenCancelamento)
    {
        var evento = JsonSerializer.Deserialize<EventoSolicitacaoCliente>(corpoMensagem, OpcoesJson);
        if (evento is null || evento.ClienteId <= 0 || evento.RequisicaoId == Guid.Empty)
        {
            registrador.LogWarning("Mensagem {MensagemId} possui payload invalido", mensagemSqsId);
            return false;
        }

        await repositorio.RegistrarEventoAsync(new EventoProcessamento(
            evento.ClienteId,
            evento.RequisicaoId,
            evento.DataHoraRequisicao,
            provedorTempo.GetUtcNow()), tokenCancelamento);

        registrador.LogInformation(
            "Evento {EventoId} do cliente {ClienteId} salvo no banco",
            evento.RequisicaoId,
            evento.ClienteId);

        return true;
    }
}
