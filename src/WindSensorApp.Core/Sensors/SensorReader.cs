using WindSensorApp.Core.Communication;
using WindSensorApp.Core.Logging;
using WindSensorApp.Core.Models;
using WindSensorApp.Core.Protocols;

namespace WindSensorApp.Core.Sensors;

public interface ISensorReader
{
    Task<SensorReading> ReadAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected { get; }
    string SensorName { get; }
}

public class LufftReader : ISensorReader
{
    private readonly IConnection _connection;
    private readonly IProtocol _protocol;
    private readonly int _retryAttempts;
    private readonly int _retryDelayMs;

    public string SensorName => "Lufft V200A-UMB";
    public bool IsConnected => _connection.IsConnected;

    public LufftReader(IConnection connection, int retryAttempts = 3, int retryDelayMs = 1000)
    {
        _connection = connection;
        _protocol = new UmbProtocol();
        _retryAttempts = retryAttempts;
        _retryDelayMs = retryDelayMs;
    }

    public async Task ConnectAsync()
    {
        await _connection.ConnectAsync();
        Logger.Info($"{SensorName} connected");
    }

    public async Task DisconnectAsync()
    {
        await _connection.DisconnectAsync();
        Logger.Info($"{SensorName} disconnected");
    }

    public async Task<SensorReading> ReadAsync()
    {
        var reading = new SensorReading
        {
            Timestamp = DateTime.Now,
            SensorName = SensorName
        };

        for (int attempt = 0; attempt < _retryAttempts; attempt++)
        {
            try
            {
                var command = _protocol.BuildCommand(0);
                var response = await _connection.SendReceiveAsync(command);
                var data = _protocol.ParseResponse(response);

                if (data.ContainsKey("Error"))
                {
                    reading.IsValid = false;
                    reading.ErrorMessage = data["Error"].ToString() ?? "Unknown error";
                    Logger.Warning($"{SensorName} parse error: {reading.ErrorMessage}");
                }
                else
                {
                    reading.Speed = (double)data["Speed"];
                    reading.Direction = (double)data["Direction"];
                    reading.Temperature = (double?)data["Temperature"];
                    reading.Pressure = (double?)data["Pressure"];
                    reading.IsValid = true;
                    Logger.Debug($"{SensorName} read: Speed={reading.Speed:F2} m/s, Direction={reading.Direction:F1}°");
                    return reading;
                }
            }
            catch (TimeoutException ex)
            {
                Logger.Warning($"{SensorName} timeout (attempt {attempt + 1}/{_retryAttempts})");
                if (attempt < _retryAttempts - 1)
                {
                    await Task.Delay(_retryDelayMs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{SensorName} error: {ex.Message}");
                reading.IsValid = false;
                reading.ErrorMessage = ex.Message;
            }
        }

        return reading;
    }
}

public class BoederReader : ISensorReader
{
    private readonly IConnection _connection;
    private readonly IProtocol _protocol;
    private readonly int _retryAttempts;
    private readonly int _retryDelayMs;

    public string SensorName => "Boeder W16";
    public bool IsConnected => _connection.IsConnected;

    public BoederReader(IConnection connection, int retryAttempts = 3, int retryDelayMs = 1000)
    {
        _connection = connection;
        _protocol = new ModbusRTUProtocol();
        _retryAttempts = retryAttempts;
        _retryDelayMs = retryDelayMs;
    }

    public async Task ConnectAsync()
    {
        await _connection.ConnectAsync();
        Logger.Info($"{SensorName} connected");
    }

    public async Task DisconnectAsync()
    {
        await _connection.DisconnectAsync();
        Logger.Info($"{SensorName} disconnected");
    }

    public async Task<SensorReading> ReadAsync()
    {
        var reading = new SensorReading
        {
            Timestamp = DateTime.Now,
            SensorName = SensorName
        };

        for (int attempt = 0; attempt < _retryAttempts; attempt++)
        {
            try
            {
                var command = _protocol.BuildCommand(3);
                var response = await _connection.SendReceiveAsync(command);
                var data = _protocol.ParseResponse(response);

                if (data.ContainsKey("Error"))
                {
                    reading.IsValid = false;
                    reading.ErrorMessage = data["Error"].ToString() ?? "Unknown error";
                    Logger.Warning($"{SensorName} parse error: {reading.ErrorMessage}");
                }
                else
                {
                    reading.SpeedPrimary = (double)data["SpeedPrimary"];
                    reading.SpeedCorrected = (double)data["SpeedCorrected"];
                    reading.Speed = reading.SpeedCorrected; // Основна - коригована
                    reading.Direction = (double)data["Direction"];
                    reading.IsValid = true;
                    Logger.Debug($"{SensorName} read: Speed={reading.SpeedCorrected:F2} m/s, Direction={reading.Direction:F1}°");
                    return reading;
                }
            }
            catch (TimeoutException ex)
            {
                Logger.Warning($"{SensorName} timeout (attempt {attempt + 1}/{_retryAttempts})");
                if (attempt < _retryAttempts - 1)
                {
                    await Task.Delay(_retryDelayMs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{SensorName} error: {ex.Message}");
                reading.IsValid = false;
                reading.ErrorMessage = ex.Message;
            }
        }

        return reading;
    }
}
