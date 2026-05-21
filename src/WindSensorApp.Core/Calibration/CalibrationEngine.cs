using WindSensorApp.Core.Logging;
using WindSensorApp.Core.Models;

namespace WindSensorApp.Core.Calibration;

public class LinearRegression
{
    public double CoefficientA { get; set; }
    public double CoefficientB { get; set; }
    public double R2 { get; set; }
    public double RMSE { get; set; }
    public int DataPointCount { get; set; }

    public static LinearRegression Calculate(List<(double x, double y)> dataPoints, double outlierThresholdSigma = 3.0)
    {
        var result = new LinearRegression { DataPointCount = dataPoints.Count };

        if (dataPoints.Count < 2)
        {
            Logger.Warning($"Not enough data points for regression: {dataPoints.Count}");
            return result;
        }

        // Видалення викидів
        var filteredPoints = RemoveOutliers(dataPoints, outlierThresholdSigma);

        if (filteredPoints.Count < 2)
        {
            Logger.Warning($"Not enough data points after outlier removal: {filteredPoints.Count}");
            return result;
        }

        // Розрахунок
        double n = filteredPoints.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

        foreach (var point in filteredPoints)
        {
            sumX += point.x;
            sumY += point.y;
            sumXY += point.x * point.y;
            sumX2 += point.x * point.x;
            sumY2 += point.y * point.y;
        }

        double denominator = (n * sumX2) - (sumX * sumX);

        if (Math.Abs(denominator) < 1e-10)
        {
            Logger.Warning("Cannot calculate regression: denominator too small");
            return result;
        }

        result.CoefficientB = ((n * sumXY) - (sumX * sumY)) / denominator;
        result.CoefficientA = (sumY - (result.CoefficientB * sumX)) / n;

        // R-squared
        double meanY = sumY / n;
        double ssRes = 0, ssTot = 0;

        foreach (var point in filteredPoints)
        {
            double predicted = result.CoefficientA + (result.CoefficientB * point.x);
            ssRes += Math.Pow(point.y - predicted, 2);
            ssTot += Math.Pow(point.y - meanY, 2);
        }

        result.R2 = Math.Abs(ssTot) < 1e-10 ? 0 : 1 - (ssRes / ssTot);

        // RMSE
        result.RMSE = Math.Sqrt(ssRes / n);

        Logger.Debug($"Linear regression: a={result.CoefficientA:F4}, b={result.CoefficientB:F4}, R²={result.R2:F4}, RMSE={result.RMSE:F4}");

        return result;
    }

    private static List<(double x, double y)> RemoveOutliers(List<(double x, double y)> dataPoints, double thresholdSigma)
    {
        if (dataPoints.Count < 3)
            return new List<(double, double)>(dataPoints);

        // Розрахунок середнього та стандартного відхилення
        double mean = dataPoints.Average(p => p.y);
        double variance = dataPoints.Average(p => Math.Pow(p.y - mean, 2));
        double stdDev = Math.Sqrt(variance);

        // Фільтрація
        var filtered = dataPoints.Where(p => Math.Abs(p.y - mean) <= (thresholdSigma * stdDev)).ToList();

        int removed = dataPoints.Count - filtered.Count;
        if (removed > 0)
        {
            Logger.Debug($"Removed {removed} outliers from {dataPoints.Count} data points");
        }

        return filtered;
    }
}

public class CalibrationMatrix
{
    public CalibrationData[,] Matrix { get; private set; }
    public int DirectionSectors { get; private set; }
    public int SpeedRanges { get; private set; }
    public double DegreesPerSector { get; private set; }
    public double MetersPerSecondPerRange { get; private set; }

    public CalibrationMatrix(int directionSectors = 36, int speedRanges = 36, 
        double degreesPerSector = 10.0, double metersPerSecondPerRange = 1.0)
    {
        DirectionSectors = directionSectors;
        SpeedRanges = speedRanges;
        DegreesPerSector = degreesPerSector;
        MetersPerSecondPerRange = metersPerSecondPerRange;

        // Ініціалізація матриці
        Matrix = new CalibrationData[directionSectors, speedRanges];
        for (int i = 0; i < directionSectors; i++)
        {
            for (int j = 0; j < speedRanges; j++)
            {
                Matrix[i, j] = new CalibrationData
                {
                    DirectionSector = i,
                    SpeedRange = j,
                    CoefficientA = 1.0,
                    CoefficientB = 0.0,
                    R2 = 0.0,
                    RMSE = 0.0,
                    IsValid = false
                };
            }
        }
    }

