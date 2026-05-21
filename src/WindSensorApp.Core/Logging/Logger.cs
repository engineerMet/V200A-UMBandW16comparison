using Serilog;

namespace WindSensorApp.Core.Logging;

public static class Logger
{
    private static ILogger? _logger;

    public static void Initialize(string logFilePath = "./logs/app.log")
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath) ?? "./logs");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Info("Logger initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize logger: {ex.Message}");
        }
    }

    public static void Debug(string message) => _logger?.Debug(message);
    public static void Info(string message) => _logger?.Information(message);
    public static void Warning(string message) => _logger?.Warning(message);
    public static void Error(string message) => _logger?.Error(message);
    public static void Error(Exception ex, string message) => _logger?.Error(ex, message);
}
