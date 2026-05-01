namespace ES2_SistemaPedidos.Shared.Domain;

public sealed class MensagemProcessada
{
    private MensagemProcessada()
    {
    }

    public MensagemProcessada(string mensagemId, Guid? pedidoId, string tipoMensagem, string status, DateTimeOffset processadaEm, string? detalhesErro = null)
    {
        MensagemId = mensagemId;
        PedidoId = pedidoId;
        TipoMensagem = tipoMensagem;
        Status = status;
        ProcessadaEm = processadaEm;
        DetalhesErro = detalhesErro;
    }

    public string MensagemId { get; private set; } = string.Empty;

    public Guid? PedidoId { get; private set; }

    public DateTimeOffset ProcessadaEm { get; private set; }

    public string TipoMensagem { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public string? DetalhesErro { get; private set; }

    public Pedido? Pedido { get; private set; }
}
