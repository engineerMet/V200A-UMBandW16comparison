using System.Globalization;
using WindSensorApp.Core.Models;
using WindSensorApp.Core.Logging;

namespace WindSensorApp.Core.Calibration;

public class LinearRegression
{
    public double CoefficientA { get; set; }
    public double CoefficientB { get; set; }
    public double R2 { get; set; }
    public double RMSE { get; set; }
    public int DataPointCount { get; private set; }

    public static LinearRegression? Calculate(List<(double x, double y)> data, bool removeOutliers = true)
    {
        if (data.Count < 2)
            return null;

        var workingData = new List<(double x, double y)>(data);

        // Видалити викиди >3σ
        if (removeOutliers)
        {
            workingData = RemoveOutliers(workingData, 3.0);
        }

        if (workingData.Count < 2)
            return null;

        int n = workingData.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0, sumY2 = 0;

        foreach (var point in workingData)
        {
            sumX += point.x;
            sumY += point.y;
            sumXY += point.x * point.y;
            sumX2 += point.x * point.x;
            sumY2 += point.y * point.y;
        }

        double denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-10)
            return null;

        double a = (n * sumXY - sumX * sumY) / denominator;
        double b = (sumY - a * sumX) / n;

        // Calculate R²
        double ssRes = 0, ssTot = 0;
        double yMean = sumY / n;

        foreach (var point in workingData)
        {
            double yPred = a * point.x + b;
            ssRes += (point.y - yPred) * (point.y - yPred);
            ssTot += (point.y - yMean) * (point.y - yMean);
        }

        double r2 = ssTot == 0 ? 0 : 1 - (ssRes / ssTot);

        // Calculate RMSE
        double rmse = Math.Sqrt(ssRes / n);

        return new LinearRegression
        {
            CoefficientA = a,
            CoefficientB = b,
            R2 = r2,
            RMSE = rmse,
            DataPointCount = workingData.Count
        };
    }

    private static List<(double x, double y)> RemoveOutliers(
        List<(double x, double y)> data, double sigmaThreshold = 3.0)
    {
        if (data.Count < 3)
            return data;

        // Calculate mean and standard deviation for Y values
        double mean = data.Average(p => p.y);
        double variance = data.Average(p => Math.Pow(p.y - mean, 2));
        double sigma = Math.Sqrt(variance);

        // Filter outliers
        return data.Where(p => Math.Abs(p.y - mean) <= sigmaThreshold * sigma).ToList();
    }
}

public class CalibrationMatrix
{
    private readonly Dictionary<(int direction, int speed), CalibrationData> _coefficients;
    private readonly CalibrationSettings _settings;

    public CalibrationMatrix(CalibrationSettings settings)
    {
        _settings = settings;
        _coefficients = new Dictionary<(int, int), CalibrationData>();
    }

    public void AddCoefficient(int directionSector, int speedRange, double a, double b, double r2, double rmse, int pointCount)
    {
        if (!ValidateSectorAndRange(directionSector, speedRange))
            return;

        var key = (directionSector, speedRange);
        _coefficients[key] = new CalibrationData
        {
            Timestamp = DateTime.Now,
            DirectionSector = directionSector,
            SpeedRange = speedRange,
            CoefficientA = a,
            CoefficientB = b,
            R2 = r2,
            RMSE = rmse,
            DataPointCount = pointCount,
            IsValid = r2 >= 0.5 && pointCount >= _settings.MinDataPointsPerCoefficient
        };
    }

    public CalibrationData? GetCoefficient(int directionSector, int speedRange)
    {
        if (!ValidateSectorAndRange(directionSector, speedRange))
            return null;

        _coefficients.TryGetValue((directionSector, speedRange), out var result);
        return result;
    }

    public double ApplyCalibration(double boederSpeed, int directionSector, int speedRange)
    {
        var coeff = GetCoefficient(directionSector, speedRange);
        if (coeff == null || !coeff.IsValid)
            return boederSpeed; // Return uncalibrated if no valid coefficient

        // Apply linear regression: CalibratedSpeed = a * BoederSpeed + b
        return coeff.CoefficientA * boederSpeed + coeff.CoefficientB;
    }

    public int GetDirectionSector(double directionDegrees)
    {
        directionDegrees = directionDegrees % 360;
        return (int)(directionDegrees / _settings.DegreesPerSector);
    }

