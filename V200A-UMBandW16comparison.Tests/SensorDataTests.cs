using Microsoft.VisualStudio.TestTools.UnitTesting;
using V200A_UMBandW16comparison.Models;
using V200A_UMBandW16comparison.Utils;

namespace V200A_UMBandW16comparison.Tests
{
    [TestClass]
    public class SensorDataTests
    {
        private SensorDataService _sensorDataService;

        [TestInitialize]
        public void Setup()
        {
            _sensorDataService = new SensorDataService();
        }

        [TestMethod]
        public void TestSensorDataInitialization()
        {
            // Arrange & Act
            var sensorData = _sensorDataService.GetLatestData();

            // Assert
            Assert.IsNotNull(sensorData);
        }

        [TestMethod]
        public void TestSensorDataValidation()
        {
            // Arrange
            var testData = new SensorData
            {
                Timestamp = System.DateTime.Now,
                Temperature = 25.5,
                Humidity = 60.0,
                WindSpeed = 5.2
            };

            // Act
            bool isValid = _sensorDataService.ValidateData(testData);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void TestSensorDataInvalidValues()
        {
            // Arrange
            var invalidData = new SensorData
            {
                Timestamp = System.DateTime.Now,
                Temperature = -500, // Invalid value
                Humidity = 150.0,   // Out of range
                WindSpeed = -10.0   // Negative wind speed
            };

            // Act
            bool isValid = _sensorDataService.ValidateData(invalidData);

            // Assert
            Assert.IsFalse(isValid);
        }
    }
}