    public int GetDirectionSector(double degrees)
    {
        // Нормалізація 0-360
        var normalized = degrees % 360;
        if (normalized < 0) normalized += 360;
        
        return (int)(normalized / DegreesPerSector) % DirectionSectors;
    }

    public int GetSpeedRange(double speedMps)
    {
        int range = (int)(speedMps / MetersPerSecondPerRange);
        return Math.Min(range, SpeedRanges - 1);
    }

    public CalibrationData GetCalibration(double direction, double speed)
    {
        int sectorIdx = GetDirectionSector(direction);
        int rangeIdx = GetSpeedRange(speed);
        return Matrix[sectorIdx, rangeIdx];
    }

    public void SetCalibration(int directionSector, int speedRange, CalibrationData calibration)
    {
        if (directionSector >= 0 && directionSector < DirectionSectors &&
            speedRange >= 0 && speedRange < SpeedRanges)
        {
            Matrix[directionSector, speedRange] = calibration;
        }
    }
}

public class CalibrationEngine
{
    private readonly CalibrationMatrix _matrix;
    private readonly int _minDataPoints;
    private readonly Dictionary<string, List<(double lufft, double boeder)>> _calibrationData;

    public CalibrationEngine(CalibrationMatrix matrix, int minDataPoints = 100)
    {
        _matrix = matrix;
        _minDataPoints = minDataPoints;
        _calibrationData = new Dictionary<string, List<(double, double)>>();
    }

    public void AddCalibrationPoint(double direction, double speed, double lufftSpeed, double boederSpeed)
    {
        string key = $"{_matrix.GetDirectionSector(direction)}_{_matrix.GetSpeedRange(speed)}";
        
        if (!_calibrationData.ContainsKey(key))
        {
            _calibrationData[key] = new List<(double, double)>();
        }

        _calibrationData[key].Add((lufftSpeed, boederSpeed));
    }

    public void CalculateCoefficients()
    {
        foreach (var kvp in _calibrationData)
        {
            var parts = kvp.Key.Split('_');
            int directionSector = int.Parse(parts[0]);
            int speedRange = int.Parse(parts[1]);
            var dataPoints = kvp.Value.Select(p => (p.lufftSpeed, p.boederSpeed)).ToList();

            if (dataPoints.Count >= _minDataPoints)
            {
                var regression = LinearRegression.Calculate(dataPoints);

                var calibration = new CalibrationData
                {
                    DirectionSector = directionSector,
                    SpeedRange = speedRange,
                    CoefficientA = regression.CoefficientA,
                    CoefficientB = regression.CoefficientB,
                    R2 = regression.R2,
                    RMSE = regression.RMSE,
                    DataPointCount = dataPoints.Count,
                    IsValid = regression.R2 > 0.9 // Валідна якщо R² > 0.9
                };

                _matrix.SetCalibration(directionSector, speedRange, calibration);
                Logger.Info($"Calibration updated: {calibration}");
            }
            else
            {
                Logger.Warning($"Insufficient data for sector {directionSector}, range {speedRange}: {dataPoints.Count}/{_minDataPoints}");
            }
        }
    }

    public double ApplyCalibration(double direction, double boederSpeed)
    {
        var calibration = _matrix.GetCalibration(direction, boederSpeed);

        if (!calibration.IsValid)
        {
            Logger.Warning($"Using uncalibrated data for direction {direction}, speed {boederSpeed}");
            return boederSpeed; // Повернути оригінальне значення
        }

        // Формула: Lufft = a + b * Boeder
        return calibration.CoefficientA + (calibration.CoefficientB * boederSpeed);
    }

    public CalibrationMatrix GetMatrix() => _matrix;

    public void ClearCalibrationData()
    {
        _calibrationData.Clear();
        Logger.Info("Calibration data cleared");
    }
}
