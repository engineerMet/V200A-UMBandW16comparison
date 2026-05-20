using WindSensorApp.Core.Models;

namespace WindSensorApp.Core.Protocols;

public interface ISensorProtocol
{
    byte[] CreateReadCommand();
    SensorReading? ParseResponse(byte[] data);
    string ProtocolName { get; }
    int ExpectedResponseLength { get; }
}

/// <summary>
/// UMB Protocol for Lufft V200A-UMB
/// </summary>
public class UmbProtocol : ISensorProtocol
{
    public string ProtocolName => "UMB";
    public int ExpectedResponseLength => 55; // UMB response length

    private const byte STX = 0x02;
    private const byte ETX = 0x03;
    private const byte DEVICE_ID = 0x01;
    private const byte COMMAND_READ = 0x01;

    public byte[] CreateReadCommand()
    {
        // UMB protocol command structure
        var command = new byte[6];
        command[0] = STX;              // Start
        command[1] = DEVICE_ID;        // Device ID
        command[2] = COMMAND_READ;     // Read command
        command[3] = 0x00;             // Data length (no parameters)
        command[4] = CalculateCRC(command, 4);
        command[5] = ETX;              // End

        return command;
    }

    public SensorReading? ParseResponse(byte[] data)
    {
        if (data.Length < ExpectedResponseLength)
            return null;

        try
        {
            // Check frame format
            if (data[0] != STX || data[data.Length - 1] != ETX)
                return null;

            // Extract values from UMB response
            // Format: [STX][ID][CMD][LEN][DATA...][CRC][ETX]
            // DATA contains: Wind Speed (4 bytes), Wind Direction (2 bytes), etc.

            int offset = 4; // Skip header

            // Wind Speed (0.01 m/s resolution)
            float windSpeed = BitConverter.ToSingle(data, offset);
            offset += 4;

            // Wind Direction (0.1° resolution)
            ushort windDirection = BitConverter.ToUInt16(data, offset);
            offset += 2;

            // Temperature (optional)
            float temperature = BitConverter.ToSingle(data, offset);
            offset += 4;

            // Pressure (optional)
            float pressure = BitConverter.ToSingle(data, offset);

            return new SensorReading
            {
                Timestamp = DateTime.Now,
                Speed = windSpeed,
                Direction = windDirection * 0.1,
                Temperature = temperature,
                Pressure = pressure,
                SensorName = "Lufft V200A-UMB",
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            Logging.Logger.Error($"UMB parsing error: {ex.Message}");
            return null;
        }
    }

    private byte CalculateCRC(byte[] data, int length)
    {
        byte crc = 0;
        for (int i = 0; i < length; i++)
        {
            crc ^= data[i];
        }
        return crc;
    }
}

/// <summary>
/// Modbus RTU Protocol for Boeder W16
/// </summary>
public class ModbusRtuProtocol : ISensorProtocol
{
    public string ProtocolName => "ModbusRTU";
    public int ExpectedResponseLength => 25; // Typical Modbus RTU response

    private const byte DEVICE_ADDRESS = 0x01;
    private const byte FUNCTION_READ_HOLDING = 0x03;
    private const ushort START_ADDRESS = 0x0000;
    private const ushort REGISTER_COUNT = 5; // Read 5 registers

    public byte[] CreateReadCommand()
    {
        var command = new byte[8];
        command[0] = DEVICE_ADDRESS;
        command[1] = FUNCTION_READ_HOLDING;
        command[2] = (byte)((START_ADDRESS >> 8) & 0xFF);
        command[3] = (byte)(START_ADDRESS & 0xFF);
        command[4] = (byte)((REGISTER_COUNT >> 8) & 0xFF);
        command[5] = (byte)(REGISTER_COUNT & 0xFF);

        ushort crc = CalculateCRC16(command, 6);
        command[6] = (byte)(crc & 0xFF);
        command[7] = (byte)((crc >> 8) & 0xFF);

        return command;
    }

    public SensorReading? ParseResponse(byte[] data)
    {
        if (data.Length < 11)
            return null;

        try
        {
            // Modbus RTU response format: [Address][Function][ByteCount][Data...][CRC_Low][CRC_High]
            if (data[0] != DEVICE_ADDRESS || data[1] != FUNCTION_READ_HOLDING)
                return null;

            byte byteCount = data[2];
            if (data.Length != byteCount + 5)
                return null;

            // Verify CRC
            ushort responseCrc = (ushort)((data[data.Length - 1] << 8) | data[data.Length - 2]);
            ushort calculatedCrc = CalculateCRC16(data, data.Length - 2);
            if (responseCrc != calculatedCrc)
                return null;

            // Extract values from register data
            int offset = 3;

            // Combine two 16-bit registers into 32-bit float for Wind Speed
            ushort reg1 = (ushort)((data[offset] << 8) | data[offset + 1]);
            ushort reg2 = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
            float windSpeedPrimary = BitConverter.ToSingle(BitConverter.GetBytes((uint)((reg1 << 16) | reg2)), 0);
            offset += 4;

            // Wind Speed Corrected
            ushort reg3 = (ushort)((data[offset] << 8) | data[offset + 1]);
            ushort reg4 = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
            float windSpeedCorrected = BitConverter.ToSingle(BitConverter.GetBytes((uint)((reg3 << 16) | reg4)), 0);
            offset += 4;

            // Wind Direction
            ushort windDirection = (ushort)((data[offset] << 8) | data[offset + 1]);

            return new SensorReading
            {
                Timestamp = DateTime.Now,
                SpeedPrimary = windSpeedPrimary,
                SpeedCorrected = windSpeedCorrected,
                Speed = windSpeedCorrected, // Use corrected as main
                Direction = (windDirection & 0x03FF) * 0.1, // 10-bit resolution, 0.1° per unit
                SensorName = "Boeder W16",
                IsValid = true
            };
        }
        catch (Exception ex)
        {
            Logging.Logger.Error($"Modbus RTU parsing error: {ex.Message}");
            return null;
        }
    }

    private ushort CalculateCRC16(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (int i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) == 1)
                {
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                }
                else
                {
                    crc >>= 1;
                }
            }
        }
        return crc;
    }
}
