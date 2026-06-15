using System;
using System.Windows;
using V200A_UMBandW16comparison.Config;
using V200A_UMBandW16comparison.Utils;

namespace V200A_UMBandW16comparison
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = new Logger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Initialize configuration
                ConfigManager.Initialize();
                Logger.Info("Application configuration loaded successfully.");

                // Initialize main window
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                Logger.Error($"Application startup error: {ex.Message}", ex);
                MessageBox.Show(
                    $"Critical error during application startup:\n\n{ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Logger.Info("Application shutting down gracefully.");
                ConfigManager.SaveState();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during application shutdown: {ex.Message}", ex);
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}
