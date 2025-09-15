using System.Text.Json;

namespace tkkn2025
{
    /// <summary>
    /// Configuration class for storing and managing game settings with metadata
    /// </summary>
    public class GameConfig
    {
        // Metadata properties
        public string ConfigName { get; set; } = "Default Config";
        public string Description { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string Version { get; set; } = "2.0";

        // Game settings
        public bool MusicEnabled { get; set; }
        public double ShipSpeed { get; set; }
        public double ParticleSpeed { get; set; }
        public double ParticleTurnSpeed { get; set; }
        public int StartingParticles { get; set; }
        public double LevelDuration { get; set; }
        public double NewParticlesPerLevel { get; set; }
        public double ParticleSpeedVariance { get; set; }
        public double ParticleRandomizerPercentage { get; set; }
        public bool IsParticleSpawnVectorTowardsShip { get; set; }
        public bool IsParticleChaseShip { get; set; }

        // PowerUp settings
        public double PowerUpSpawnRate { get; set; }
        public double TimeWarpDuration { get; set; }
        public double RepulsorDuration { get; set; }
        public double RepulsorForce { get; set; }
        public double SingularityDuration { get; set; }
        public double SingulaiortyForce { get; set; }

        /// <summary>
        /// Creates a deep copy of this GameConfig for game instances
        /// </summary>
        public GameConfig CreateCopy()
        {
            return new GameConfig
            {
                ConfigName = this.ConfigName,
                Description = this.Description,
                CreatedBy = this.CreatedBy,
                DateCreated = this.DateCreated,
                LastModified = this.LastModified,
                Version = this.Version,
                MusicEnabled = this.MusicEnabled,
                ShipSpeed = this.ShipSpeed,
                ParticleSpeed = this.ParticleSpeed,
                ParticleTurnSpeed = this.ParticleTurnSpeed,
                StartingParticles = this.StartingParticles,
                LevelDuration = this.LevelDuration,
                NewParticlesPerLevel = this.NewParticlesPerLevel,
                ParticleSpeedVariance = this.ParticleSpeedVariance,
                ParticleRandomizerPercentage = this.ParticleRandomizerPercentage,
                IsParticleSpawnVectorTowardsShip = this.IsParticleSpawnVectorTowardsShip,
                IsParticleChaseShip = this.IsParticleChaseShip,
                PowerUpSpawnRate = this.PowerUpSpawnRate,
                TimeWarpDuration = this.TimeWarpDuration,
                RepulsorDuration = this.RepulsorDuration,
                RepulsorForce = this.RepulsorForce,
                SingularityDuration = this.SingularityDuration,
                SingulaiortyForce = this.SingulaiortyForce
            };
        }
    }

    /// <summary>
    /// Application configuration class for storing application-level settings like player name
    /// </summary>
    public class AppConfig
    {
        public string PlayerName { get; set; } = "Anonymous";
        public DateTime LastSaved { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Handles saving and loading all configuration types from JSON files with versioning and migration support
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "gameconfig.json";
        private static readonly string AppConfigFileName = "appconfig.json";
        private static readonly string GameSettingsDirectory = "GameSettings";
        
        private static readonly string ConfigFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            ConfigFileName);
        private static readonly string AppConfigFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            AppConfigFileName);
        private static readonly string GameSettingsDirectoryPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            GameSettingsDirectory);

        #region Default Game Configuration

        /// <summary>
        /// Creates a default game configuration with sensible values
        /// </summary>
        public static GameConfig CreateDefaultGameConfig()
        {
            return new GameConfig
            {
                ConfigName = "Default Configuration",
                Description = "Default game settings",
                CreatedBy = "System",
                DateCreated = DateTime.Now,
                LastModified = DateTime.Now,
                Version = "2.0",
                
                // Game settings with default values
                MusicEnabled = true,
                ShipSpeed = 150.0,
                ParticleSpeed = 75.0,
                ParticleTurnSpeed = 2.0,
                StartingParticles = 5,
                LevelDuration = 10.0,
                NewParticlesPerLevel = 3.0,
                ParticleSpeedVariance = 25.0,
                ParticleRandomizerPercentage = 15.0,
                IsParticleSpawnVectorTowardsShip = true,
                IsParticleChaseShip = false,
                
                // PowerUp settings with default values
                PowerUpSpawnRate = 15.0,
                TimeWarpDuration = 5.0,
                RepulsorDuration = 3.0,
                RepulsorForce = 200.0,
                SingularityDuration = 5.0,
                SingulaiortyForce = 150.0
            };
        }

        #endregion

        #region Game Configuration Management

