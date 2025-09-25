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
        /// Game mode setting that defines which preset configuration to use
        /// </summary>
        public static EnumSetting<GameMode> GameMode { get; } = new EnumSetting<GameMode>(
            name: nameof(GameMode),
            displayName: "Game Mode",
            category: "Gameplay",
            defaultValue: Models.GameMode.Standard,
            description: "Choose between different game modes with preset configurations"
        );

        public static DoubleSetting ShipSpeed { get; } = new DoubleSetting(
            name: nameof(ShipSpeed),
            displayName: "Ship Speed",
            category: "Movement",
            defaultValue: 200.0,
            min: 50.0,
            max: 500.0
        );

        public static DoubleSetting ShipBoost { get; } = new DoubleSetting(
            name: nameof(ShipBoost),
            displayName: "Ship Boost",
            category: "Movement",
            defaultValue: 75,
            min: 0.0,
            max: 150.0,
            description: "Ship speed value when shift key is pressed"
        );

        public static DoubleSetting ShipSuperBoost { get; } = new DoubleSetting(
            name: nameof(ShipSuperBoost),
            displayName: "Ship Super Boost",
            category: "Movement",
            defaultValue: 150,
            min: 0.0,
            max: 300,
            description: "Ship speed value when double-tapping and holding space key"
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
            min: 0,
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