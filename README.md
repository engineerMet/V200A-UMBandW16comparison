# V200A-UMB and W16 Comparison

## Wind Sensor Data Logger & Calibration System

A comprehensive C# WPF application for data logging, real-time monitoring, and calibration comparison between Lufft V200A-UMB and Boeder W16 wind sensors.

### Features

#### 📊 Data Acquisition
- Real-time monitoring of wind speed and direction from both sensors
- Support for TCP and COM port connections
- UMB Protocol (Lufft V200A-UMB)
- Modbus RTU Protocol (Boeder W16)
- Configurable sampling intervals

#### 📈 Data Aggregation
- 10-minute moving window (max, min, avg)
- 3-hour aggregation (fixed periods or moving window)
- 24-hour aggregation (calendar, meteorological, or moving window)

#### 🔬 Calibration
- 36×36 calibration matrix (36 wind direction sectors × 36 speed ranges)
- Linear regression coefficients with R² validation
- Automatic outlier detection (>3σ)
- Wind sector division: 10° per sector (0-359.9°)
- Speed ranges: 1 m/s per range (0-35+ m/s)

#### 💾 Storage
- CSV archive format (primary)
- SQLite export option
- Automatic weekly exports
- Configurable backup intervals

#### 🔔 Notifications & Alerts
- Idle warning system (sound + visual feedback)
- Data age indicators with color coding
- Real-time data quality monitoring
- System tray notifications

#### 📡 Data Transmission
- TCP socket transmission
- COM port transmission
- Multiple message formats: JSON, CSV, TEXT
- Configurable transmission intervals
- Data buffering support

#### 🎨 User Interface
- WPF with responsive layout (supports 1280×800 minimum)
- Real-time monitoring dashboard
- Calibration matrix viewer
- Settings window with validation
- INI configuration editor

### Project Structure

```
V200A-UMBandW16comparison/
├── src/
│   ├── WindSensorApp.Core/          # Core logic (no UI dependencies)
│   │   ├── Models/
│   │   ├── Communication/
│   │   ├── Protocols/
│   │   ├── Sensors/
│   │   ├── Calibration/
│   │   ├── Storage/
│   │   ├── Notification/
│   │   └── Transmission/
│   └── WindSensorApp.UI/            # WPF application
│       ├── Views/
│       ├── ViewModels/
│       ├── Controls/
│       └── Converters/
├── tests/
│   └── WindSensorApp.Core.Tests/    # Unit tests
├── config/
│   ├── sensors.ini
│   ├── calibration.ini
│   ├── ui.ini
│   ├── data-display.ini
│   ├── transmission.ini
│   └── archive.ini
├── docs/
│   ├── INSTALL.md
│   ├── USER_GUIDE.md
│   ├── ARCHITECTURE.md
│   └── API.md
└── README.md
```

### Requirements

- **.NET 6.0 LTS** or higher
- Windows 10/11 (WPF requirement)
- Visual Studio 2022 or Rider

### Dependencies

- `Newtonsoft.Json` 13.0.3 - JSON serialization
- `IniParser` 2.5.2 - INI configuration parsing
- `Serilog` 3.1.0 - Structured logging
- `Serilog.Sinks.File` 5.0.0 - File logging
- `SerialPortStream` 3.0.0 - COM port communication
- `CommunityToolkit.Mvvm` 8.2.2 - MVVM pattern support

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/engineerMet/V200A-UMBandW16comparison.git
   cd V200A-UMBandW16comparison
   ```

2. **Open in Visual Studio**
   ```bash
   start WindSensorApp.sln
   ```

3. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

4. **Build the solution**
   ```bash
   dotnet build
   ```

5. **Configure sensors** (edit `config/sensors.ini`)
   ```ini
   [Lufft]
   Enabled=1
   ConnectionType=TCP
   [Lufft.TCP]
   Host=192.168.1.100
   Port=4001
   ```

6. **Run the application**
   ```bash
   dotnet run --project src/WindSensorApp.UI/WindSensorApp.UI.csproj
   ```

### Configuration

All settings are managed through INI files in the `config/` directory:

- **sensors.ini** - Sensor connection settings (TCP/COM)
- **calibration.ini** - Calibration matrix and coefficients
- **ui.ini** - UI layout and window settings
- **data-display.ini** - Data display thresholds and colors
- **transmission.ini** - Data transmission settings
- **archive.ini** - Data storage and archiving settings

### Usage

1. **Start monitoring** - Click "Start" button or auto-start from settings
2. **View real-time data** - Monitor current readings from both sensors
3. **Pause monitoring** - Temporarily pause data collection
4. **Edit calibration** - Access calibration matrix in dedicated tab
5. **Export data** - Export CSV archives to SQLite database
6. **Configure settings** - Use Settings window or edit INI files directly

### Data Formats

#### CSV Archive
```csv
Time,Lufft_Speed,Lufft_Direction,Boeder_Speed_Primary,Boeder_Speed_Corrected,Boeder_Direction,Calibration_a,Calibration_b,10min_Max,10min_Min,10min_Avg,3hour_Max,3hour_Min,3hour_Avg
2026-05-18 14:30:00,2.9,85.0,2.6,2.9,85.0,0.95,0.05,5.7,1.2,3.1,6.2,0.8,3.5
```

#### Transmission (JSON)
```json
{
  "timestamp": "2026-05-18T14:30:00Z",
  "lufft": {"speed": 2.9, "direction": 85.0},
  "boeder": {"speed_primary": 2.6, "speed_corrected": 2.9, "direction": 85.0},
  "calibration": {"a": 0.95, "b": 0.05, "r2": 0.98}
}
```

### Troubleshooting

**Connection Issues**
- Verify sensor IP addresses in `config/sensors.ini`
- Check network connectivity
- Test connection via Settings window

**No Data Displayed**
- Check if monitoring is started (green indicator)
- Verify sensor intervals in configuration
- Check logs in `logs/` directory

**Calibration Not Applied**
- Ensure calibration matrix has sufficient data points (min 100 per coefficient)
- Verify calibration file is loaded: `config/calibration.ini`

### License

MIT License - See LICENSE file for details

### Support

For issues and feature requests, please create an Issue on GitHub.

### Authors

- engineerMet - Initial development

---

**Last Updated:** 2026-05-20
