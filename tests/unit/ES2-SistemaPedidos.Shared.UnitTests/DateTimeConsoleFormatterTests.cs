using ES2_SistemaPedidos.Shared.Logging;
using Serilog.Events;
using Serilog.Parsing;

namespace ES2_SistemaPedidos.Shared.UnitTests;

public sealed class DateTimeConsoleFormatterTests
{
    [Fact]
    public void Format_deve_escrever_timestamp_de_brasilia_nivel_e_mensagem()
    {
        var formatter = new DateTimeConsoleFormatter();
        using var output = new StringWriter();
        var logEvent = new LogEvent(
            new DateTimeOffset(2026, 5, 3, 15, 4, 5, 123, TimeSpan.Zero),
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse("Pedido {PedidoId} processado"),
            [new LogEventProperty("PedidoId", new ScalarValue(42))]);

        formatter.Format(logEvent, output);

        Assert.Equal("[2026-05-03 12:04:05.123 -03:00 INF] Pedido 42 processado" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Format_quando_existe_exception_deve_escrever_exception_em_linha_separada()
    {
        var formatter = new DateTimeConsoleFormatter();
        using var output = new StringWriter();
        var excecao = new InvalidOperationException("falha esperada");
        var logEvent = new LogEvent(
            new DateTimeOffset(2026, 5, 3, 15, 4, 5, TimeSpan.Zero),
            LogEventLevel.Error,
            excecao,
            new MessageTemplateParser().Parse("Falha ao processar pedido"),
            []);

        formatter.Format(logEvent, output);

        var texto = output.ToString();
        Assert.Contains("[2026-05-03 12:04:05.000 -03:00 ERR] Falha ao processar pedido" + Environment.NewLine, texto);
        Assert.Contains("System.InvalidOperationException: falha esperada", texto);
    }
}
