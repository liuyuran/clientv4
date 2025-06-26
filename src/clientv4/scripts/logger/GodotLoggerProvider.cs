using System;
using Microsoft.Extensions.Logging;
using ModLoader.logger;

namespace game.scripts.logger;

public sealed class GodotLoggerProvider : ILoggerProvider
{

    private readonly TimeProvider _timeProvider = new DefaultTimeProvider();

    public ILogger CreateLogger(string categoryName) => new GodotLogger(_timeProvider, categoryName);

    public void Dispose()
    {
    }

}
