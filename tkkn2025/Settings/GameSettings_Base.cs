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
            // Create the settings list (removed MusicEnabled)
            BasicSettings = new List<ISettingModel>
            {
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
                PowerUpSpawnRate,
                TimeWarpDuration,
               
                SingularityDuration,
                SingulaiortyForce,

                RepulsorDuration,
                RepulsorForce,

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
                // Use default metadata - this will be updated by the UI when saving with specific names
                ConfigName = "Current Settings",
                Description = "Current UI settings",
                CreatedBy = Session.PlayerName,
                DateCreated = DateTime.Now,
                LastModified = DateTime.Now,
                Version = "2.0",

                // Game settings from UI controls (removed MusicEnabled)
                ShipSpeed = ShipSpeed,
                LevelDuration = LevelDuration,
                StartingParticles = StartingParticles,
                NewParticlesPerLevel = NewParticlesPerLevel,

                ParticleSpeed = ParticleSpeed,
                ParticleTurnSpeed = ParticleTurnSpeed,
                ParticleSpeedVariance = ParticleSpeedVariance,
                ParticleRandomizerPercentage = ParticleRandomizerPercentage,
                IsParticleSpawnVectorTowardsShip = IsParticleSpawnVectorTowardsShip,
                IsParticleChaseShip = IsParticleChaseShip,

                // PowerUp settings
                PowerUpSpawnRate = PowerUpSpawnRate,
                TimeWarpDuration = TimeWarpDuration,
                RepulsorDuration = RepulsorDuration,
                RepulsorForce = RepulsorForce,
                SingularityDuration = SingularityDuration,
                SingulaiortyForce = SingulaiortyForce,
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
            IsParticleChaseShip.Value = config.IsParticleChaseShip;
            // Removed MusicEnabled - now handled in AppConfig

            // PowerUp settings
            PowerUpSpawnRate.Value = config.PowerUpSpawnRate;
            TimeWarpDuration.Value = config.TimeWarpDuration;
            RepulsorDuration.Value = config.RepulsorDuration;
            RepulsorForce.Value = config.RepulsorForce;
            SingularityDuration.Value = config.SingularityDuration;
            SingulaiortyForce.Value = config.SingulaiortyForce;
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