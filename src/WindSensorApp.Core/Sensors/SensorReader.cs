using WindSensorApp.Core.Communication;
using WindSensorApp.Core.Models;
using WindSensorApp.Core.Protocols;
using WindSensorApp.Core.Logging;

namespace WindSensorApp.Core.Sensors;

public interface ISensorReader
{
    Task<SensorReading?> ReadAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
    bool IsConnected { get; }
    string SensorName { get; }
    DateTime LastSuccessfulRead { get; }
    int SecondsSinceLastRead { get; }
}

public class SensorReader : ISensorReader
{
    private readonly IConnection _connection;
    private readonly ISensorProtocol _protocol;
    private readonly SensorConnectionSettings _settings;
    private int _retryCount = 0;

    public string SensorName { get; }
    public bool IsConnected => _connection.IsConnected;
    public DateTime LastSuccessfulRead => _connection.LastSuccessfulRead;
    public int SecondsSinceLastRead => LastSuccessfulRead == DateTime.MinValue 
        ? int.MaxValue 
        : (int)(DateTime.Now - LastSuccessfulRead).TotalSeconds;

    public SensorReader(
        string sensorName,
        IConnection connection,
        ISensorProtocol protocol,
        SensorConnectionSettings settings)
    {
        SensorName = sensorName;
        _connection = connection;
        _protocol = protocol;
        _settings = settings;
    }

    public async Task ConnectAsync()
    {
        try
        {
            await _connection.ConnectAsync();
            _retryCount = 0;
            Logger.Info($"{SensorName} connected successfully");
        }
        catch (ConnectionException ex)
        {
            Logger.Error($"{SensorName} connection failed: {ex.Message}");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await _connection.DisconnectAsync();
            Logger.Info($"{SensorName} disconnected");
        }
        catch (Exception ex)
        {
            Logger.Error($"{SensorName} disconnection error: {ex.Message}");
        }
    }

    public async Task<SensorReading?> ReadAsync()
    {
        if (!IsConnected)
        {
            try
            {
                await ConnectAsync();
            }
            catch (ConnectionException ex)
            {
                Logger.Warning($"{SensorName} auto-reconnect failed: {ex.Message}");
                return null;
            }
        }

        try
        {
            var command = _protocol.CreateReadCommand();
            var response = await _connection.SendReceiveAsync(command);
            var reading = _protocol.ParseResponse(response);

            if (reading != null)
            {
                _retryCount = 0;
                return reading;
            }
            else
            {
                Logger.Warning($"{SensorName} failed to parse response");
                return null;
            }
        }
        catch (TimeoutException ex)
        {
            _retryCount++;
            Logger.Warning($"{SensorName} timeout (attempt {_retryCount}/{_settings.RetryAttempts})");
            
            if (_retryCount >= _settings.RetryAttempts)
            {
                await DisconnectAsync();
                _retryCount = 0;
            }
            return null;
        }
        catch (ConnectionException ex)
        {
            _retryCount++;
            Logger.Error($"{SensorName} communication error: {ex.Message}");
            
            if (_retryCount >= _settings.RetryAttempts)
            {
                await DisconnectAsync();
                _retryCount = 0;
            }
            return null;
        }
    }
}

public class SensorManager
{
    private readonly Configuration _config;
    private ISensorReader? _lufftReader;
    private ISensorReader? _boederReader;
    private CancellationTokenSource? _cancellationToken;
    private Task? _readingTask;
    private bool _isPaused = false;

    public event Action<SensorReading>? LufftDataReceived;
    public event Action<SensorReading>? BoederDataReceived;
    public event Action<string>? ErrorOccurred;

    public bool IsRunning { get; private set; }
    public bool IsPaused => _isPaused;

    public SensorManager(Configuration config)
    {
        _config = config;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Initialize Lufft
            if (_config.Lufft.Enabled)
            {
                var lufftConnection = ConnectionFactory.CreateConnection(
                    _config.Lufft,
                    _config.Lufft.TCP,
                    _config.Lufft.COM
                );

                var lufftProtocol = new UmbProtocol();
                _lufftReader = new SensorReader(
                    "Lufft V200A-UMB",
                    lufftConnection,
                    lufftProtocol,
                    _config.Lufft
                );

                await _lufftReader.ConnectAsync();
            }

            // Initialize Boeder
            if (_config.Boeder.Enabled)
            {
                var boederConnection = ConnectionFactory.CreateConnection(
                    _config.Boeder,
                    _config.Boeder.TCP,
                    _config.Boeder.COM
                );

                var boederProtocol = new ModbusRtuProtocol();
                _boederReader = new SensorReader(
                    "Boeder W16",
                    boederConnection,
                    boederProtocol,
                    _config.Boeder
                );

                await _boederReader.ConnectAsync();
            }

            Logger.Info("Sensors initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error($"Sensor initialization failed: {ex.Message}");
            throw;
        }
    }

    public async Task StartAsync()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        _isPaused = false;
        _cancellationToken = new CancellationTokenSource();

        try
        {
            await InitializeAsync();
            _readingTask = ReadSensorsAsync(_cancellationToken.Token);
            Logger.Info("Sensor reading started");
        }
        catch (Exception ex)
        {
            IsRunning = false;
            Logger.Error($"Failed to start sensor reading: {ex.Message}");
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        _cancellationToken?.Cancel();

        try
        {
            if (_readingTask != null)
                await _readingTask;

            await _lufftReader?.DisconnectAsync()!;
            await _boederReader?.DisconnectAsync()!;

            Logger.Info("Sensor reading stopped");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping sensor reading: {ex.Message}");
        }
    }

    public void Pause()
    {
        _isPaused = true;
        Logger.Info("Sensor reading paused");
    }

    public void Resume()
    {
        _isPaused = false;
        Logger.Info("Sensor reading resumed");
    }

    private async Task ReadSensorsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_isPaused)
                {
                    // Read Lufft
                    if (_lufftReader != null)
                    {
                        var lufftReading = await _lufftReader.ReadAsync();
                        if (lufftReading != null)
                        {
                            LufftDataReceived?.Invoke(lufftReading);
                        }
                    }

                    // Read Boeder
                    if (_boederReader != null)
                    {
                        var boederReading = await _boederReader.ReadAsync();
                        if (boederReading != null)
                        {
                            BoederDataReceived?.Invoke(boederReading);
                        }
                    }
                }

                // Respawn interval
                await Task.Delay(_config.Lufft.Interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in sensor reading loop: {ex.Message}");
                ErrorOccurred?.Invoke(ex.Message);
                await Task.Delay(1000); // Wait before retry
            }
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _cancellationToken?.Dispose();
    }
}
