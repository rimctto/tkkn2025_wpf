using System.Text.Json;

namespace tkkn2025
{
    /// <summary>
    /// Configuration class for storing and managing game settings
    /// </summary>
    public class GameConfig
    {
        // Metadata properties
        public string ConfigName { get; set; } = "Default Config";
        public string Description { get; set; } = "";
        public DateTime DateCreated { get; set; } = DateTime.Now;

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
    }

    /// <summary>
    /// Application configuration class for storing application-level settings like player name
    /// </summary>
    public class AppConfig
    {
        public string PlayerName { get; set; } = "Anonymous";
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Handles saving and loading game configuration from JSON file
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "gameconfig.json";
        private static readonly string AppConfigFileName = "appconfig.json";
        private static readonly string ConfigFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            ConfigFileName);
        private static readonly string AppConfigFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            AppConfigFileName);

        /// <summary>
        /// Saves the current game configuration to a JSON file
        /// </summary>
        /// <param name="config">The configuration to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public static bool SaveConfig(GameConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Pretty format
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string jsonString = JsonSerializer.Serialize(config, options);
                System.IO.File.WriteAllText(ConfigFilePath, jsonString);
                return true;
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads game configuration from JSON file, returns default if file doesn't exist
        /// </summary>
        /// <returns>Loaded configuration or default configuration</returns>
        public static GameConfig LoadConfig()
        {
            try
            {
                if (!System.IO.File.Exists(ConfigFilePath))
                {
                    // Return default config if file doesn't exist
                    return new GameConfig();
                }

                string jsonString = System.IO.File.ReadAllText(ConfigFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var config = JsonSerializer.Deserialize<GameConfig>(jsonString, options);
                return config ?? new GameConfig(); // Return default if deserialization fails
            }
            catch (Exception ex)
            {
                // Log error and return default config
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
                return new GameConfig();
            }
        }

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
    }
}