    public int GetSpeedRange(double speedMps)
    {
        return Math.Min((int)(speedMps / _settings.MetersPerSecondPerRange), _settings.WindSpeedRanges - 1);
    }

    private bool ValidateSectorAndRange(int sector, int range)
    {
        return sector >= 0 && sector < _settings.WindDirectionSectors &&
               range >= 0 && range < _settings.WindSpeedRanges;
    }

    public Dictionary<(int, int), CalibrationData> GetAllCoefficients() => _coefficients;

    public int GetCoveragePercentage()
    {
        int totalCells = _settings.WindDirectionSectors * _settings.WindSpeedRanges;
        int filledCells = _coefficients.Count;
        return (filledCells * 100) / totalCells;
    }
}

public class CalibrationEngine
{
    private readonly CalibrationMatrix _calibrationMatrix;
    private readonly CalibrationSettings _settings;
    private readonly Dictionary<(int direction, int speed), List<(double boeder, double lufft)>> _rawData;

    public CalibrationEngine(CalibrationSettings settings)
    {
        _settings = settings;
        _calibrationMatrix = new CalibrationMatrix(settings);
        _rawData = new Dictionary<(int, int), List<(double, double)>>();
    }

    public void AddCalibrationPoint(double boederSpeed, double lufftSpeed, double directionDegrees)
    {
        int directionSector = _calibrationMatrix.GetDirectionSector(directionDegrees);
        int speedRange = _calibrationMatrix.GetSpeedRange(boederSpeed);

        var key = (directionSector, speedRange);
        if (!_rawData.ContainsKey(key))
        {
            _rawData[key] = new List<(double, double)>();
        }

        _rawData[key].Add((boederSpeed, lufftSpeed));
    }

    public bool CalculateCoefficients(int? directionSectorFilter = null, int? speedRangeFilter = null)
    {
        int calculatedCount = 0;

        foreach (var kvp in _rawData)
        {
            int dirSector = kvp.Key.direction;
            int spdRange = kvp.Key.speed;
            var points = kvp.Value;

            // Apply filters if specified
            if (directionSectorFilter.HasValue && dirSector != directionSectorFilter)
                continue;
            if (speedRangeFilter.HasValue && spdRange != speedRangeFilter)
                continue;

            // Check minimum data points
            if (points.Count < _settings.MinDataPointsPerCoefficient)
            {
                Logger.Warning($"Not enough data points for sector {dirSector}, range {spdRange}: {points.Count}/{_settings.MinDataPointsPerCoefficient}");
                continue;
            }

            // Calculate linear regression
            var regression = LinearRegression.Calculate(points, _settings.EnableOutlierDetection);
            if (regression != null)
            {
                _calibrationMatrix.AddCoefficient(
                    dirSector,
                    spdRange,
                    regression.CoefficientA,
                    regression.CoefficientB,
                    regression.R2,
                    regression.RMSE,
                    regression.DataPointCount
                );
                calculatedCount++;
            }
        }

        Logger.Info($"Calculated {calculatedCount} calibration coefficients");
        return calculatedCount > 0;
    }

    public double ApplyCalibration(double boederSpeed, double directionDegrees)
    {
        int directionSector = _calibrationMatrix.GetDirectionSector(directionDegrees);
        int speedRange = _calibrationMatrix.GetSpeedRange(boederSpeed);
        return _calibrationMatrix.ApplyCalibration(boederSpeed, directionSector, speedRange);
    }

    public CalibrationMatrix GetMatrix() => _calibrationMatrix;

    public void ExportToCSV(string filePath)
    {
        try
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Direction_Sector,Speed_Range,Coefficient_A,Coefficient_B,R2,RMSE,Data_Points,Valid");

                var coefficients = _calibrationMatrix.GetAllCoefficients();
                foreach (var coeff in coefficients.Values.OrderBy(c => c.DirectionSector).ThenBy(c => c.SpeedRange))
                {
                    writer.WriteLine(
                        $"{coeff.DirectionSector},{coeff.SpeedRange},{coeff.CoefficientA:F6},{coeff.CoefficientB:F6},{coeff.R2:F4},{coeff.RMSE:F6},{coeff.DataPointCount},{coeff.IsValid}"
                    );
                }
            }
            Logger.Info($"Calibration data exported to {filePath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error exporting calibration data: {ex.Message}");
        }
    }
}