        /// <summary>
        /// Saves the current game configuration to the default JSON file
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveConfig(GameConfig config)
        {
            try
            {
                config.LastModified = DateTime.Now;
                config.Version = "2.0";

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(ConfigFilePath, jsonString);
                
                System.Diagnostics.Debug.WriteLine($"Game config saved to: {ConfigFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads game configuration from the default JSON file, returns default if file doesn't exist
        /// </summary>
        /// <returns>Loaded configuration or default configuration</returns>
        public static GameConfig LoadConfig()
        {
            try
            {
                if (!System.IO.File.Exists(ConfigFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("Game config file not found, returning default config");
                    return CreateDefaultGameConfig();
                }

                string jsonString = System.IO.File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<GameConfig>(jsonString, options);
                var result = config ?? CreateDefaultGameConfig();
                
                // Migrate old config versions if needed
                result = MigrateGameConfig(result);
                
                System.Diagnostics.Debug.WriteLine($"Game config loaded successfully from: {ConfigFilePath}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
                return CreateDefaultGameConfig();
            }
        }

        /// <summary>
        /// Saves a game configuration to the GameSettings directory with metadata
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <param name="fileName">Optional custom filename (without extension)</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveGameConfigToSettings(GameConfig config, string? fileName = null)
        {
            try
            {
                EnsureGameSettingsDirectoryExistsInternal();
                
                config.LastModified = DateTime.Now;
                config.Version = "2.0";

                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"{config.ConfigName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                }
                
                // Sanitize filename
                fileName = SanitizeFileName(fileName);
                string filePath = System.IO.Path.Combine(GameSettingsDirectoryPath, $"{fileName}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(filePath, jsonString);
                
                System.Diagnostics.Debug.WriteLine($"Game config saved to settings: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config to settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a game configuration from the GameSettings directory
        /// </summary>
        /// <param name="filePath">Full path to the configuration file</param>
        /// <returns>Loaded configuration or null if failed</returns>
        public static GameConfig? LoadGameConfigFromSettings(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Game config file not found: {filePath}");
                    return null;
                }

                string jsonString = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<GameConfig>(jsonString, options);
                if (config != null)
                {
                    config = MigrateGameConfig(config);
                    System.Diagnostics.Debug.WriteLine($"Game config loaded from settings: {filePath}");
                }
                
                return config;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config from settings: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all saved game configuration files in the GameSettings directory
        /// </summary>
        /// <returns>Array of file paths to saved configurations</returns>
        public static string[] GetSavedGameConfigs()
        {
            try
            {
                EnsureGameSettingsDirectoryExistsInternal();
                return System.IO.Directory.GetFiles(GameSettingsDirectoryPath, "*.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get saved game configs: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        #endregion

        #region App Configuration Management

        /// <summary>
        /// Saves the application configuration (like player name) to a JSON file
        /// </summary>
        /// <param name="config">The app configuration to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveAppConfig(AppConfig config)
        {
            try
            {
                config.LastSaved = DateTime.Now;
                config.Version = "1.0";
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(AppConfigFilePath, jsonString);
                
                System.Diagnostics.Debug.WriteLine($"App config saved successfully. Player name: {config.PlayerName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save app config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads application configuration from JSON file, returns default if file doesn't exist
        /// </summary>
        /// <returns>Loaded app configuration or default configuration</returns>
        public static AppConfig LoadAppConfig()
        {
            try
            {
                if (!System.IO.File.Exists(AppConfigFilePath))
                {
                    System.Diagnostics.Debug.WriteLine("App config file not found, returning default config");
                    return new AppConfig();
                }

                string jsonString = System.IO.File.ReadAllText(AppConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<AppConfig>(jsonString, options);
                var result = config ?? new AppConfig();
                
                System.Diagnostics.Debug.WriteLine($"App config loaded successfully. Player name: {result.PlayerName}");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load app config: {ex.Message}");
                return new AppConfig();
            }
        }

        #endregion

        #region File Path and Status Methods

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        /// <returns>Full path to the config file</returns>
        public static string GetConfigFilePath()
        {
            return ConfigFilePath;
        }

        /// <summary>
        /// Gets the full path to the application configuration file
        /// </summary>
        /// <returns>Full path to the app config file</returns>
        public static string GetAppConfigFilePath()
        {
            return AppConfigFilePath;
        }

        /// <summary>
        /// Gets the full path to the GameSettings directory
        /// </summary>
        /// <returns>Full path to the GameSettings directory</returns>
        public static string GetGameSettingsDirectory()
        {
            return GameSettingsDirectoryPath;
        }

        /// <summary>
        /// Checks if the configuration file exists
        /// </summary>
        /// <returns>True if config file exists, false otherwise</returns>
        public static bool ConfigFileExists()
        {
            return System.IO.File.Exists(ConfigFilePath);
        }

        /// <summary>
        /// Checks if the application configuration file exists
        /// </summary>
        /// <returns>True if app config file exists, false otherwise</returns>
        public static bool AppConfigFileExists()
        {
            return System.IO.File.Exists(AppConfigFilePath);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Ensures the GameSettings directory exists
        /// </summary>
        private static void EnsureGameSettingsDirectoryExistsInternal()
        {
            try
            {
                if (!System.IO.Directory.Exists(GameSettingsDirectoryPath))
                {
                    System.IO.Directory.CreateDirectory(GameSettingsDirectoryPath);
                    System.Diagnostics.Debug.WriteLine($"Created GameSettings directory: {GameSettingsDirectoryPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create GameSettings directory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Public method to ensure GameSettings directory exists (used by SettingsManager)
        /// </summary>
        public static void EnsureGameSettingsDirectoryExists()
        {
            EnsureGameSettingsDirectoryExistsInternal();
        }

        /// <summary>
        /// Sanitizes a filename by removing invalid characters
        /// </summary>
        /// <param name="fileName">The filename to sanitize</param>
        /// <returns>Sanitized filename</returns>
        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            return fileName;
        }

        /// <summary>
        /// Migrates old config versions to the current version
        /// </summary>
        /// <param name="config">The configuration to migrate</param>
        /// <returns>Migrated configuration</returns>
        private static GameConfig MigrateGameConfig(GameConfig config)
        {
            // Handle version migration
            if (string.IsNullOrEmpty(config.Version) || config.Version == "1.0")
            {
                // Migrate from version 1.0 to 2.0
                config.Version = "2.0";
                config.LastModified = DateTime.Now;
                
                if (string.IsNullOrEmpty(config.ConfigName))
                {
                    config.ConfigName = "Migrated Configuration";
                }
                
                if (string.IsNullOrEmpty(config.CreatedBy))
                {
                    config.CreatedBy = "System Migration";
                }
                
                System.Diagnostics.Debug.WriteLine("GameConfig migrated from v1.0 to v2.0");
            }
            
            return config;
        }

        #endregion
    }
}