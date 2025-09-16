using System.Text.Json;
using Microsoft.Win32;
using System.ComponentModel;
using tkkn2025.Settings.Models;
using tkkn2025.Helpers;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Manages game settings with default values, save/load functionality, and file operations.
    /// Also serves as ViewModel for UI data binding.
    /// </summary>
    public class SettingsManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public SettingsManager()
        {
            // Initialize with default values on construction
            InitializeWithDefaults();
        }
       
        public GameSettings GameSettings { get; set; } = new GameSettings();

        public void ResetToDefaults()
        {
            GameSettings.ResetToDefaults();
            OnPropertyChanged();
        }

        public GameConfig ToGameConfig()
        {
            return GameSettings.ToGameConfig();
        }
       
        public void FromGameConfig(GameConfig config)
        {
            GameSettings.LoadFromConfig(config);
            OnPropertyChanged();
        }

        /// <summary>
        /// Saves game settings using the dialog and ConfigManager
        /// </summary>
        /// <param name="settings">The game settings to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveSettings(GameConfig settings)
        {
            try
            {
                ConfigManager.EnsureGameSettingsDirectoryExists();

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Game Settings",
                    Filter = "Game Settings (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = ConfigManager.GetGameSettingsDirectory(),
                    FileName = $"GameSettings_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
                };

                DebugHelper.WriteLine($"Opening save dialog - Default directory: {ConfigManager.GetGameSettingsDirectory()}");

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Update metadata before saving
                    settings.LastModified = DateTime.Now;
                    settings.CreatedBy = Session.PlayerName;
                    
                    // Use ConfigManager to save to the GameSettings directory
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                    DebugHelper.WriteLine($"User selected save file: {saveFileDialog.FileName}");
                    
                    bool result = ConfigManager.SaveGameConfigToSettings(settings, fileName);
                    if (result)
                    {
                        DebugHelper.WriteLine($"Settings save completed successfully for config: '{settings.ConfigName}'");
                    }
                    else
                    {
                        DebugHelper.WriteLine($"Settings save failed for config: '{settings.ConfigName}'");
                    }
                    return result;
                }

                DebugHelper.WriteLine("Save dialog was canceled by user");
                return false;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Error in SaveSettings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads settings using the dialog and updates the current GameSettings
        /// </summary>
        /// <returns>The loaded GameConfig or null if failed/canceled</returns>
        public GameConfig? LoadSettings()
        {
            try
            {
                var loadedSettings = LoadSettingsWithDialog();
                if (loadedSettings != null)
                {
                    DebugHelper.WriteLine($"Applying loaded settings: '{loadedSettings.ConfigName}' to current GameSettings");
                    FromGameConfig(loadedSettings);
                    DebugHelper.WriteLine("Settings applied successfully to UI");
                }
                else
                {
                    DebugHelper.WriteLine("No settings were loaded (dialog canceled or load failed)");
                }
                return loadedSettings;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Error in LoadSettings: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shows a dialog to load settings from the GameSettings directory
        /// </summary>
        /// <returns>The loaded GameConfig or null if failed/canceled</returns>
        public GameConfig? LoadSettingsWithDialog()
        {
            try
            {
                ConfigManager.EnsureGameSettingsDirectoryExists();

                var openFileDialog = new OpenFileDialog
                {
                    Title = "Load Game Settings",
                    Filter = "Game Settings (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = ConfigManager.GetGameSettingsDirectory(),
                    Multiselect = false
                };

                DebugHelper.WriteLine($"Opening load dialog - Default directory: {ConfigManager.GetGameSettingsDirectory()}");

                if (openFileDialog.ShowDialog() == true)
                {
                    DebugHelper.WriteLine($"User selected load file: {openFileDialog.FileName}");
                    var config = ConfigManager.LoadGameConfigFromSettings(openFileDialog.FileName);
                    
                    if (config != null)
                    {
                        DebugHelper.WriteLine($"Successfully loaded config: '{config.ConfigName}' from dialog");
                    }
                    else
                    {
                        DebugHelper.WriteLine("Failed to load config from selected file");
                    }
                    
                    return config;
                }

                DebugHelper.WriteLine("Load dialog was canceled by user");
                return null;
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Error in LoadSettingsWithDialog: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the default game configuration
        /// </summary>
        /// <returns>Default GameConfig</returns>
        public GameConfig GetDefaultSettings()
        {
            return ConfigManager.CreateDefaultGameConfig();
        }

        /// <summary>
        /// Validates and clamps settings values to acceptable ranges
        /// </summary>
        /// <param name="settings">The settings to validate</param>
        /// <returns>Validated settings</returns>
        public GameConfig ValidateSavedOrLoadedSettings(GameConfig settings)
        {
            // Create a new config with validated values
            var validated = settings.CreateCopy();
            
            // Validate basic settings
            validated.ShipSpeed = Math.Max(50, Math.Min(300, validated.ShipSpeed));
            validated.ParticleSpeed = Math.Max(25, Math.Min(300, validated.ParticleSpeed));
            validated.ParticleTurnSpeed = Math.Max(0.1, Math.Min(10, validated.ParticleTurnSpeed));
            validated.StartingParticles = Math.Max(1, Math.Min(100, validated.StartingParticles));
            validated.LevelDuration = Math.Max(1, Math.Min(60, validated.LevelDuration));
            validated.NewParticlesPerLevel = Math.Max(1, Math.Min(50, validated.NewParticlesPerLevel));
            validated.ParticleSpeedVariance = Math.Max(0, Math.Min(100, validated.ParticleSpeedVariance));
            validated.ParticleRandomizerPercentage = Math.Max(0, Math.Min(100, validated.ParticleRandomizerPercentage));
            
            // Validate power-up settings
            validated.PowerUpSpawnRate = Math.Max(1, Math.Min(60, validated.PowerUpSpawnRate));
            validated.PowerUpDuration_TimeWarp = Math.Max(1, Math.Min(30, validated.PowerUpDuration_TimeWarp));
            validated.PowerUpDuration_Repulsor = Math.Max(1, Math.Min(15, validated.PowerUpDuration_Repulsor));
            validated.PowerUpForce_Repulsor = Math.Max(50, Math.Min(500, validated.PowerUpForce_Repulsor));
            validated.PowerUpDuration_Singularity = Math.Max(1, Math.Min(15, validated.PowerUpDuration_Singularity));
            validated.PowerUpForce_Singulaiorty = Math.Max(50, Math.Min(300, validated.PowerUpForce_Singulaiorty));
            
            return validated;
        }

        /// <summary>
        /// Gets all saved settings files in the GameSettings directory
        /// </summary>
        /// <returns>Array of file paths to saved settings</returns>
        public static string[] GetSavedSettingsFiles()
        {
            return ConfigManager.GetSavedGameConfigs();
        }

        /// <summary>
        /// Gets the GameSettings directory path
        /// </summary>
        /// <returns>Full path to the GameSettings directory</returns>
        public string GetSavedSettingsDirectory()
        {
            return ConfigManager.GetGameSettingsDirectory();
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Initialize the GameSettings with default values from a default GameConfig
        /// </summary>
        public void InitializeWithDefaults()
        {
            var defaultConfig = ConfigManager.CreateDefaultGameConfig();
            GameSettings.LoadFromConfig(defaultConfig);
            OnPropertyChanged();
        }
    }
}