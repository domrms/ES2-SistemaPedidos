using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;

namespace ES2_SistemaPedidos.Shared.Logging;

public sealed class HorarioBrasiliaConsoleFormatter : ITextFormatter
{
    private static readonly TimeSpan DeslocamentoBrasilia = TimeSpan.FromHours(-3);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var timestampBrasilia = logEvent.Timestamp.ToUniversalTime().ToOffset(DeslocamentoBrasilia);

        output.Write('[');
        output.Write(timestampBrasilia.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        output.Write(' ');
        output.Write(AbreviarNivel(logEvent.Level));
        output.Write("] ");

        logEvent.RenderMessage(output, CultureInfo.InvariantCulture);
        output.WriteLine();

        if (logEvent.Exception is not null)
        {
            output.WriteLine(logEvent.Exception);
        }
    }

    private static string AbreviarNivel(LogEventLevel nivel)
    {
        return nivel switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => nivel.ToString().ToUpperInvariant()
        };
    }
}
