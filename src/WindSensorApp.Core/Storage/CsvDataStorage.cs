using System.Text;
using WindSensorApp.Core.Logging;
using WindSensorApp.Core.Models;

namespace WindSensorApp.Core.Storage;

public interface IDataStorage
{
    Task WriteReadingAsync(SensorReading reading);
    Task WriteAggregatedAsync(AggregatedData data);
    Task<List<SensorReading>> ReadReadingsAsync(DateTime from, DateTime to);
    Task ExportAsync(string filePath);
}

public class CsvDataStorage : IDataStorage
{
    private readonly string _archiveFolder;
    private readonly object _lockObject = new();
    private string? _currentFileName;
    private StreamWriter? _currentWriter;

    public CsvDataStorage(string archiveFolder = "./data/archives")
    {
        _archiveFolder = archiveFolder;
        Directory.CreateDirectory(_archiveFolder);
    }

    public async Task WriteReadingAsync(SensorReading reading)
    {
        lock (_lockObject)
        {
            try
            {
                string fileName = GetFileName(reading.Timestamp);
                
                // Якщо файл змінився, закрити старий
                if (_currentFileName != fileName)
                {
                    _currentWriter?.Dispose();
                    _currentWriter = null;
                }

                // Відкрити файл
                if (_currentWriter == null)
                {
                    string filePath = Path.Combine(_archiveFolder, fileName);
                    bool fileExists = File.Exists(filePath);
                    _currentWriter = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8);
                    _currentFileName = fileName;

                    // Записати заголовок якщо новий файл
                    if (!fileExists)
                    {
                        _currentWriter.WriteLine(GetCsvHeader());
                    }
                }

                // Записати дані
                string line = reading.SensorName switch
                {
                    "Lufft V200A-UMB" => FormatLufftReading(reading),
                    "Boeder W16" => FormatBoederReading(reading),
                    _ => FormatGenericReading(reading)
                };

                _currentWriter.WriteLine(line);
                _currentWriter.Flush();
            }
            catch (Exception ex)
            {
                Logger.Error($"CSV write error: {ex.Message}");
            }
        }

        await Task.CompletedTask;
    }

    public async Task WriteAggregatedAsync(AggregatedData data)
    {
        string fileName = $"aggregated_{data.Timestamp:yyyy-MM-dd}.csv";
        string filePath = Path.Combine(_archiveFolder, fileName);

        try
        {
            lock (_lockObject)
            {
                bool fileExists = File.Exists(filePath);
                using (var writer = new StreamWriter(filePath, append: true, encoding: Encoding.UTF8))
                {
                    if (!fileExists)
                    {
                        writer.WriteLine("Timestamp,SensorName,Stats10min_Max,Stats10min_Min,Stats10min_Avg,Stats3hour_Max,Stats3hour_Min,Stats3hour_Avg,Stats24hour_Max,Stats24hour_Min,Stats24hour_Avg");
                    }

                    writer.WriteLine($"{data.Timestamp:yyyy-MM-dd HH:mm:ss},{data.SensorName}" +
                        $",{data.Stats10Min.MaxSpeed:F2},{data.Stats10Min.MinSpeed:F2},{data.Stats10Min.AvgSpeed:F2}" +
                        $",{data.Stats3Hour.MaxSpeed:F2},{data.Stats3Hour.MinSpeed:F2},{data.Stats3Hour.AvgSpeed:F2}" +
                        $",{data.Stats24Hour.MaxSpeed:F2},{data.Stats24Hour.MinSpeed:F2},{data.Stats24Hour.AvgSpeed:F2}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CSV aggregated write error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public async Task<List<SensorReading>> ReadReadingsAsync(DateTime from, DateTime to)
    {
        var readings = new List<SensorReading>();

        try
        {
            for (DateTime date = from.Date; date <= to.Date; date = date.AddDays(1))
            {
                string fileName = GetFileName(date);
                string filePath = Path.Combine(_archiveFolder, fileName);

                if (!File.Exists(filePath))
                    continue;

                using (var reader = new StreamReader(filePath))
                {
                    string? line;
                    bool skipHeader = true;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (skipHeader)
                        {
                            skipHeader = false;
                            continue;
                        }

                        var reading = ParseCsvLine(line);
                        if (reading?.Timestamp >= from && reading?.Timestamp <= to)
                        {
                            readings.Add(reading);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CSV read error: {ex.Message}");
        }

        return readings;
    }

    public async Task ExportAsync(string filePath)
    {
        Logger.Info($"Exporting data to {filePath}");
        await Task.CompletedTask;
    }

    private string GetFileName(DateTime dateTime) => $"sensor_data_{dateTime:yyyy-MM-dd}.csv";

    private string GetCsvHeader() =>
        "Timestamp,SensorName,Speed,SpeedPrimary,SpeedCorrected,Direction,Temperature,Pressure,IsValid,ErrorMessage";

    private string FormatLufftReading(SensorReading reading) =>
        $"{reading.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{reading.SensorName}" +
        $",{reading.Speed:F2},,{reading.SpeedCorrected:F2},{reading.Direction:F1}" +
        $",{reading.Temperature:F2},{reading.Pressure:F2},{reading.IsValid},\"{reading.ErrorMessage}\"";

    private string FormatBoederReading(SensorReading reading) =>
        $"{reading.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{reading.SensorName}" +
        $",{reading.Speed:F2},{reading.SpeedPrimary:F2},{reading.SpeedCorrected:F2},{reading.Direction:F1}" +
        $",,{reading.IsValid},\"{reading.ErrorMessage}\"";

    private string FormatGenericReading(SensorReading reading) =>
        $"{reading.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{reading.SensorName}" +
        $",{reading.Speed:F2},,{reading.SpeedCorrected:F2},{reading.Direction:F1}" +
        $",,{reading.IsValid},\"{reading.ErrorMessage}\"";

    private SensorReading? ParseCsvLine(string line)
    {
        try
        {
            var parts = line.Split(',');
            if (parts.Length < 9)
                return null;

            return new SensorReading
            {
                Timestamp = DateTime.Parse(parts[0]),
                SensorName = parts[1],
                Speed = double.Parse(parts[2]),
                SpeedPrimary = string.IsNullOrEmpty(parts[3]) ? 0 : double.Parse(parts[3]),
                SpeedCorrected = string.IsNullOrEmpty(parts[4]) ? 0 : double.Parse(parts[4]),
                Direction = double.Parse(parts[5]),
                Temperature = string.IsNullOrEmpty(parts[6]) ? null : double.Parse(parts[6]),
                Pressure = string.IsNullOrEmpty(parts[7]) ? null : double.Parse(parts[7]),
                IsValid = bool.Parse(parts[8]),
                ErrorMessage = parts.Length > 9 ? parts[9] : string.Empty
            };
        }
        catch
        {
            return null;
        }
    }
}
