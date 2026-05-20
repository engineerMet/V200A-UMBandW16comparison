# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-05-20

### Added

#### Core Functionality
- Lufft V200A-UMB sensor support (UMB Protocol over TCP/COM)
- Boeder W16 sensor support (Modbus RTU over TCP/COM)
- Real-time data acquisition and monitoring
- Dual protocol support: UMB and Modbus RTU

#### Data Processing
- 10-minute moving window aggregation (max, min, avg)
- 3-hour aggregation with fixed/moving window modes
- 24-hour aggregation with calendar/meteorological/moving modes
- Data age tracking and visualization
- Color-coded data quality indicators (fresh/old/critical)
- Blinking animation for stale data (>300 seconds)
- "- - -" placeholder for missing data (>600 seconds)

#### Calibration System
- 36×36 calibration matrix (wind direction sectors × speed ranges)
- 10° wind direction sectors (36 total: 0-9.9°, 10-19.9°, ...)
- 1 m/s speed ranges (36 total: 0-0.9, 1-1.9, ..., 35+)
- Linear regression coefficients (a, b)
- R² correlation validation
- RMSE error calculation
- Automatic outlier detection (>3σ standard deviations)
- Minimum 100 data points per coefficient for reliability

#### Storage & Archiving
- CSV archive format (primary)
- Automatic CSV to SQLite export (weekly)
- Configurable archive intervals
- Automatic backup support
- Data retention policies

#### Communication & Transmission
- TCP socket transmission
- COM port transmission
- Multiple message formats: JSON, CSV, TEXT
- Configurable transmission intervals (milliseconds)
- Data buffering and batch transmission
- Selective parameter transmission (enable/disable per sensor)

#### Notifications & Alerts
- Idle warning system
- Sound alerts (beep patterns: бип-бип-бип бип-бип-бип)
- Visual notifications (window flashing)
- System tray balloon tips
- Configurable warning intervals
- Status indicators (green/orange/red)

#### User Interface
- WPF application with MVVM pattern
- Responsive layout (supports 1280×800 minimum)
- Real-time monitoring dashboard
- Separate calibration matrix viewer
- Settings window with configuration editor
- INI file text editor integration
- Tab-based navigation
- Connection test functionality

#### Configuration Management
- INI-based configuration files
- Separate config files for each subsystem:
  - sensors.ini - Connection settings
  - calibration.ini - Calibration matrix
  - ui.ini - Window and layout settings
  - data-display.ini - Display thresholds and colors
  - transmission.ini - Data transmission settings
  - archive.ini - Storage and backup settings
- Configuration validation
- Live reload support

#### Logging & Diagnostics
- Serilog structured logging
- File-based log rotation
- Debug/Info/Warning/Error levels
- Connection state logging
- Data transmission logging
- Error reporting

#### Features
- Auto-start on application launch (configurable)
- Pause/Resume monitoring
- Graceful shutdown
- Memory-efficient data processing
- Thread-safe concurrent operations
- Error recovery with retries
- Data validation

### Technical
- .NET 6.0 LTS framework
- Async/await throughout
- Dependency Injection pattern
- Thread-safe collections
- Unit test framework
- CI/CD GitHub Actions
- Semantic versioning

### Documentation
- Comprehensive README.md
- Installation guide (INSTALL.md)
- User guide (USER_GUIDE.md)
- Architecture documentation (ARCHITECTURE.md)
- API documentation (API.md)
- This CHANGELOG

---

## Version Notes

### v1.0.0 - Initial Release
First stable version with complete MVP functionality:
- Both sensors working
- Calibration system operational
- All required UI components
- Comprehensive configuration system
- Data archiving and transmission
- Full test coverage for Core module
