namespace WindSensorApp.Core.Protocols;

public interface IProtocol
{
    byte[] BuildCommand(int dataType);
    Dictionary<string, object> ParseResponse(byte[] data);
    string ProtocolName { get; }
}

public class UmbProtocol : IProtocol
{
    public string ProtocolName => "UMB";

    // UMB Protocol CRC-16
    public static ushort CalculateCRC16(byte[] data, int length)
    {
        ushort crc = 0xFFFF;

        for (int i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) == 1)
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

    public byte[] BuildCommand(int dataType)
    {
        // M20S - Lufft wind data request
        var command = new byte[] { 0x05, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x00 };
        var crc = CalculateCRC16(command, 6);
        command[6] = (byte)(crc & 0xFF);
        command[7] = (byte)((crc >> 8) & 0xFF);
        return command;
    }

    public Dictionary<string, object> ParseResponse(byte[] data)
    {
        var result = new Dictionary<string, object>();

        if (data.Length < 20)
        {
            result["Error"] = "Invalid response length";
            return result;
        }

        try
        {
            // Перевірка CRC
            var crc = CalculateCRC16(data, data.Length - 2);
            var receivedCrc = (ushort)(data[data.Length - 2] | (data[data.Length - 1] << 8));

            if (crc != receivedCrc)
            {
                result["Error"] = "CRC mismatch";
                return result;
            }

            // Парсинг даних
            // Байти 3-4: Швидкість вітру (м/с * 100)
            var speedRaw = (data[3] << 8) | data[4];
            var speed = speedRaw / 100.0;

            // Байти 5-6: Напрямок вітру (градуси * 10)
            var directionRaw = (data[5] << 8) | data[6];
            var direction = directionRaw / 10.0;

            // Байти 7-8: Температура (°C * 100)
            var tempRaw = (data[7] << 8) | data[8];
            var temperature = tempRaw / 100.0;

            // Байти 9-10: Тиск (hPa * 10)
            var pressureRaw = (data[9] << 8) | data[10];
            var pressure = pressureRaw / 10.0;

            result["Speed"] = speed;
            result["Direction"] = direction;
            result["Temperature"] = temperature;
            result["Pressure"] = pressure;
            result["IsValid"] = true;
        }
        catch (Exception ex)
        {
            result["Error"] = ex.Message;
            result["IsValid"] = false;
        }

        return result;
    }
}

public class ModbusRTUProtocol : IProtocol
{
    public string ProtocolName => "ModbusRTU";

    // Modbus CRC-16
    public static ushort CalculateCRC16(byte[] data, int length)
    {
        ushort crc = 0xFFFF;

        for (int i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) == 1)
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

    public byte[] BuildCommand(int functionCode)
    {
        // Читання холдингових регістрів (функція 3)
        // Формат: SlaveID | Function | StartAddress | Quantity | CRC
        var command = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00 };
        var crc = CalculateCRC16(command, 6);
        command[6] = (byte)(crc & 0xFF);
        command[7] = (byte)((crc >> 8) & 0xFF);
        return command;
    }

    public Dictionary<string, object> ParseResponse(byte[] data)
    {
        var result = new Dictionary<string, object>();

        if (data.Length < 11)
        {
            result["Error"] = "Invalid response length";
            return result;
        }

        try
        {
            // Перевірка CRC
            var crc = CalculateCRC16(data, data.Length - 2);
            var receivedCrc = (ushort)(data[data.Length - 2] | (data[data.Length - 1] << 8));

            if (crc != receivedCrc)
            {
                result["Error"] = "CRC mismatch";
                return result;
            }

            // Парсинг даних
            // Байти 3-4: Швидкість вітру первинна (м/с * 100)
            var speedPrimaryRaw = (data[3] << 8) | data[4];
            var speedPrimary = speedPrimaryRaw / 100.0;

            // Байти 5-6: Швидкість вітру коригована (м/с * 100)
            var speedCorrectedRaw = (data[5] << 8) | data[6];
            var speedCorrected = speedCorrectedRaw / 100.0;

            // Байти 7-8: Напрямок вітру (градуси * 10)
            var directionRaw = (data[7] << 8) | data[8];
            var direction = directionRaw / 10.0;

            result["SpeedPrimary"] = speedPrimary;
            result["SpeedCorrected"] = speedCorrected;
            result["Direction"] = direction;
            result["IsValid"] = true;
        }
        catch (Exception ex)
        {
            result["Error"] = ex.Message;
            result["IsValid"] = false;
        }

        return result;
    }
}
