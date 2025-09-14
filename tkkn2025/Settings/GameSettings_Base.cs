using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {
      
        public List<ISettingModel> BasicSettings { get; }
        public List<ISettingModel> ParticleSettings { get; }
        public List<ISettingModel> PowerUpSettings { get; }

        public List<ISettingModel> AllSettings { get; }

        
        public GameSettings()
        {
            // Create the settings list
            BasicSettings = new List<ISettingModel>
            {
                MusicEnabled,

                ShipSpeed,                                
                LevelDuration,
                StartingParticles,
                NewParticlesPerLevel,
                                
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

            PowerUpSettings = new List<ISettingModel>
            {
                
            };

            AllSettings = new List<ISettingModel>();
            AllSettings.AddRange(BasicSettings);
            AllSettings.AddRange(ParticleSettings);
            AllSettings.AddRange(PowerUpSettings);

        }

       
        public GameConfig ToGameConfig()
        {
            return new GameConfig
            {
                MusicEnabled = MusicEnabled.Value,

                ShipSpeed = ShipSpeed.Value,
                LevelDuration = LevelDuration.Value,
                StartingParticles = StartingParticles.Value,
                NewParticlesPerLevel = NewParticlesPerLevel.Value,

                ParticleSpeed = ParticleSpeed.Value,
                ParticleTurnSpeed = ParticleTurnSpeed.Value,
                ParticleSpeedVariance = ParticleSpeedVariance.Value,
                ParticleRandomizerPercentage = ParticleRandomizerPercentage.Value,
                IsParticleSpawnVectorTowardsShip = IsParticleSpawnVectorTowardsShip.Value,
            };
        }

    
        public void LoadFromConfig(GameConfig config)
        {
            ShipSpeed.Value = config.ShipSpeed;
            ParticleSpeed.Value = config.ParticleSpeed;
            ParticleTurnSpeed.Value = config.ParticleTurnSpeed;
            StartingParticles.Value = config.StartingParticles;
            LevelDuration.Value = config.LevelDuration;
            NewParticlesPerLevel.Value = config.NewParticlesPerLevel;
            ParticleSpeedVariance.Value = config.ParticleSpeedVariance;
            ParticleRandomizerPercentage.Value = config.ParticleRandomizerPercentage;
            IsParticleSpawnVectorTowardsShip.Value = config.IsParticleSpawnVectorTowardsShip;
            MusicEnabled.Value = config.MusicEnabled;
        }


        public void ResetToDefaults()
        {
            foreach (var setting in AllSettings)
            {
                setting.Value = setting.DefaultValue;
            }
        }

        
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