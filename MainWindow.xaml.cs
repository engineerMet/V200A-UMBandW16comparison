using System;
using System.Windows;
using V200A_UMBandW16comparison.Config;
using V200A_UMBandW16comparison.Models;
using V200A_UMBandW16comparison.Utils;

namespace V200A_UMBandW16comparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly Logger Logger = new Logger(typeof(MainWindow));
        private SensorDataService _sensorDataService;
        private CalibrationService _calibrationService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            LoadWindowState();
        }

        /// <summary>
        /// Initialize application services
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                _sensorDataService = new SensorDataService();
                _calibrationService = new CalibrationService();
                
                Logger.Info("Main window services initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error initializing services: {ex.Message}", ex);
                MessageBox.Show(
                    "Failed to initialize application services.",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Load saved window state (position, size)
        /// </summary>
        private void LoadWindowState()
        {
            try
            {
                var windowState = ConfigManager.GetWindowState();
                if (windowState != null)
                {
                    this.Left = windowState.Left;
                    this.Top = windowState.Top;
                    this.Width = windowState.Width;
                    this.Height = windowState.Height;
                    this.WindowState = (WindowState)Enum.Parse(typeof(WindowState), windowState.State);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not restore window state: {ex.Message}");
            }
        }

        /// <summary>
        /// Save window state on closing
        /// </summary>
        private void MainWindow_OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                ConfigManager.SaveWindowState(
                    this.Left,
                    this.Top,
                    this.Width,
                    this.Height,
                    this.WindowState.ToString());
                
                Logger.Info("Window state saved successfully.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving window state: {ex.Message}", ex);
            }
        }

        #region Event Handlers

        private void ButtonLoadData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Load data button clicked.");
                // TODO: Implement data loading logic
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading data: {ex.Message}", ex);
                ShowErrorMessage("Failed to load data.", ex);
            }
        }

        private void ButtonCalibrate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Calibrate button clicked.");
                // TODO: Implement calibration logic
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during calibration: {ex.Message}", ex);
                ShowErrorMessage("Calibration failed.", ex);
            }
        }

        private void ButtonExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Export button clicked.");
                // TODO: Implement export logic
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting data: {ex.Message}", ex);
                ShowErrorMessage("Failed to export data.", ex);
            }
        }

        #endregion

        /// <summary>
        /// Show error message to user
        /// </summary>
        private void ShowErrorMessage(string message, Exception ex = null)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\n\nDetails: {ex.Message}";
            }

            MessageBox.Show(
                fullMessage,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
