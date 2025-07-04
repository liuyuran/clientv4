using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ModLoader.logger;

public sealed class RedirectLogger : ILogger {
    public static Action<LogLevel, string> WriteLine = (level, s) => {
        Console.WriteLine($"[{level}] {s}");
    };
    private readonly TimeProvider _timeProvider = new DefaultTimeProvider();
    private readonly string _categoryName;

    public RedirectLogger(string categoryName) {
        if (categoryName.Length == 0) {
            throw new ArgumentException("Category name can't be empty.", nameof(categoryName));
        }

        _categoryName = categoryName;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        switch (logLevel) {
            case LogLevel.None:
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
            case LogLevel.Warning:
            case LogLevel.Error:
            case LogLevel.Critical:
                var handler = new DefaultInterpolatedStringHandler(17, 3);
                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(logLevel.ToString());
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));
                WriteLine(logLevel, handler.ToString());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}