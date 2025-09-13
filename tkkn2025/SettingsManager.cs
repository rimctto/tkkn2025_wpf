using System.Text.Json;
using Microsoft.Win32;

namespace tkkn2025
{
    /// <summary>
    /// Manages game settings with default values, save/load functionality, and file operations
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SavedSettingsDirectory = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "Saved", "GameSettings");

        /// <summary>
        /// Default game settings that can be restored at any time
        /// </summary>
        public static readonly GameConfig DefaultSettings = new GameConfig
        {
            ShipSpeed = 200.0,
            ParticleSpeed = 175.0,
            StartingParticles = 30,
            GenerationRate = 5.0,
            IncreaseRate = 15.0,
            ParticleSpeedVariance = 15.0,
            ParticleRandomizerPercentage = 30.0,
            ParticleChase_Initial = true
        };

        /// <summary>
        /// Gets a copy of the default settings
        /// </summary>
        /// <returns>A new GameConfig instance with default values</returns>
        public static GameConfig GetDefaultSettings()
        {
            return new GameConfig
            {
                ShipSpeed = DefaultSettings.ShipSpeed,
                ParticleSpeed = DefaultSettings.ParticleSpeed,
                StartingParticles = DefaultSettings.StartingParticles,
                GenerationRate = DefaultSettings.GenerationRate,
                IncreaseRate = DefaultSettings.IncreaseRate,
                ParticleSpeedVariance = DefaultSettings.ParticleSpeedVariance,
                ParticleRandomizerPercentage = DefaultSettings.ParticleRandomizerPercentage,
                ParticleChase_Initial = DefaultSettings.ParticleChase_Initial
            };
        }

        /// <summary>
        /// Ensures the saved settings directory exists
        /// </summary>
        private static void EnsureDirectoryExists()
        {
            try
            {
                if (!System.IO.Directory.Exists(SavedSettingsDirectory))
                {
                    System.IO.Directory.CreateDirectory(SavedSettingsDirectory);
                    System.Diagnostics.Debug.WriteLine($"Created settings directory: {SavedSettingsDirectory}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create settings directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves settings to a specific file with a file picker dialog
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        public static bool SaveSettingsWithDialog(GameConfig settings)
        {
            try
            {
                EnsureDirectoryExists();

                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save Game Settings",
                    Filter = "Game Settings (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = SavedSettingsDirectory,
                    FileName = $"GameSettings_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    return SaveSettingsToFile(settings, saveFileDialog.FileName);
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveSettingsWithDialog: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads settings from a specific file with a file picker dialog
        /// </summary>
        /// <returns>Loaded settings or null if cancelled/failed</returns>
        public static GameConfig? LoadSettingsWithDialog()
        {
            try
            {
                EnsureDirectoryExists();

                var openFileDialog = new OpenFileDialog
                {
                    Title = "Load Game Settings",
                    Filter = "Game Settings (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    InitialDirectory = SavedSettingsDirectory,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    return LoadSettingsFromFile(openFileDialog.FileName);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadSettingsWithDialog: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves settings to a specific file path
        /// /// <param name="settings">Settings to save</param>
        /// <param name="filePath">Full path to save the file</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        public static bool SaveSettingsToFile(GameConfig settings, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Create a settings object with metadata
                var settingsWithMetadata = new
                {
                    SavedAt = DateTime.Now,
                    Version = "1.0",
                    Settings = settings
                };

                string jsonString = JsonSerializer.Serialize(settingsWithMetadata, options);
                System.IO.File.WriteAllText(filePath, jsonString);

                System.Diagnostics.Debug.WriteLine($"Settings saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings to {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads settings from a specific file path
        /// </summary>
        /// <param name="filePath">Full path to the settings file</param>
        /// <returns>Loaded settings or null if failed</returns>
        public static GameConfig? LoadSettingsFromFile(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Settings file not found: {filePath}");
                    return null;
                }

                string jsonString = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Try to load with metadata first
                try
                {
                    var settingsWithMetadata = JsonSerializer.Deserialize<dynamic>(jsonString, options);
                    var settingsJson = JsonSerializer.Serialize(((JsonElement)settingsWithMetadata).GetProperty("settings"));
                    var settings = JsonSerializer.Deserialize<GameConfig>(settingsJson, options);
                    
                    System.Diagnostics.Debug.WriteLine($"Settings loaded from: {filePath}");
                    return settings ?? GetDefaultSettings();
                }
                catch
                {
                    // Fallback: try to load as direct GameConfig
                    var settings = JsonSerializer.Deserialize<GameConfig>(jsonString, options);
                    System.Diagnostics.Debug.WriteLine($"Settings loaded (legacy format) from: {filePath}");
                    return settings ?? GetDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the saved settings directory path
        /// /// <returns>Full path to the saved settings directory</returns>
        public static string GetSavedSettingsDirectory()
        {
            return SavedSettingsDirectory;
        }

        /// <summary>
        /// Gets all saved settings files in the default directory
        /// </summary>
        /// <returns>Array of file paths to saved settings</returns>
        public static string[] GetSavedSettingsFiles()
        {
            try
            {
                EnsureDirectoryExists();
                return System.IO.Directory.GetFiles(SavedSettingsDirectory, "*.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get saved settings files: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// Validates that settings are within acceptable ranges
        /// </summary>
        /// <param name="settings">Settings to validate</param>
        /// <returns>Validated and clamped settings</returns>
        public static GameConfig ValidateSettings(GameConfig settings)
        {
            return new GameConfig
            {
                ShipSpeed = Math.Max(50, Math.Min(500, settings.ShipSpeed)),
                ParticleSpeed = Math.Max(25, Math.Min(300, settings.ParticleSpeed)),
                StartingParticles = Math.Max(1, Math.Min(100, settings.StartingParticles)),
                GenerationRate = Math.Max(1, Math.Min(20, settings.GenerationRate)),
                IncreaseRate = Math.Max(1, Math.Min(50, settings.IncreaseRate)),
                ParticleSpeedVariance = Math.Max(0, Math.Min(100, settings.ParticleSpeedVariance)),
                ParticleRandomizerPercentage = Math.Max(0, Math.Min(100, settings.ParticleRandomizerPercentage)),
                ParticleChase_Initial = settings.ParticleChase_Initial
            };
        }
    }
}