using System.Text.Json;
using Microsoft.Win32;
using System.ComponentModel;
using tkkn2025.Settings.Models;

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
                
        }
       
        public GameSettings GameSettings { get; set; } = new GameSettings();


        private static readonly string SavedSettingsDirectory = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "Saved", "GameSettings");


        public void ResetToDefaults()
        {
            GameSettings.ResetToDefaults();
        }

      
        public GameConfig ToGameConfig()
        {
            return GameSettings.ToGameConfig();
        }

       
        public void FromGameConfig(GameConfig config)
        {
            GameSettings.LoadFromConfig(config);
        }


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

   
        public static bool SaveSettings(GameConfig settings)
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
                    return WriteSettingsToFile(settings, saveFileDialog.FileName);
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveSettingsWithDialog: {ex.Message}");
                return false;
            }
        }

        private static bool WriteSettingsToFile(GameConfig settings, string filePath)
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


       
        public GameConfig? LoadSettings()
        {
            try
            {
                var loadedSettings = LoadSettingsWithDialog();
                if (loadedSettings != null)
                {
                    FromGameConfig(loadedSettings);
                }
                return loadedSettings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadSettingsWithDialog: {ex.Message}");
                return null;
            }
        }

       
        public GameConfig? LoadSettingsWithDialog()
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
                    return ReadConfigFromFile(openFileDialog.FileName);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadSettingsWithDialogStatic: {ex.Message}");
                return null;
            }
        }

      

        public GameConfig? ReadConfigFromFile(string filePath)
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
        public string GetSavedSettingsDirectory()
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

        

      
        public GameConfig GetDefaultSettings()
        {
            return new GameConfig
            {
                ShipSpeed = GameSettings.ShipSpeed.DefaultValue,
                ParticleSpeed = GameSettings.ParticleSpeed.DefaultValue,
                ParticleTurnSpeed = GameSettings.ParticleTurnSpeed.DefaultValue,
                StartingParticles = GameSettings.StartingParticles.DefaultValue,
                LevelDuration = GameSettings.LevelDuration.DefaultValue,
                NewParticlesPerLevel = GameSettings.NewParticlesPerLevel.DefaultValue,
                ParticleSpeedVariance = GameSettings.ParticleSpeedVariance.DefaultValue,
                ParticleRandomizerPercentage = GameSettings.ParticleRandomizerPercentage.DefaultValue,
                IsParticleSpawnVectorTowardsShip = GameSettings.IsParticleChaseShip.DefaultValue,
                MusicEnabled = GameSettings.MusicEnabled.DefaultValue
            };
        }

      
        public GameConfig ValidateSavedOrLoadedSettings(GameConfig settings)
        {
            return new GameConfig
            {
                ShipSpeed = Math.Max(GameSettings.ShipSpeed.Min, Math.Min(GameSettings.ShipSpeed.Max, settings.ShipSpeed)),
                ParticleSpeed = Math.Max(25, Math.Min(300, settings.ParticleSpeed)),
                ParticleTurnSpeed = Math.Max(0.1, Math.Min(10, settings.ParticleTurnSpeed)),
                StartingParticles = Math.Max(1, Math.Min(100, settings.StartingParticles)),
                LevelDuration = Math.Max(1, Math.Min(20, settings.LevelDuration)),
                NewParticlesPerLevel = Math.Max(1, Math.Min(50, settings.NewParticlesPerLevel)),
                ParticleSpeedVariance = Math.Max(0, Math.Min(100, settings.ParticleSpeedVariance)),
                ParticleRandomizerPercentage = Math.Max(0, Math.Min(100, settings.ParticleRandomizerPercentage)),
                IsParticleSpawnVectorTowardsShip = settings.IsParticleSpawnVectorTowardsShip,
                MusicEnabled = settings.MusicEnabled // Boolean doesn't need validation
            };
        }
    }
}