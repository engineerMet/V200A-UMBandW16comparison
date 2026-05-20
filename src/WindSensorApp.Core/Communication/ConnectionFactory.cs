using WindSensorApp.Core.Models;

namespace WindSensorApp.Core.Communication;

public static class ConnectionFactory
{
    public static IConnection CreateConnection(SensorConnectionSettings settings, TcpSettings? tcpSettings = null, ComSettings? comSettings = null)
    {
        if (settings.ConnectionType == "TCP")
        {
            if (tcpSettings == null)
                throw new ArgumentException("TCP settings required for TCP connection");

            return new TcpConnection(tcpSettings.Host, tcpSettings.Port, tcpSettings.Timeout);
        }
        else if (settings.ConnectionType == "COM")
        {
            if (comSettings == null)
                throw new ArgumentException("COM settings required for COM connection");

            var parity = Enum.Parse<System.IO.Ports.Parity>(comSettings.Parity);
            var handshake = Enum.Parse<System.IO.Ports.Handshake>(comSettings.Handshake);

            return new ComConnection(
                comSettings.PortName,
                comSettings.BaudRate,
                comSettings.DataBits,
                comSettings.StopBits,
                parity,
                handshake,
                comSettings.Timeout
            );
        }
        else
        {
            throw new ArgumentException($"Unknown connection type: {settings.ConnectionType}");
        }
    }
}
