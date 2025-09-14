using System.Collections.Generic;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public class BasicSettings
    {
        /// <summary>
        /// List of all basic game settings
        /// </summary>
        public List<ISettingModel> Settings { get; }

        /// <summary>
        /// Ship movement speed setting
        /// </summary>
        public DoubleSetting ShipSpeed { get; }

        /// <summary>
        /// Particle movement speed setting
        /// </summary>
        public DoubleSetting ParticleSpeed { get; }

        /// <summary>
        /// Number of particles to start the game with
        /// </summary>
        public IntegerSetting StartingParticles { get; }

        /// <summary>
        /// Time between level increases (in seconds)
        /// </summary>
        public DoubleSetting GenerationRate { get; }

        /// <summary>
        /// Percentage increase in particle count per level
        /// </summary>
        public DoubleSetting IncreaseRate { get; }

        /// <summary>
        /// Percentage variance in particle speed
        /// </summary>
        public DoubleSetting ParticleSpeedVariance { get; }

        /// <summary>
        /// Percentage of particles that get randomized speed
        /// </summary>
        public DoubleSetting ParticleRandomizerPercentage { get; }

        /// <summary>
        /// Whether particles chase the ship's current position or move toward center
        /// </summary>
        public BoolSetting ParticleChase_Initial { get; }

        /// <summary>
        /// Initialize the basic settings with default values
        /// </summary>
        public BasicSettings()
        {
            // Initialize individual settings with proper constraints based on the existing game
            ShipSpeed = new DoubleSetting(
                name: "ShipSpeed",
                displayName: "Ship Speed",
                category: "Movement",
                defaultValue: 200.0,
                min: 50.0,
                max: 500.0
            );

            ParticleSpeed = new DoubleSetting(
                name: "ParticleSpeed",
                displayName: "Particle Speed",
                category: "Particles",
                defaultValue: 175.0,
                min: 25.0,
                max: 300.0
            );

            StartingParticles = new IntegerSetting(
                name: "StartingParticles",
                displayName: "Starting Particles",
                category: "Particles",
                defaultValue: 30,
                min: 1,
                max: 100
            );

            GenerationRate = new DoubleSetting(
                name: "GenerationRate",
                displayName: "Level Duration (s)",
                category: "Gameplay",
                defaultValue: 5.0,
                min: 1.0,
                max: 20.0
            );

            IncreaseRate = new DoubleSetting(
                name: "IncreaseRate",
                displayName: "New Particles / Level",
                category: "Gameplay",
                defaultValue: 15.0,
                min: 1.0,
                max: 50.0
            );

            ParticleSpeedVariance = new DoubleSetting(
                name: "ParticleSpeedVariance",
                displayName: "Speed Variance (%)",
                category: "Particles",
                defaultValue: 15.0,
                min: 0.0,
                max: 100.0
            );

            ParticleRandomizerPercentage = new DoubleSetting(
                name: "ParticleRandomizerPercentage",
                displayName: "Random Particle %",
                category: "Particles",
                defaultValue: 30.0,
                min: 0.0,
                max: 100.0
            );

            ParticleChase_Initial = new BoolSetting(
                name: "ParticleChase_Initial",
                displayName: "Particles Chase Ship",
                category: "Particles",
                defaultValue: true
            );

            // Create the settings list
            Settings = new List<ISettingModel>
            {
                ShipSpeed,
                ParticleSpeed,
                StartingParticles,
                GenerationRate,
                IncreaseRate,
                ParticleSpeedVariance,
                ParticleRandomizerPercentage,
                ParticleChase_Initial
            };
        }

        /// <summary>
        /// Create a GameConfig instance from the current setting values
        /// </summary>
        /// <returns>GameConfig with current setting values</returns>
        public GameConfig ToGameConfig()
        {
            return new GameConfig
            {
                ShipSpeed = ShipSpeed.Value,
                ParticleSpeed = ParticleSpeed.Value,
                StartingParticles = StartingParticles.Value,
                GenerationRate = GenerationRate.Value,
                IncreaseRate = IncreaseRate.Value,
                ParticleSpeedVariance = ParticleSpeedVariance.Value,
                ParticleRandomizerPercentage = ParticleRandomizerPercentage.Value,
                ParticleChase_Initial = ParticleChase_Initial.Value
            };
        }

        /// <summary>
        /// Update all settings from a GameConfig instance
        /// </summary>
        /// <param name="config">GameConfig to load values from</param>
        public void FromGameConfig(GameConfig config)
        {
            ShipSpeed.Value = config.ShipSpeed;
            ParticleSpeed.Value = config.ParticleSpeed;
            StartingParticles.Value = config.StartingParticles;
            GenerationRate.Value = config.GenerationRate;
            IncreaseRate.Value = config.IncreaseRate;
            ParticleSpeedVariance.Value = config.ParticleSpeedVariance;
            ParticleRandomizerPercentage.Value = config.ParticleRandomizerPercentage;
            ParticleChase_Initial.Value = config.ParticleChase_Initial;
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var setting in Settings)
            {
                setting.Value = setting.DefaultValue;
            }
        }

        /// <summary>
        /// Get settings grouped by category
        /// </summary>
        /// <returns>Dictionary of category name to settings in that category</returns>
        public Dictionary<string, List<ISettingModel>> GetSettingsByCategory()
        {
            var result = new Dictionary<string, List<ISettingModel>>();
            
            foreach (var setting in Settings)
            {
                if (!result.ContainsKey(setting.Category))
                {
                    result[setting.Category] = new List<ISettingModel>();
                }
                result[setting.Category].Add(setting);
            }
            
            return result;
        }
    }
}