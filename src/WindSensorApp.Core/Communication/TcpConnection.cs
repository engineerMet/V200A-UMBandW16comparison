using System.Net.Sockets;
using WindSensorApp.Core.Logging;

namespace WindSensorApp.Core.Communication;

public class TcpConnection : IConnection, IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public string ConnectionName => $"TCP({_host}:{_port})";
    public bool IsConnected => _client?.Connected ?? false;
    public DateTime LastSuccessfulRead { get; private set; } = DateTime.MinValue;

    public TcpConnection(string host, int port, int timeoutMs = 5000)
    {
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            _client.ReceiveTimeout = _timeoutMs;
            _client.SendTimeout = _timeoutMs;

            var connectTask = _client.ConnectAsync(_host, _port);
            if (await Task.WhenAny(connectTask, Task.Delay(_timeoutMs)) == connectTask)
            {
                _stream = _client.GetStream();
                Logger.Info($"TCP connection established: {ConnectionName}");
            }
            else
            {
                _client?.Dispose();
                throw new TimeoutException(ConnectionName);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"TCP connection failed: {ex.Message}");
            throw new ConnectionFailedException(ConnectionName, ex.Message);
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _stream?.Dispose();
            _client?.Close();
            _client?.Dispose();
            Logger.Info($"TCP connection closed: {ConnectionName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error closing TCP connection: {ex.Message}");
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
            await _stream!.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();

            // Прочитати відповідь
            var buffer = new byte[1024];
            var cts = new CancellationTokenSource(_timeoutMs);
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

            if (bytesRead == 0)
            {
                throw new ConnectionException(ConnectionName, "Connection closed by remote host");
            }

            LastSuccessfulRead = DateTime.Now;
            var result = new byte[bytesRead];
            Array.Copy(buffer, result, bytesRead);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(ConnectionName);
        }
        catch (Exception ex)
        {
            Logger.Error($"TCP communication error: {ex.Message}");
            throw new ConnectionException(ConnectionName, ex.Message);
        }
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
