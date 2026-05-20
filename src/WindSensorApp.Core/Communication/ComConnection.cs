using System.IO.Ports;
using WindSensorApp.Core.Logging;

namespace WindSensorApp.Core.Communication;

public class ComConnection : IConnection, IDisposable
{
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly int _dataBits;
    private readonly int _stopBits;
    private readonly Parity _parity;
    private readonly Handshake _handshake;
    private readonly int _timeoutMs;
    private SerialPort? _port;

    public string ConnectionName => $"COM({_portName},{_baudRate})";
    public bool IsConnected => _port?.IsOpen ?? false;
    public DateTime LastSuccessfulRead { get; private set; } = DateTime.MinValue;

    public ComConnection(
        string portName, int baudRate = 19200, int dataBits = 8, 
        int stopBits = 1, Parity parity = Parity.None, 
        Handshake handshake = Handshake.None, int timeoutMs = 5000)
    {
        _portName = portName;
        _baudRate = baudRate;
        _dataBits = dataBits;
        _stopBits = stopBits;
        _parity = parity;
        _handshake = handshake;
        _timeoutMs = timeoutMs;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _port = new SerialPort(
                _portName, _baudRate, _parity, _dataBits, 
                (StopBits)_stopBits)
            {
                Handshake = _handshake,
                ReadTimeout = _timeoutMs,
                WriteTimeout = _timeoutMs
            };

            _port.Open();
            Logger.Info($"COM connection established: {ConnectionName}");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"COM connection failed: {ex.Message}");
            throw new ConnectionFailedException(ConnectionName, ex.Message);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _port?.Close();
            _port?.Dispose();
            Logger.Info($"COM connection closed: {ConnectionName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error closing COM connection: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public async Task<byte[]> SendReceiveAsync(byte[] data)
    {
        if (!IsConnected)
        {
            throw new ConnectionException(ConnectionName, "Not connected");
        }

        try
        {
            // Надіслати
            _port!.Write(data, 0, data.Length);
            await Task.Delay(50); // Затримка для обробки на приладі

            // Прочитати відповідь
            var buffer = new byte[1024];
            int bytesRead = 0;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < _timeoutMs && bytesRead == 0)
            {
                if (_port.BytesToRead > 0)
                {
                    bytesRead = _port.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    await Task.Delay(10);
                }
            }

            if (bytesRead == 0)
            {
                throw new TimeoutException(ConnectionName);
            }

            LastSuccessfulRead = DateTime.Now;
            var result = new byte[bytesRead];
            Array.Copy(buffer, result, bytesRead);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"COM communication error: {ex.Message}");
            throw new ConnectionException(ConnectionName, ex.Message);
        }
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
