using System;
using System.Runtime.CompilerServices;
using Godot;
using Microsoft.Extensions.Logging;

namespace game.scripts.logger;

internal sealed class GodotLogger : ILogger
{

    private readonly TimeProvider _timeProvider;
    private readonly string _categoryName;

    public GodotLogger(TimeProvider timeProvider, string categoryName)
    {
        if (categoryName.Length == 0)
        {
            throw new ArgumentException("Category name can't be empty.", nameof(categoryName));
        }

        _timeProvider = timeProvider;
        _categoryName = categoryName;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        DefaultInterpolatedStringHandler handler;

        switch (logLevel)
        {
            case LogLevel.None:
                break;
            case LogLevel.Trace:
                handler = new DefaultInterpolatedStringHandler(14, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Trace));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.Print(handler.ToString());
                break;
            case LogLevel.Debug:
                handler = new DefaultInterpolatedStringHandler(14, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Debug));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.Print(handler.ToString());
                break;
            case LogLevel.Information:
                handler = new DefaultInterpolatedStringHandler(20, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Information));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.Print(handler.ToString());
                break;
            case LogLevel.Warning:
                handler = new DefaultInterpolatedStringHandler(16, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Warning));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.PushWarning(handler.ToString());
                break;
            case LogLevel.Error:
                handler = new DefaultInterpolatedStringHandler(14, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Error));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.PushError(handler.ToString());
                break;
            case LogLevel.Critical:
                handler = new DefaultInterpolatedStringHandler(17, 3);

                handler.AppendLiteral("[");
                handler.AppendFormatted(_timeProvider.GetLocalNow());
                handler.AppendLiteral("] [");
                handler.AppendFormatted(_categoryName);
                handler.AppendLiteral("] [");
                handler.AppendLiteral(nameof(LogLevel.Critical));
                handler.AppendLiteral("] ");
                handler.AppendFormatted(formatter(state, exception));

                GD.PushError(handler.ToString());
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

}
