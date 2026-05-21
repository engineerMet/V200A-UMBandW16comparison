using WindSensorApp.Core.Communication;
using WindSensorApp.Core.Logging;
using WindSensorApp.Core.Models;

namespace WindSensorApp.Core.Sensors;

public interface ISensorManager
{
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    Task PauseAsync();
    Task ResumeAsync();
    bool IsMonitoring { get; }
    bool IsPaused { get; }

    SensorReading? LastLufftReading { get; }
    SensorReading? LastBoederReading { get; }

    event Action<SensorReading>? LufftDataReceived;
    event Action<SensorReading>? BoederDataReceived;
    event Action<string>? ErrorOccurred;
}

public class SensorManager : ISensorManager, IDisposable
{
    private readonly ISensorReader _lufftReader;
    private readonly ISensorReader _boederReader;
    private readonly int _lufftIntervalMs;
    private readonly int _boederIntervalMs;
    private CancellationTokenSource? _cancellationToken;
    private Task? _lufftMonitoringTask;
    private Task? _boederMonitoringTask;

    public bool IsMonitoring { get; private set; }
    public bool IsPaused { get; private set; }
    public SensorReading? LastLufftReading { get; private set; }
    public SensorReading? LastBoederReading { get; private set; }

    public event Action<SensorReading>? LufftDataReceived;
    public event Action<SensorReading>? BoederDataReceived;
    public event Action<string>? ErrorOccurred;

    public SensorManager(
        ISensorReader lufftReader,
        ISensorReader boederReader,
        int lufftIntervalMs = 3000,
        int boederIntervalMs = 3000)
    {
        _lufftReader = lufftReader;
        _boederReader = boederReader;
        _lufftIntervalMs = lufftIntervalMs;
        _boederIntervalMs = boederIntervalMs;
    }

    public async Task StartMonitoringAsync()
    {
        if (IsMonitoring)
        {
            Logger.Warning("Monitoring already running");
            return;
        }

        try
        {
            _cancellationToken = new CancellationTokenSource();
            IsMonitoring = true;
            IsPaused = false;

            // Підключити обидва датчики
            await _lufftReader.ConnectAsync();
            await _boederReader.ConnectAsync();

            // Запустити моніторинг
            _lufftMonitoringTask = MonitorLufftAsync(_cancellationToken.Token);
            _boederMonitoringTask = MonitorBoederAsync(_cancellationToken.Token);

            Logger.Info("Sensor monitoring started");
        }
        catch (Exception ex)
        {
            IsMonitoring = false;
            Logger.Error($"Failed to start monitoring: {ex.Message}");
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    public async Task StopMonitoringAsync()
    {
        if (!IsMonitoring)
        {
            return;
        }

        try
        {
            IsMonitoring = false;
            IsPaused = false;
            _cancellationToken?.Cancel();

            if (_lufftMonitoringTask != null)
                await _lufftMonitoringTask;
            if (_boederMonitoringTask != null)
                await _boederMonitoringTask;

            await _lufftReader.DisconnectAsync();
            await _boederReader.DisconnectAsync();

            Logger.Info("Sensor monitoring stopped");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping monitoring: {ex.Message}");
        }
    }

    public async Task PauseAsync()
    {
        if (!IsMonitoring || IsPaused)
        {
            return;
        }

        IsPaused = true;
        Logger.Info("Sensor monitoring paused");
        await Task.CompletedTask;
    }

    public async Task ResumeAsync()
    {
        if (!IsMonitoring || !IsPaused)
        {
            return;
        }

        IsPaused = false;
        Logger.Info("Sensor monitoring resumed");
        await Task.CompletedTask;
    }

    private async Task MonitorLufftAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (IsPaused)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                if (_lufftReader.IsConnected)
                {
                    var reading = await _lufftReader.ReadAsync();
                    LastLufftReading = reading;
                    LufftDataReceived?.Invoke(reading);
                }

                await Task.Delay(_lufftIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Lufft monitoring: {ex.Message}");
                ErrorOccurred?.Invoke($"Lufft error: {ex.Message}");
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private async Task MonitorBoederAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (IsPaused)
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                if (_boederReader.IsConnected)
                {
                    var reading = await _boederReader.ReadAsync();
                    LastBoederReading = reading;
                    BoederDataReceived?.Invoke(reading);
                }

                await Task.Delay(_boederIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Boeder monitoring: {ex.Message}");
                ErrorOccurred?.Invoke($"Boeder error: {ex.Message}");
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        StopMonitoringAsync().Wait();
        _cancellationToken?.Dispose();
    }
}
