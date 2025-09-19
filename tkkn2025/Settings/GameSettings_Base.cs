using System;
using System.Collections.Generic;
using System.ComponentModel;
using tkkn2025.Helpers;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public List<ISettingModel> BasicSettings { get; }
        public List<ISettingModel> ParticleSettings { get; }
        public List<ISettingModel> PowerUpSettings { get; }

        public List<ISettingModel> AllSettings { get; }

        private double _difficulty;
        public double Difficulty
        {
            get => _difficulty;
            private set
            {
                if (_difficulty != value)
                {
                    _difficulty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Difficulty)));
                }
            }
        }

        public GameSettings()
        {
            // Create the settings list (removed MusicEnabled)
            BasicSettings = new List<ISettingModel>
            {
                ShipSpeed,
                StartingParticles,
                LevelDuration,
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

                IsPowerUpEnabled_TimeWarp,
                PowerUpDuration_TimeWarp,

                IsPowerUpEnabled_Singularity,
                PowerUpDuration_Singularity,
                PowerUpForce_Singularity,

                IsPowerUpEnabled_Repulsor,
                PowerUpDuration_Repulsor,
                PowerUpForce_Repulsor,
            };

            AllSettings = new List<ISettingModel>();
            AllSettings.AddRange(BasicSettings);
            AllSettings.AddRange(ParticleSettings);
            AllSettings.AddRange(PowerUpSettings);

            // Subscribe to changes in settings that affect difficulty calculation
            SubscribeToSettingChanges();

            // Calculate initial difficulty
            CalculateDifficuly();
        }

        private void SubscribeToSettingChanges()
        {
            // Subscribe to the settings that affect difficulty calculation
            ShipSpeed.PropertyChanged += OnDifficultyRelevantSettingChanged;
            ParticleSpeed.PropertyChanged += OnDifficultyRelevantSettingChanged;
            ParticleTurnSpeed.PropertyChanged += OnDifficultyRelevantSettingChanged;
            StartingParticles.PropertyChanged += OnDifficultyRelevantSettingChanged;
            NewParticlesPerLevel.PropertyChanged += OnDifficultyRelevantSettingChanged;
            LevelDuration.PropertyChanged += OnDifficultyRelevantSettingChanged;
        }

        private void OnDifficultyRelevantSettingChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettingModel.Value))
            {
                CalculateDifficuly();
            }
        }

        private void CalculateDifficuly()
        {
            var speedRatio = ParticleSpeed / ShipSpeed * ParticleTurnSpeed * ParticleTurnSpeed;
            var particleCount = StartingParticles * ((1 + NewParticlesPerLevel) / LevelDuration);
            var powerUpFactor = PowerUpDuration_Repulsor + PowerUpDuration_Singularity + PowerUpDuration_TimeWarp;

            Difficulty = speedRatio * particleCount;

            Difficulty = Math.Round(Difficulty, 2);
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
                IsPowerUpEnabled_TimeWarp = IsPowerUpEnabled_TimeWarp,
                IsPowerUpEnabled_Singularity = IsPowerUpEnabled_Singularity,
                IsPowerUpEnabled_Repulsor = IsPowerUpEnabled_Repulsor,
                PowerUpDuration_TimeWarp = PowerUpDuration_TimeWarp,
                PowerUpDuration_Repulsor = PowerUpDuration_Repulsor,
                PowerUpForce_Repulsor = PowerUpForce_Repulsor,
                PowerUpDuration_Singularity = PowerUpDuration_Singularity,
                PowerUpForce_Singulaiorty = PowerUpForce_Singularity,
            };
        }

        public void LoadFromConfig(GameConfig config)
        {
            // Temporarily unsubscribe to avoid multiple calculations during bulk updates
            UnsubscribeFromSettingChanges();

            DebugHelper.WriteLine($"Loading config values into GameSettings:");
            DebugHelper.WriteLine($"  IsPowerUpTimeWarpEnabled: {config.IsPowerUpEnabled_TimeWarp}");
            DebugHelper.WriteLine($"  IsPowerUpSingularityEnabled: {config.IsPowerUpEnabled_Singularity}");
            DebugHelper.WriteLine($"  IsPowerUpRepulsorEnabled: {config.IsPowerUpEnabled_Repulsor}");

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
            IsPowerUpEnabled_TimeWarp.Value = config.IsPowerUpEnabled_TimeWarp;
            IsPowerUpEnabled_Singularity.Value = config.IsPowerUpEnabled_Singularity;
            IsPowerUpEnabled_Repulsor.Value = config.IsPowerUpEnabled_Repulsor;
            PowerUpDuration_TimeWarp.Value = config.PowerUpDuration_TimeWarp;
            PowerUpDuration_Repulsor.Value = config.PowerUpDuration_Repulsor;
            PowerUpForce_Repulsor.Value = config.PowerUpForce_Repulsor;
            PowerUpDuration_Singularity.Value = config.PowerUpDuration_Singularity;
            PowerUpForce_Singularity.Value = config.PowerUpForce_Singulaiorty;

            DebugHelper.WriteLine($"After loading - PowerUp enabled states:");
            DebugHelper.WriteLine($"  IsPowerUpTimeWarpEnabled: {IsPowerUpEnabled_TimeWarp.Value}");
            DebugHelper.WriteLine($"  IsPowerUpSingularityEnabled: {IsPowerUpEnabled_Singularity.Value}");
            DebugHelper.WriteLine($"  IsPowerUpRepulsorEnabled: {IsPowerUpEnabled_Repulsor.Value}");

            // Re-subscribe and calculate difficulty once
            SubscribeToSettingChanges();
            CalculateDifficuly();
        }

        private void UnsubscribeFromSettingChanges()
        {
            // Unsubscribe from the settings that affect difficulty calculation
            ShipSpeed.PropertyChanged -= OnDifficultyRelevantSettingChanged;
            ParticleSpeed.PropertyChanged -= OnDifficultyRelevantSettingChanged;
            ParticleTurnSpeed.PropertyChanged -= OnDifficultyRelevantSettingChanged;
            StartingParticles.PropertyChanged -= OnDifficultyRelevantSettingChanged;
            NewParticlesPerLevel.PropertyChanged -= OnDifficultyRelevantSettingChanged;
            LevelDuration.PropertyChanged -= OnDifficultyRelevantSettingChanged;
        }

        public void ResetToDefaults()
        {
            // Temporarily unsubscribe to avoid multiple calculations during bulk updates
            UnsubscribeFromSettingChanges();

            foreach (var setting in AllSettings)
            {
                setting.Value = setting.DefaultValue;
            }

            // Re-subscribe and calculate difficulty once
            SubscribeToSettingChanges();
            CalculateDifficuly();
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

            // Process PowerUpSettings
            foreach (var setting in PowerUpSettings)
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