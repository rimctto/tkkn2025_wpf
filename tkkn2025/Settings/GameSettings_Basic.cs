using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {

       
        public static DoubleSetting ShipSpeed { get; } = new DoubleSetting(
            name: nameof(ShipSpeed),
            displayName: "Ship Speed",
            category: "Movement",
            defaultValue: 200.0,
            min: 50.0,
            max: 500.0
        );

        public static DoubleSetting LevelDuration { get; } = new DoubleSetting(
            name: nameof(LevelDuration),
            displayName: "Level Duration (s)",
            category: "Gameplay",
            defaultValue: 5.0,
            min: 1.0,
            max: 20.0
        );

        public static IntegerSetting StartingParticles { get; } = new IntegerSetting(
            name: nameof(StartingParticles),
            displayName: "Starting Particles",
            category: "Particles",
            defaultValue: 25,
            min: 1,
            max: 100
        );

        public static DoubleSetting NewParticlesPerLevel { get; } = new DoubleSetting(
            name: nameof(NewParticlesPerLevel),
            displayName: "New Particles / Level",
            category: "Gameplay",
            defaultValue: 5.0,
            min: 1.0,
            max: 50.0
        );
    }
}