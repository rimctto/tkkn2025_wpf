using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {

        public static DoubleSetting PowerUpSpawnRate { get; } = new DoubleSetting(
            name: nameof(PowerUpSpawnRate),
            displayName: "PowerUp Spawn Rate (s)",
            category: "PowerUps",
            defaultValue: 5,
            min: 3,
            max: 30,
            description: "How often power ups are created."
        );

        // Power-up enable/disable settings
        public static BoolSetting IsPowerUpEnabled_TimeWarp { get; } = new BoolSetting(
            name: nameof(IsPowerUpEnabled_TimeWarp),
            displayName: "Enable TimeWarp PowerUp",
            category: "PowerUps",
            defaultValue: true,
            description: "Enable or disable TimeWarp power-up spawning."
        );

        public static BoolSetting IsPowerUpEnabled_Singularity { get; } = new BoolSetting(
            name: nameof(IsPowerUpEnabled_Singularity),
            displayName: "Enable Singularity PowerUp",
            category: "PowerUps",
            defaultValue: true,
            description: "Enable or disable Singularity power-up spawning."
        );

        public static BoolSetting IsPowerUpEnabled_Repulsor { get; } = new BoolSetting(
            name: nameof(IsPowerUpEnabled_Repulsor),
            displayName: "Enable Repulsor PowerUp",
            category: "PowerUps",
            defaultValue: true,
            description: "Enable or disable Repulsor power-up spawning."
        );

        public static DoubleSetting PowerUpDuration_TimeWarp{ get; } = new DoubleSetting(
            name: nameof(PowerUpDuration_TimeWarp),
            displayName: "TimeWarp Duration (s)",
            category: "PowerUps",
            defaultValue: 3,
            min: 1,
            max: 10,
            description: "How long the power up lasts."
        );


        public static DoubleSetting PowerUpDuration_Singularity { get; } = new DoubleSetting(
            name: nameof(PowerUpDuration_Singularity),
            displayName: "Singularity Duration (s)",
            category: "PowerUps",
            defaultValue: 5,
            min: 3,
            max: 10,
            description: "How long the power up lasts."
        );


        public static DoubleSetting PowerUpForce_Singularity { get; } = new DoubleSetting(
            name: nameof(PowerUpForce_Singularity),
            displayName: "Singularity Force",
            category: "Particles",
            defaultValue: 350,
            min: 100.0,
            max: 1000,
            description: "Strength of the singularity force applied to particles"
        );


        public static DoubleSetting PowerUpDuration_Repulsor { get; } = new DoubleSetting(
            name: nameof(PowerUpDuration_Repulsor),
            displayName: "Repulsor Duration",
            category: "Particles",
            defaultValue: 5.0,
            min: 1.0,
            max: 10,
            description: "Duration in seconds that the Repulsor effect lasts when activated."
        );


        public static DoubleSetting PowerUpForce_Repulsor { get; } = new DoubleSetting(
            name: nameof(PowerUpForce_Repulsor),
            displayName: "Repulsor Force",
            category: "Particles",
            defaultValue: 50,
            min: 5,
            max: 150,
            description: "Strength of the repelling force applied by the Repulsor power-up."
        );

    }

}