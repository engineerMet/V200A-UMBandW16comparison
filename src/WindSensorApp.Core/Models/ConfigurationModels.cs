namespace WindSensorApp.Core.Models;

public class SensorConnectionSettings
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "TCP"; // TCP або COM
    public int Interval { get; set; } = 3000; // мс
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class TcpSettings
{
    public string Host { get; set; } = "192.168.1.100";
    public int Port { get; set; } = 4001;
    public int Timeout { get; set; } = 5000; // мс
}

public class ComSettings
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public int StopBits { get; set; } = 1;
    public string Parity { get; set; } = "None";
    public string Handshake { get; set; } = "None";
    public int Timeout { get; set; } = 5000; // мс
}

public class LufftSettings : SensorConnectionSettings
{
    public string Protocol { get; set; } = "UMB";
    public TcpSettings TCP { get; set; } = new();
    public ComSettings COM { get; set; } = new();
}

public class BoederSettings : SensorConnectionSettings
{
    public string Protocol { get; set; } = "ModbusRTU";
    public TcpSettings TCP { get; set; } = new();
    public ComSettings COM { get; set; } = new();
}

public class CalibrationSettings
{
    public int WindDirectionSectors { get; set; } = 36;
    public int WindSpeedRanges { get; set; } = 36;
    public double DegreesPerSector { get; set; } = 10.0; // 360 / 36
    public double MetersPerSecondPerRange { get; set; } = 1.0; // 0-35+ m/s
    public int MinDataPointsPerCoefficient { get; set; } = 100;
    public bool EnableOutlierDetection { get; set; } = true;
    public double OutlierThresholdSigma { get; set; } = 3.0; // >3σ
}

public class StorageSettings
{
    public string ArchiveFolder { get; set; } = "./data/archives";
    public string ArchiveFormat { get; set; } = "CSV"; // CSV, SQLite
    public bool EnableSQLiteExport { get; set; } = true;
    public int SQLiteExportIntervalMs { get; set; } = 604800000; // 1 тиждень
    public string SQLiteExportPath { get; set; } = "./data/sensor_data.db";
}

public class IdleWarningSettings
{
    public bool Enabled { get; set; } = true;
    public int WarningIntervalMs { get; set; } = 60000; // 1 хвилина
    public string WarningType { get; set; } = "All"; // All, Sound, Visual, None
    public string SoundFile { get; set; } = "./sounds/warning.wav";
    public int SoundFrequency { get; set; } = 1000; // Hz
    public int SoundDuration { get; set; } = 200; // мс
    public int SoundGroupPause { get; set; } = 200; // мс
    public int SoundGroupCount { get; set; } = 2; // 2 групи = бип-бип-бип бип-бип-бип
    public int SoundVolume { get; set; } = 80; // 0-100
    public bool FlashWindow { get; set; } = true;
    public string FlashColor { get; set; } = "#FF0000";
    public int FlashDuration { get; set; } = 300; // мс
    public int FlashCount { get; set; } = 3;
    public bool TrayNotificationEnabled { get; set; } = true;
}

public class UISettings
{
    public bool AutoStartOnLaunch { get; set; } = true;
    public string StartupMode { get; set; } = "Monitoring"; // Monitoring, ArchiveOnly, Standby
    public int MinimumWindowWidth { get; set; } = 1280;
    public int MinimumWindowHeight { get; set; } = 800;
    public int DefaultWindowWidth { get; set; } = 1280;
    public int DefaultWindowHeight { get; set; } = 800;
    public int MaximumWindowWidth { get; set; } = 1280;
    public int MaximumWindowHeight { get; set; } = 800;
    public bool RememberWindowSize { get; set; } = true;
    public int LastWindowWidth { get; set; } = 1280;
    public int LastWindowHeight { get; set; } = 800;
    public bool RememberWindowPosition { get; set; } = true;
    public int LastWindowX { get; set; } = 0;
    public int LastWindowY { get; set; } = 0;
    public bool MinimizeToTray { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
}

public class DataDisplaySettings
{
    public bool ShowMeasurementTime { get; set; } = true;
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public bool ShowDataAgeIndicator { get; set; } = true;

