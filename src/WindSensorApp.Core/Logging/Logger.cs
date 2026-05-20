using Serilog;

namespace WindSensorApp.Core.Logging;

public static class Logger
{
    private static ILogger? _logger;

    public static void Initialize(string logPath = "./logs")
    {
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(logPath, "app-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}")
            .CreateLogger();
    }

    public static void Debug(string message) => _logger?.Debug(message);
    public static void Info(string message) => _logger?.Information(message);
    public static void Warning(string message) => _logger?.Warning(message);
    public static void Error(string message) => _logger?.Error(message);
    public static void Error(Exception ex, string message) => _logger?.Error(ex, message);

    public static void Dispose()
    {
        if (_logger is IDisposable disposable)
            disposable.Dispose();
    }
}
