using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;

namespace ES2_SistemaPedidos.Shared.Logging;

public sealed class DateTimeConsoleFormatter : ITextFormatter
{
    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);

    public void Format(LogEvent logEvent, TextWriter output)
    {
        var brasiliaTimestamp = logEvent.Timestamp.ToUniversalTime().ToOffset(BrasiliaOffset);

        output.Write('[');
        output.Write(brasiliaTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture));
        output.Write(' ');
        output.Write(AbbreviateLevel(logEvent.Level));
        output.Write("] ");

        logEvent.RenderMessage(output, CultureInfo.InvariantCulture);
        output.WriteLine();

        if (logEvent.Exception is not null) output.WriteLine(logEvent.Exception);
    }

    private static string AbbreviateLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => level.ToString().ToUpperInvariant()
        };
    }
}
