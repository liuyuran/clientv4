using Microsoft.Extensions.Logging;

namespace ModLoader.logger;

public class LogManager {
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => {
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddProvider(new RedirectLoggerProvider());
    });
    
    public static ILogger GetLogger<T>() {
        return _loggerFactory.CreateLogger<T>();
    }
}