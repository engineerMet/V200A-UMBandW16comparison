namespace WindSensorApp.Core.Models;

public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public double Speed { get; set; }
    public double SpeedPrimary { get; set; } // Для Boeder
    public double SpeedCorrected { get; set; } // Для Boeder
    public double Direction { get; set; }
    public double? Temperature { get; set; }
    public double? Pressure { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;

    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss}] {SensorName}: Speed={Speed:F2} m/s, Direction={Direction:F1}°";
}

public class StatisticsData
{
    public double MaxSpeed { get; set; }
    public double MinSpeed { get; set; }
    public double AvgSpeed { get; set; }
    public int Count { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Period { get; set; } = string.Empty; // "10min", "3hour", "24hour"
}

public class CalibrationData
{
    public DateTime Timestamp { get; set; }
    public int DirectionSector { get; set; } // 0-35
    public int SpeedRange { get; set; } // 0-35
    public double CoefficientA { get; set; }
    public double CoefficientB { get; set; }
    public double R2 { get; set; } // Коефіцієнт детермінації
    public double RMSE { get; set; } // Root Mean Square Error
    public int DataPointCount { get; set; }
    public bool IsValid { get; set; } = true;

    public override string ToString() =>
        $"Sector[{DirectionSector}] Range[{SpeedRange}]: a={CoefficientA:F4}, b={CoefficientB:F4}, R²={R2:F4}";
}

public class AggregatedData
{
    public DateTime Timestamp { get; set; }
    public string SensorName { get; set; } = string.Empty;
    public StatisticsData Stats10Min { get; set; } = new();
    public StatisticsData Stats3Hour { get; set; } = new();
    public StatisticsData Stats24Hour { get; set; } = new();
}

public class TransmissionMessage
{
    public DateTime Timestamp { get; set; }

    // Lufft
    public double? LufftSpeed { get; set; }
    public double? LufftDirection { get; set; }
    public double? LufftTemperature { get; set; }
    public double? LufftPressure { get; set; }

    // Boeder
    public double? BoederSpeedPrimary { get; set; }
    public double? BoederSpeedCorrected { get; set; }
    public double? BoederDirection { get; set; }

    // Calibration
    public double? CalibrationCoefficient_a { get; set; }
    public double? CalibrationCoefficient_b { get; set; }
    public double? CalibrationR2 { get; set; }
    public double? CalibrationRMSE { get; set; }

    // Statistics
    public StatisticsData? Stats10Min { get; set; }
    public StatisticsData? Stats3Hour { get; set; }
    public StatisticsData? Stats24Hour { get; set; }

    // Status
    public string StatusLufft { get; set; } = "Unknown";
    public string StatusBoeder { get; set; } = "Unknown";
    public double? SignalQuality { get; set; }
}
