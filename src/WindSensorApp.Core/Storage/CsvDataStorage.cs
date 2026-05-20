using System.Globalization;
using WindSensorApp.Core.Models;
using WindSensorApp.Core.Logging;

namespace WindSensorApp.Core.Storage;

public interface IDataStorage
{
    Task SaveMeasurementAsync(SensorReading reading);
    Task<List<SensorReading>> LoadMeasurementsAsync(DateTime startTime, DateTime endTime);
    Task SaveCalibrationAsync(CalibrationData calibration);
    Task<List<CalibrationData>> LoadCalibrationAsync();
}

public class CsvDataStorage : IDataStorage
{
    private readonly string _archiveFolder;
    private readonly string _calibrationFile;

    public CsvDataStorage(string archiveFolder)
    {
        _archiveFolder = archiveFolder;
        _calibrationFile = Path.Combine(archiveFolder, "calibration.csv");

        if (!Directory.Exists(archiveFolder))
            Directory.CreateDirectory(archiveFolder);
    }

    public async Task SaveMeasurementAsync(SensorReading reading)
    {
        try
        {
            string filename = Path.Combine(_archiveFolder, $"measurements_{DateTime.Now:yyyy-MM-dd}.csv");
            bool fileExists = File.Exists(filename);

            using (var writer = new StreamWriter(filename, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,Sensor_Name,Speed,Speed_Primary,Speed_Corrected,Direction,Temperature,Pressure,Valid");
                }

                writer.WriteLine(
                    $"{reading.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{reading.SensorName},{reading.Speed:F4}," +
                    $"{reading.SpeedPrimary:F4},{reading.SpeedCorrected:F4},{reading.Direction:F2}," +
                    $"{(reading.Temperature?.ToString("F2") ?? "")},(reading.Pressure?.ToString("F2") ?? "")},{reading.IsValid}"
                );
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving measurement: {ex.Message}");
        }
    }

    public async Task<List<SensorReading>> LoadMeasurementsAsync(DateTime startTime, DateTime endTime)
    {
        var readings = new List<SensorReading>();

        try
        {
            string pattern = "measurements_*.csv";
            var files = Directory.GetFiles(_archiveFolder, pattern)
                .Where(f => File.GetCreationTime(f) >= startTime && File.GetCreationTime(f) <= endTime)
                .ToList();

            foreach (var file in files)
            {
                using (var reader = new StreamReader(file))
                {
                    string? line;
                    bool isHeader = true;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (isHeader)
                        {
                            isHeader = false;
                            continue;
                        }

                        var parts = line.Split(',');
                        if (parts.Length >= 7 && DateTime.TryParseExact(
                            parts[0], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var timestamp))
                        {
                            if (timestamp >= startTime && timestamp <= endTime)
                            {
                                readings.Add(new SensorReading
                                {
                                    Timestamp = timestamp,
                                    SensorName = parts[1],
                                    Speed = double.Parse(parts[2]),
                                    SpeedPrimary = double.Parse(parts[3]),
                                    SpeedCorrected = double.Parse(parts[4]),
                                    Direction = double.Parse(parts[5]),
                                    Temperature = parts[6] == "" ? null : double.Parse(parts[6]),
                                    Pressure = parts[7] == "" ? null : double.Parse(parts[7]),
                                    IsValid = bool.Parse(parts[8])
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading measurements: {ex.Message}");
        }

        return readings;
    }

    public async Task SaveCalibrationAsync(CalibrationData calibration)
    {
        try
        {
            bool fileExists = File.Exists(_calibrationFile);

            using (var writer = new StreamWriter(_calibrationFile, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("Timestamp,Direction_Sector,Speed_Range,Coefficient_A,Coefficient_B,R2,RMSE,Data_Points,Valid");
                }

                writer.WriteLine(
                    $"{calibration.Timestamp:yyyy-MM-dd HH:mm:ss},{calibration.DirectionSector}," +
                    $"{calibration.SpeedRange},{calibration.CoefficientA:F6},{calibration.CoefficientB:F6}," +
                    $"{calibration.R2:F4},{calibration.RMSE:F6},{calibration.DataPointCount},{calibration.IsValid}"
                );
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error saving calibration: {ex.Message}");
        }
    }

    public async Task<List<CalibrationData>> LoadCalibrationAsync()
    {
        var calibrations = new List<CalibrationData>();

        try
        {
            if (!File.Exists(_calibrationFile))
                return calibrations;

            using (var reader = new StreamReader(_calibrationFile))
            {
                string? line;
                bool isHeader = true;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    var parts = line.Split(',');
                    if (parts.Length >= 9)
                    {
                        calibrations.Add(new CalibrationData
                        {
                            Timestamp = DateTime.Parse(parts[0]),
                            DirectionSector = int.Parse(parts[1]),
                            SpeedRange = int.Parse(parts[2]),
                            CoefficientA = double.Parse(parts[3]),
                            CoefficientB = double.Parse(parts[4]),
                            R2 = double.Parse(parts[5]),
                            RMSE = double.Parse(parts[6]),
                            DataPointCount = int.Parse(parts[7]),
                            IsValid = bool.Parse(parts[8])
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error loading calibration: {ex.Message}");
        }

        return calibrations;
    }
}
