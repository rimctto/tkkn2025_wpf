using System.Text.Json;

namespace tkkn2025
{
    /// <summary>
    /// Configuration class for storing and managing game settings
    /// </summary>
    public class GameConfig
    {
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

    }

    /// <summary>
    /// Handles saving and loading game configuration from JSON file
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "gameconfig.json";
        private static readonly string ConfigFilePath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            ConfigFileName);

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
        /// Gets the full path to the configuration file
        /// </summary>
        /// <returns>Full path to the config file</returns>
        public static string GetConfigFilePath()
        {
            return ConfigFilePath;
        }

        /// <summary>
        /// Checks if the configuration file exists
        /// </summary>
        /// <returns>True if config file exists, false otherwise</returns>
        public static bool ConfigFileExists()
        {
            return System.IO.File.Exists(ConfigFilePath);
        }
    }
}