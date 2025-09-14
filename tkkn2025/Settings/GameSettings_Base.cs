using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {
        /// <summary>
        /// List of all basic game settings
        /// </summary>
        public List<ISettingModel> BasicSettings { get; }
        public List<ISettingModel> ParticleSettings { get; }
        public List<ISettingModel> PowerUpSettings { get; }

        /// <summary>
        /// Initialize the basic settings with default values
        /// </summary>
        public GameSettings()
        {
            // Create the settings list
            BasicSettings = new List<ISettingModel>
            {
                ShipSpeed,                                
                LevelDuration,
                StartingParticles,
                NewParticlesPerLevel,
                MusicEnabled,
                                
            };

            ParticleSettings = new List<ISettingModel>
            {
                ParticleSpeed,
                ParticleTurnSpeed,
                ParticleRandomizerPercentage,
                ParticleSpeedVariance,
                IsParticleSpawnVectorTowardsShip,
                IsParticleChaseShip
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
                ParticleTurnSpeed = ParticleTurnSpeed.Value,
                StartingParticles = StartingParticles.Value,
                LevelDuration = LevelDuration.Value,
                NewParticlesPerLevel = NewParticlesPerLevel.Value,
                ParticleSpeedVariance = ParticleSpeedVariance.Value,
                ParticleRandomizerPercentage = ParticleRandomizerPercentage.Value,
                ParticleChase_Initial = IsParticleSpawnVectorTowardsShip.Value,
                MusicEnabled = MusicEnabled.Value
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
            ParticleTurnSpeed.Value = config.ParticleTurnSpeed;
            StartingParticles.Value = config.StartingParticles;
            LevelDuration.Value = config.LevelDuration;
            NewParticlesPerLevel.Value = config.NewParticlesPerLevel;
            ParticleSpeedVariance.Value = config.ParticleSpeedVariance;
            ParticleRandomizerPercentage.Value = config.ParticleRandomizerPercentage;
            IsParticleSpawnVectorTowardsShip.Value = config.ParticleChase_Initial;
            MusicEnabled.Value = config.MusicEnabled;
        }

        /// <summary>
        /// Reset all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            foreach (var setting in BasicSettings)
            {
                setting.Value = setting.DefaultValue;
            }
            
            foreach (var setting in ParticleSettings)
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
            
            // Process BasicSettings
            foreach (var setting in BasicSettings)
            {
                if (!result.ContainsKey(setting.Category))
                {
                    result[setting.Category] = new List<ISettingModel>();
                }
                result[setting.Category].Add(setting);
            }
            
            // Process ParticleSettings
            foreach (var setting in ParticleSettings)
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