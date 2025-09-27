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
        public List<ISettingModel> LevelMechanicsSettings { get; }

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
                GameMode,
                ShipSpeed,
                ShipBoost,
                ShipSuperBoost,
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

            LevelMechanicsSettings = new List<ISettingModel>
            {
                AreMazeWallsEnabled,
                MazeMode_InitialParticleSpeed,
                MazeMode_SpeedIncreasePerLevel
            };

            AllSettings = new List<ISettingModel>();
            AllSettings.AddRange(BasicSettings);
            AllSettings.AddRange(ParticleSettings);
            AllSettings.AddRange(PowerUpSettings);
            AllSettings.AddRange(LevelMechanicsSettings);

            // Subscribe to changes in settings that affect difficulty calculation
            SubscribeToSettingChanges();

            // Subscribe to GameMode changes to apply preset configurations
            GameMode.PropertyChanged += OnGameModeChanged;

            // Calculate initial difficulty
            CalculateDifficuly();
        }

        /// <summary>
        /// Handle game mode changes and apply preset configurations
        /// </summary>
        private void OnGameModeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISettingModel.Value))
            {
                ApplyGameModePreset(GameMode.Value);
            }
        }

        /// <summary>
        /// Apply preset configuration based on selected game mode
        /// </summary>
        /// <param name="gameMode">The game mode to apply</param>
        public void ApplyGameModePreset(Settings.Models.GameMode gameMode)
        {
            // Temporarily unsubscribe to avoid multiple calculations during bulk updates
            UnsubscribeFromSettingChanges();

            try
            {
                DebugHelper.WriteLine($"Applying game mode preset: {gameMode}");

                switch (gameMode)
                {
                   
                    case Settings.Models.GameMode.Survival:
                        // Reset to defaults first, then apply survival-specific changes
                        foreach (var setting in AllSettings.Where(s => s != GameMode))
                        {
                            setting.Value = setting.DefaultValue;
                        }
                        // Survival mode: Disable level mechanics
                        AreMazeWallsEnabled.Value = false;
                        DebugHelper.WriteLine("Applied Survival mode: LevelMechanicsEnabled = false");
                        break;

                    case Settings.Models.GameMode.Maze:
                        // Reset to defaults first, then apply maze-specific changes
                        foreach (var setting in AllSettings.Where(s => s != GameMode))
                        {
                            setting.Value = setting.DefaultValue;
                        }
                        // Maze mode: No particles, no power-ups
                        NewParticlesPerLevel.Value = 0;
                        StartingParticles.Value = 0;
                        IsPowerUpEnabled_TimeWarp.Value = false;
                        IsPowerUpEnabled_Repulsor.Value = false;
                        IsPowerUpEnabled_Singularity.Value = false;
                        DebugHelper.WriteLine("Applied Maze mode: No particles, no power-ups");
                        break;
                }
            }
            finally
            {
                // Re-subscribe and calculate difficulty once
                SubscribeToSettingChanges();
                CalculateDifficuly();
            }
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
            
            // Subscribe to game mode changes
            GameMode.PropertyChanged += OnGameModeChanged;
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

                // Game mode
                GameMode = GameMode,

                // Game settings from UI controls (removed MusicEnabled)
                ShipSpeed = ShipSpeed,
                ShipBoost = ShipBoost,
                ShipSuperBoost = ShipSuperBoost,
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

                LevelMechanicsEnabled = AreMazeWallsEnabled,
                InitialLevelSpeed = MazeMode_InitialParticleSpeed,
                SpeedIncreasePerLevel = MazeMode_SpeedIncreasePerLevel
            };
        }

        public void LoadFromConfig(GameConfig config)
        {
            // Temporarily unsubscribe to avoid multiple calculations during bulk updates
            UnsubscribeFromSettingChanges();

            DebugHelper.WriteLine($"Loading config values into GameSettings:");
            DebugHelper.WriteLine($"  GameMode: {config.GameMode}");
            DebugHelper.WriteLine($"  IsPowerUpTimeWarpEnabled: {config.IsPowerUpEnabled_TimeWarp}");
            DebugHelper.WriteLine($"  IsPowerUpSingularityEnabled: {config.IsPowerUpEnabled_Singularity}");
            DebugHelper.WriteLine($"  IsPowerUpRepulsorEnabled: {config.IsPowerUpEnabled_Repulsor}");

            GameMode.Value = config.GameMode;
            ShipSpeed.Value = config.ShipSpeed;
            ShipBoost.Value = config.ShipBoost;
            ShipSuperBoost.Value = config.ShipSuperBoost;
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

            AreMazeWallsEnabled.Value = config.LevelMechanicsEnabled;
            MazeMode_InitialParticleSpeed.Value = config.InitialLevelSpeed;
            MazeMode_SpeedIncreasePerLevel.Value = config.SpeedIncreasePerLevel;

            DebugHelper.WriteLine($"After loading - GameMode: {GameMode.Value}");
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
            
            // Unsubscribe from game mode changes
            GameMode.PropertyChanged -= OnGameModeChanged;
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

            // Process LevelMechanicsSettings
            foreach (var setting in LevelMechanicsSettings)
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