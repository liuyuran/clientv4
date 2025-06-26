using Microsoft.Extensions.Logging;

namespace ModLoader.logger;

public class LogManager {
    private static ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => {
        builder.SetMinimumLevel(LogLevel.Debug).AddConsole();
    });

    public static void SetLoggerBuilder(ILoggerFactory factory) {
        _loggerFactory = factory ?? throw new ArgumentNullException(nameof(factory), "Logger factory cannot be null.");
    }
    
    public static ILogger GetLogger<T>() {
        return _loggerFactory.CreateLogger<T>();
    }
}