using Microsoft.VisualStudio.TestTools.UnitTesting;
using V200A_UMBandW16comparison.Models;
using V200A_UMBandW16comparison.Utils;

namespace V200A_UMBandW16comparison.Tests
{
    [TestClass]
    public class CalibrationTests
    {
        private CalibrationService _calibrationService;

        [TestInitialize]
        public void Setup()
        {
            _calibrationService = new CalibrationService();
        }

        [TestMethod]
        public void TestCalibrationOffsetCalculation()
        {
            // Arrange
            double referenceValue = 20.0;
            double measuredValue = 20.5;

            // Act
            double offset = _calibrationService.CalculateOffset(referenceValue, measuredValue);

            // Assert
            Assert.AreEqual(0.5, offset, 0.01);
        }

        [TestMethod]
        public void TestCalibrationFactor()
        {
            // Arrange
            double referenceValue = 10.0;
            double measuredValue = 9.5;

            // Act
            double factor = _calibrationService.CalculateCalibrationFactor(referenceValue, measuredValue);

            // Assert
            Assert.IsTrue(factor > 0);
            Assert.IsTrue(factor <= 1.1); // Reasonable calibration range
        }

        [TestMethod]
        public void TestApplyCalibration()
        {
            // Arrange
            var calibration = new CalibrationData
            {
                Offset = 0.5,
                Factor = 1.02
            };
            double rawValue = 20.0;

            // Act
            double calibratedValue = _calibrationService.ApplyCalibration(rawValue, calibration);

            // Assert
            Assert.IsNotNull(calibratedValue);
            Assert.IsTrue(calibratedValue > 0);
        }

        [TestMethod]
        public void TestCalibrationDataPersistence()
        {
            // Arrange
            var testCalibration = new CalibrationData
            {
                SensorId = "V200A",
                Offset = 1.5,
                Factor = 1.01,
                Timestamp = System.DateTime.Now
            };

            // Act
            _calibrationService.SaveCalibration(testCalibration);
            var loadedCalibration = _calibrationService.LoadCalibration("V200A");

            // Assert
            Assert.IsNotNull(loadedCalibration);
            Assert.AreEqual(testCalibration.Offset, loadedCalibration.Offset);
        }
    }
}
