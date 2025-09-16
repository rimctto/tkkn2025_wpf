using System.Configuration;
using System.Data;
using System.Windows;

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Static reference to the current session for automatic config saving
        /// </summary>
        public static Session? CurrentSession { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Ensure configuration files are loaded at application startup
            InitializeConfigurations();
            
            System.Diagnostics.Debug.WriteLine("Application started - configurations initialized");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Save current configurations before the application exits
            SaveConfigurations();
            
            System.Diagnostics.Debug.WriteLine("Application exiting - configurations saved");
            
            base.OnExit(e);
        }

        /// <summary>
        /// Initialize configurations on application startup
        /// This ensures default configs exist and are loaded
        /// </summary>
        private void InitializeConfigurations()
        {
            try
            {
                // Load or create default configurations
                var defaultGameConfig = ConfigManager.LoadDefaultConfig();
                var appConfig = ConfigManager.LoadAppConfig();
                
                System.Diagnostics.Debug.WriteLine($"Configurations initialized - App config exists: {ConfigManager.AppConfigFileExists()}, Default game config exists: {ConfigManager.DefaultConfigFileExists()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing configurations: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current configurations on application exit
        /// This ensures the current working settings are persisted
        /// </summary>
        private void SaveConfigurations()
        {
            try
            {
                // If we have a current session, save its configurations
                if (CurrentSession != null)
                {
                    CurrentSession.SaveConfigurations();
                    System.Diagnostics.Debug.WriteLine("Current session configurations saved during app exit");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No current session to save during app exit");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configurations during app exit: {ex.Message}");
            }
        }
    }
}