    // Пороги
    public int Threshold_DarkGray { get; set; } = 60; // сек
    public int Threshold_LightGray { get; set; } = 180; // сек
    public int Threshold_Blinking { get; set; } = 300; // сек
    public int Threshold_NoData { get; set; } = 600; // сек

    // Кольори
    public string ColorFresh { get; set; } = "#000000";
    public string ColorDarkGray { get; set; } = "#808080";
    public string ColorLightGray { get; set; } = "#C0C0C0";
    public string ColorNoData { get; set; } = "#CCCCCC";
    public string BackgroundFlashing { get; set; } = "#FFFF00";

    // Блимання
    public bool BlinkingEnabled { get; set; } = true;
    public int BlinkInterval { get; set; } = 500; // мс
    public int FlashDuration { get; set; } = 500; // мс
    public string BlinkType { get; set; } = "Opacity"; // Opacity, Color, Both
    public int MinimumOpacity { get; set; } = 30; // %
    public int MaximumOpacity { get; set; } = 100; // %

    public string NoDataText { get; set; } = "- - -";
}

public class TransmissionSettings
{
    public bool Enabled { get; set; } = true;
    public int TransmissionIntervalMs { get; set; } = 1000; // мс
    public string MessageFormat { get; set; } = "JSON"; // JSON, CSV, TEXT
    public bool EnableBuffering { get; set; } = true;
    public int BufferSize { get; set; } = 10;
    public bool LogTransmissions { get; set; } = true;

    public TransmissionChannels Channels { get; set; } = new();
    public TcpTransmissionSettings TCP { get; set; } = new();
    public ComTransmissionSettings COM { get; set; } = new();
}

public class TransmissionChannels
{
    public bool Lufft_Speed { get; set; } = true;
    public bool Lufft_Direction { get; set; } = true;
    public bool Lufft_Temperature { get; set; } = false;
    public bool Lufft_Pressure { get; set; } = false;
    public bool Lufft_Statistics_10min { get; set; } = true;
    public bool Lufft_Statistics_3hour { get; set; } = true;
    public bool Lufft_Statistics_24hour { get; set; } = true;

    public bool Boeder_Speed_Primary { get; set; } = true;
    public bool Boeder_Speed_Corrected { get; set; } = true;
    public bool Boeder_Direction { get; set; } = true;

    public bool Calibration_Coefficient_a { get; set; } = true;
    public bool Calibration_Coefficient_b { get; set; } = true;
    public bool Calibration_R2 { get; set; } = false;
    public bool Calibration_RMSE { get; set; } = false;

    public bool Timestamp { get; set; } = true;
    public bool Status_Lufft { get; set; } = true;
    public bool Status_Boeder { get; set; } = true;
    public bool Signal_Quality { get; set; } = true;
}

public class TcpTransmissionSettings
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "192.168.1.50";
    public int Port { get; set; } = 5005;
    public int TimeoutMs { get; set; } = 5000;
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public string Delimiter { get; set; } = ";";
    public string LineTerminator { get; set; } = "\r\n";
}

public class ComTransmissionSettings
{
    public bool Enabled { get; set; } = false;
    public string PortName { get; set; } = "COM4";
    public int BaudRate { get; set; } = 19200;
    public int DataBits { get; set; } = 8;
    public int StopBits { get; set; } = 1;
    public string Parity { get; set; } = "None";
    public string Handshake { get; set; } = "None";
    public int TimeoutMs { get; set; } = 5000;
    public string Delimiter { get; set; } = ";";
    public string LineTerminator { get; set; } = "\r\n";
}

public class Configuration
{
    public LufftSettings Lufft { get; set; } = new();
    public BoederSettings Boeder { get; set; } = new();
    public CalibrationSettings Calibration { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public IdleWarningSettings IdleWarningSettings { get; set; } = new();
    public UISettings UISettings { get; set; } = new();
    public DataDisplaySettings DataDisplaySettings { get; set; } = new();
    public TransmissionSettings TransmissionSettings { get; set; } = new();
}
