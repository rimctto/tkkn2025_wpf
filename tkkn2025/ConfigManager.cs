using System.Text.Json;
using tkkn2025.Settings;
using tkkn2025.Settings.Models;
using static tkkn2025.Helpers.DebugHelper;

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

        // Game mode setting
        public GameMode GameMode { get; set; }

        // Game settings (removed MusicEnabled - now in AppConfig)
        public double ShipSpeed { get; set; }
        public double ShipBoost { get; set; }
        public double ShipSuperBoost { get; set; }
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
        public bool IsPowerUpEnabled_TimeWarp { get; set; }
        public bool IsPowerUpEnabled_Singularity { get; set; }
        public bool IsPowerUpEnabled_Repulsor { get; set; }
        public double PowerUpDuration_TimeWarp { get; set; }
        public double PowerUpDuration_Repulsor { get; set; }
        public double PowerUpForce_Repulsor { get; set; }
        public double PowerUpDuration_Singularity { get; set; }
        public double PowerUpForce_Singulaiorty { get; set; }

        // Level Mechanics settings
        public bool LevelMechanicsEnabled { get; set; }
        public double InitialLevelSpeed { get; set; }
        public double SpeedIncreasePerLevel { get; set; }
        
       

        /// <summary>
        /// Creates a deep copy of this GameConfig for game instances
        /// </summary>
        public GameConfig CreateCopy()
        {
            return new GameConfig
            {
                // Metadata
                ConfigName = this.ConfigName,
                Description = this.Description,
                CreatedBy = this.CreatedBy,
                DateCreated = this.DateCreated,
                LastModified = this.LastModified,
                Version = this.Version,

                // Game mode
                GameMode = this.GameMode,

                // Basic Settings
                ShipSpeed = this.ShipSpeed,
                ShipBoost = this.ShipBoost,
                ShipSuperBoost = this.ShipSuperBoost,
                ParticleSpeed = this.ParticleSpeed,
                ParticleTurnSpeed = this.ParticleTurnSpeed,
                StartingParticles = this.StartingParticles,
                LevelDuration = this.LevelDuration,

                // Particle Settings
                NewParticlesPerLevel = this.NewParticlesPerLevel,
                ParticleSpeedVariance = this.ParticleSpeedVariance,
                ParticleRandomizerPercentage = this.ParticleRandomizerPercentage,
                IsParticleSpawnVectorTowardsShip = this.IsParticleSpawnVectorTowardsShip,
                IsParticleChaseShip = this.IsParticleChaseShip,

                // PowerUp Settings
                PowerUpSpawnRate = this.PowerUpSpawnRate,
                IsPowerUpEnabled_TimeWarp = this.IsPowerUpEnabled_TimeWarp,
                IsPowerUpEnabled_Singularity = this.IsPowerUpEnabled_Singularity,
                IsPowerUpEnabled_Repulsor = this.IsPowerUpEnabled_Repulsor,
                PowerUpDuration_TimeWarp = this.PowerUpDuration_TimeWarp,
                PowerUpDuration_Repulsor = this.PowerUpDuration_Repulsor,
                PowerUpForce_Repulsor = this.PowerUpForce_Repulsor,
                PowerUpDuration_Singularity = this.PowerUpDuration_Singularity,
                PowerUpForce_Singulaiorty = this.PowerUpForce_Singulaiorty,

                // Level Mechanics Settings
                LevelMechanicsEnabled = this.LevelMechanicsEnabled,
                InitialLevelSpeed = this.InitialLevelSpeed,
                SpeedIncreasePerLevel = this.SpeedIncreasePerLevel,
                
              
            };
        }
    }

    /// <summary>
    /// Application configuration class for storing application-level settings like player name and music preferences
    /// </summary>
    public class AppConfig
    {
        public string PlayerName { get; set; } = "Anonymous";
        public string Version { get; set; } = "1.0";
        
        // Music player properties
        public bool RepeatTrack { get; set; } = false;
        public string DefaultTrack { get; set; } = "";
        public bool IsPlaying { get; set; } = false;
    }
    
    /// <summary>
    /// Static class for managing configuration files and settings
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string AppDataDirectory = System.IO.Path.Combine(AppDataPath, "tkkn2025"); 
        
        private static readonly string DefaultConfigFileName = "gameconfig_default.json";
        private static readonly string AppConfigFileName = "appconfig.json";
        private static readonly string GameSettingsDirectory = "GameSettings";
                
        private static readonly string DefaultConfigFilePath = System.IO.Path.Combine(AppDataDirectory, DefaultConfigFileName);
        private static readonly string AppConfigFilePath = System.IO.Path.Combine(AppDataDirectory, AppConfigFileName);
        private static readonly string GameSettingsDirectoryPath = System.IO.Path.Combine(AppDataDirectory, GameSettingsDirectory);


        #region Default Game Configuration

        /// <summary>
        /// Uses the actual default values from the GameSettings class definitions
        /// </summary>
        public static GameConfig CreateDefaultGameConfig()
        {
            var defaultConfig = new GameConfig
            {
                ConfigName = "Default Configuration",
                Description = "Default game settings",
                CreatedBy = "System",
                DateCreated = DateTime.Now,
                LastModified = DateTime.Now,
                Version = "2.0",

                // Game mode
                GameMode = GameSettings.GameMode.DefaultValue,

                // Game settings with default values from GameSettings_Basic.cs
                ShipSpeed = GameSettings.ShipSpeed.DefaultValue,
                ShipBoost = GameSettings.ShipBoost.DefaultValue,
                ShipSuperBoost = GameSettings.ShipSuperBoost.DefaultValue,
                StartingParticles = GameSettings.StartingParticles.DefaultValue,
                LevelDuration = GameSettings.LevelDuration.DefaultValue,
                NewParticlesPerLevel = GameSettings.NewParticlesPerLevel.DefaultValue,

                // Particle settings with default values from GameSettings_Particles.cs
                ParticleSpeed = GameSettings.ParticleSpeed.DefaultValue,
                ParticleTurnSpeed = GameSettings.ParticleTurnSpeed.DefaultValue,
                ParticleSpeedVariance = GameSettings.ParticleSpeedVariance.DefaultValue,
                ParticleRandomizerPercentage = GameSettings.ParticleRandomizerPercentage.DefaultValue,
                IsParticleSpawnVectorTowardsShip = GameSettings.IsParticleSpawnVectorTowardsShip.DefaultValue,
                IsParticleChaseShip = GameSettings.IsParticleChaseShip.DefaultValue,

                // PowerUp settings with default values from GameSettings_PowerUps.cs
                PowerUpSpawnRate = GameSettings.PowerUpSpawnRate.DefaultValue,
                IsPowerUpEnabled_TimeWarp = GameSettings.IsPowerUpEnabled_TimeWarp.DefaultValue,
                IsPowerUpEnabled_Singularity = GameSettings.IsPowerUpEnabled_Singularity.DefaultValue,
                IsPowerUpEnabled_Repulsor = GameSettings.IsPowerUpEnabled_Repulsor.DefaultValue,
                PowerUpDuration_TimeWarp = GameSettings.PowerUpDuration_TimeWarp.DefaultValue,
                PowerUpDuration_Repulsor = GameSettings.PowerUpDuration_Repulsor.DefaultValue,
                PowerUpForce_Repulsor = GameSettings.PowerUpForce_Repulsor.DefaultValue,
                PowerUpDuration_Singularity = GameSettings.PowerUpDuration_Singularity.DefaultValue,
                PowerUpForce_Singulaiorty = GameSettings.PowerUpForce_Singularity.DefaultValue,

                // Level Mechanics settings
                LevelMechanicsEnabled = GameSettings.LevelMechanicsEnabled.DefaultValue,
                InitialLevelSpeed = GameSettings.InitialLevelSpeed.DefaultValue,
                SpeedIncreasePerLevel = GameSettings.SpeedIncreasePerLevel.DefaultValue,


            };
            
               
            return defaultConfig;
        }

        /// <summary>
        /// Creates a game configuration for Survival mode
        /// Uses all default settings except LevelMechanicsEnabled = false
        /// </summary>
        public static GameConfig CreateSurvivalModeConfig()
        {
            var survivalConfig = CreateDefaultGameConfig();
            
            survivalConfig.ConfigName = "Survival Mode";
            survivalConfig.Description = "Survival mode with level mechanics disabled";
            survivalConfig.GameMode = Settings.Models.GameMode.Survival;
            
            // Survival mode specific settings
            survivalConfig.LevelMechanicsEnabled = false;
            
            return survivalConfig;
        }

        /// <summary>
        /// Creates a game configuration for Maze mode
        /// NewParticlesPerLevel = 0, StartingParticles = 0, all power-ups disabled
        /// </summary>
        public static GameConfig CreateMazeModeConfig()
        {
            var mazeConfig = CreateDefaultGameConfig();
            
            mazeConfig.ConfigName = "Maze Mode";
            mazeConfig.Description = "Maze mode with no particles and no power-ups";
            mazeConfig.GameMode = Settings.Models.GameMode.Maze;
            
            // Maze mode specific settings
            mazeConfig.NewParticlesPerLevel = 0;
            mazeConfig.StartingParticles = 0;
            mazeConfig.IsPowerUpEnabled_TimeWarp = false;
            mazeConfig.IsPowerUpEnabled_Repulsor = false;
            mazeConfig.IsPowerUpEnabled_Singularity = false;
            mazeConfig.LevelMechanicsEnabled = true;
            
            return mazeConfig;
        }

        /// <summary>
        /// Creates a game configuration based on the specified game mode
        /// </summary>
        /// <param name="gameMode">The game mode to create configuration for</param>
        /// <returns>GameConfig configured for the specified mode</returns>
        public static GameConfig CreateConfigForGameMode(Settings.Models.GameMode gameMode)
        {
            return gameMode switch
            {
                Settings.Models.GameMode.Standard => CreateDefaultGameConfig(),
                Settings.Models.GameMode.Survival => CreateSurvivalModeConfig(),
                Settings.Models.GameMode.Maze => CreateMazeModeConfig(),
                _ => CreateDefaultGameConfig()
            };
        }

        #endregion

        #region Default Configuration Management (Auto-persist current settings)

       
        public static bool SaveDefaultConfig(GameConfig config)
        {
            try
            {
                // Ensure the AppData directory exists
                if (!System.IO.Directory.Exists(AppDataDirectory))
                {
                    System.IO.Directory.CreateDirectory(AppDataDirectory);
                    WriteLine($"Created AppData directory: {AppDataDirectory}");
                }
                
                config.LastModified = DateTime.Now;
                config.Version = "2.0";
                config.ConfigName = "Current Default Settings";
                config.Description = "Automatically saved current game settings";

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(DefaultConfigFilePath, jsonString);
                
                WriteLine($"Default game config saved to: {DefaultConfigFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to save default config to {DefaultConfigFilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the default game configuration that auto-loads on app start
        /// Returns system defaults if file doesn't exist
        /// </summary>
        /// <returns>Loaded default configuration or system default configuration</returns>
        public static GameConfig LoadDefaultConfig()
        {
            try
            {
                if (!System.IO.File.Exists(DefaultConfigFilePath))
                {
                    WriteLine($"Default config file not found at {DefaultConfigFilePath}, creating system default config");
                    var defaultConfig = CreateDefaultGameConfig();
                    // Save the system defaults as the new default config
                    SaveDefaultConfig(defaultConfig);
                    return defaultConfig;
                }

                string jsonString = System.IO.File.ReadAllText(DefaultConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<GameConfig>(jsonString, options);
                var result = config ?? CreateDefaultGameConfig();
                
                // Migrate old config versions if needed
                result = MigrateGameConfig(result);
                
                WriteLine($"Default game config '{result.ConfigName}' loaded successfully from: {DefaultConfigFilePath}");
                return result;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to load default config from {DefaultConfigFilePath}: {ex.Message}");
                var defaultConfig = CreateDefaultGameConfig();
                // Try to save the system defaults as backup
                SaveDefaultConfig(defaultConfig);
                return defaultConfig;
            }
        }

        /// <summary>
        /// Checks if the default configuration file exists
        /// </summary>
        /// <returns>True if default config file exists, false otherwise</returns>
        public static bool DefaultConfigFileExists()
        {
            return System.IO.File.Exists(DefaultConfigFilePath);
        }

        /// <summary>
        /// Gets the full path to the default configuration file
        /// </summary>
        /// <returns>Full path to the default config file</returns>
        public static string GetDefaultConfigFilePath()
        {
            return DefaultConfigFilePath;
        }

        #endregion

        #region Game Configuration Management (Manual Save/Load to GameSettings folder)

        /// <summary>
        /// Saves a game configuration to the GameSettings directory with metadata
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <param name="fileName">Optional custom filename (without extension)</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveGameConfig(GameConfig config, string? fileName = null)
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
                
                WriteLine($"Game config '{config.ConfigName}' saved to: {filePath}");
                WriteLine($"Config details: Created by {config.CreatedBy}, Version {config.Version}");
                return true;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to save config '{config.ConfigName}' to GameSettings directory: {ex.Message}");
                WriteLine($"Target directory: {GameSettingsDirectoryPath}");
                return false;
            }
        }

        /// <summary>
        /// Loads a game configuration from the GameSettings directory
        /// </summary>
        /// <param name="filePath">Full path to the configuration file</param>
        /// <returns>Loaded configuration or null if failed</returns>
        public static GameConfig? LoadGameConfig(string filePath)
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    WriteLine($"Game config file not found at: {filePath}");
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
                    WriteLine($"Game config '{config.ConfigName}' loaded from: {filePath}");
                    WriteLine($"Config details: Created by {config.CreatedBy}, Version {config.Version}, Last modified: {config.LastModified}");
                }
                
                return config;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to load config from: {filePath}");
                WriteLine($"Error: {ex.Message}");
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
                var files = System.IO.Directory.GetFiles(GameSettingsDirectoryPath, "*.json");
                WriteLine($"Found {files.Length} saved game config files in: {GameSettingsDirectoryPath}");
                return files;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to get saved game configs from {GameSettingsDirectoryPath}: {ex.Message}");
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
                // Ensure the AppData directory exists
                if (!System.IO.Directory.Exists(AppDataDirectory))
                {
                    System.IO.Directory.CreateDirectory(AppDataDirectory);
                    WriteLine($"Created AppData directory: {AppDataDirectory}");
                }
                
                config.Version = "1.0";
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(AppConfigFilePath, jsonString);
                
                WriteLine($"App config saved to: {AppConfigFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to save app config to {AppConfigFilePath}: {ex.Message}");
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
                    WriteLine($"App config file not found at {AppConfigFilePath}, returning default config");
                    return new AppConfig();
                }

                string jsonString = System.IO.File.ReadAllText(AppConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<AppConfig>(jsonString, options);
                var result = config ?? new AppConfig();
                
                WriteLine($"App config loaded from: {AppConfigFilePath}");
                WriteLine($"Player name: '{result.PlayerName}', Repeat: {result.RepeatTrack}, Default track: '{result.DefaultTrack}', Is playing: {result.IsPlaying}");
                return result;
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to load app config from {AppConfigFilePath}: {ex.Message}");
                return new AppConfig();
            }
        }

        #endregion

        #region File Path and Status Methods

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
                // First ensure the main AppData directory exists
                if (!System.IO.Directory.Exists(AppDataDirectory))
                {
                    System.IO.Directory.CreateDirectory(AppDataDirectory);
                    WriteLine($"Created AppData directory: {AppDataDirectory}");
                }
                
                // Then ensure the GameSettings subdirectory exists
                if (!System.IO.Directory.Exists(GameSettingsDirectoryPath))
                {
                    System.IO.Directory.CreateDirectory(GameSettingsDirectoryPath);
                    WriteLine($"Created GameSettings directory: {GameSettingsDirectoryPath}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to create GameSettings directory {GameSettingsDirectoryPath}: {ex.Message}");
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
        /// Ensures the configuration is compatible with the current version
        /// </summary>
        /// <param name="config">The configuration to validate</param>
        /// <returns>The validated configuration</returns>
        private static GameConfig MigrateGameConfig(GameConfig config)
        {
            // Assume all configs are latest version - no migration needed
            // Just ensure version is set correctly
            if (string.IsNullOrEmpty(config.Version))
            {
                config.Version = "2.0";
                config.LastModified = DateTime.Now;
                WriteLine($"Updated version for config '{config.ConfigName}' to v2.0");
            }
            
            return config;
        }

        #endregion
    }
}