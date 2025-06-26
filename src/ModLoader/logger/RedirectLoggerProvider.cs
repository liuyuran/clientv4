using Microsoft.Extensions.Logging;

namespace ModLoader.logger;

public sealed class RedirectLoggerProvider : ILoggerProvider {
    public ILogger CreateLogger(string categoryName) => new RedirectLogger(categoryName);

    public void Dispose() { }
}