namespace WindSensorApp.Core.Communication;

public interface IConnection
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task<byte[]> SendReceiveAsync(byte[] data);
    bool IsConnected { get; }
    string ConnectionName { get; }
    DateTime LastSuccessfulRead { get; }
}

public class ConnectionException : Exception
{
    public string ConnectionName { get; set; }
    public ConnectionException(string connectionName, string message) : base(message)
    {
        ConnectionName = connectionName;
    }
}

public class TimeoutException : ConnectionException
{
    public TimeoutException(string connectionName) 
        : base(connectionName, $"Connection timeout: {connectionName}") { }
}

public class ConnectionFailedException : ConnectionException
{
    public ConnectionFailedException(string connectionName, string reason) 
        : base(connectionName, $"Connection failed ({connectionName}): {reason}") { }
}
