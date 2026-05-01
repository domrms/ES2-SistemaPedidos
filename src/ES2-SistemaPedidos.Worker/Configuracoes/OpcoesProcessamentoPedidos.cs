namespace ES2_SistemaPedidos.Worker.Configuracoes;

public sealed class OpcoesProcessamentoPedidos
{
    public string FilaUrl { get; init; } = "http://localhost:4566/000000000000/processamento-solicitacoes";

    public int QuantidadeMaximaMensagens { get; init; } = 10;

    public int TempoEsperaSegundos { get; init; } = 10;

    public int TempoVisibilidadeSegundos { get; init; } = 60;
}
