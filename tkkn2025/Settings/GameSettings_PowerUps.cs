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

        public static DoubleSetting TimeWarpDuration{ get; } = new DoubleSetting(
            name: nameof(TimeWarpDuration),
            displayName: "TimeWarp Duration (s)",
            category: "PowerUps",
            defaultValue: 3,
            min: 1,
            max: 10,
            description: "How long the power up lasts."
        );


        public static DoubleSetting SingularityDuration { get; } = new DoubleSetting(
            name: nameof(SingularityDuration),
            displayName: "Singularity Duration (s)",
            category: "PowerUps",
            defaultValue: 5,
            min: 3,
            max: 10,
            description: "How long the power up lasts."
        );


        public static DoubleSetting SingulaiortyForce { get; } = new DoubleSetting(
            name: nameof(SingulaiortyForce),
            displayName: "Singularity Force",
            category: "Particles",
            defaultValue: 350,
            min: 100.0,
            max: 1000,
            description: "Strength of the singularity force applied to particles"
        );


        public static DoubleSetting RepulsorDuration { get; } = new DoubleSetting(
            name: nameof(RepulsorDuration),
            displayName: "Repulsor Duration",
            category: "Particles",
            defaultValue: 5.0,
            min: 1.0,
            max: 10,
            description: "Duration in seconds that the Repulsor effect lasts when activated."
        );


        public static DoubleSetting RepulsorForce { get; } = new DoubleSetting(
            name: nameof(RepulsorForce),
            displayName: "Repulsor Force",
            category: "Particles",
            defaultValue: 50,
            min: 5,
            max: 150,
            description: "Strength of the repelling force applied by the Repulsor power-up."
        );

    }

}