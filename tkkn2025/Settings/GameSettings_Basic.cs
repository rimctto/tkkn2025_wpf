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
        /// Ship movement speed setting
        /// </summary>
        public DoubleSetting ShipSpeed { get; } = new DoubleSetting(
            name: nameof(ShipSpeed),
            displayName: "Ship Speed",
            category: "Movement",
            defaultValue: 200.0,
            min: 50.0,
            max: 500.0
        );




        /// <summary>
        /// Time between level increases (in seconds)
        /// </summary>
        public DoubleSetting LevelDuration { get; } = new DoubleSetting(
            name: nameof(LevelDuration),
            displayName: "Level Duration (s)",
            category: "Gameplay",
            defaultValue: 5.0,
            min: 1.0,
            max: 20.0
        );



        /// <summary>
        /// Percentage increase in particle count per level
        /// </summary>
        public DoubleSetting NewParticlesPerLevel { get; } = new DoubleSetting(
            name: nameof(NewParticlesPerLevel),
            displayName: "New Particles / Level",
            category: "Gameplay",
            defaultValue: 15.0,
            min: 1.0,
            max: 50.0
        );



        /// <summary>
        /// Number of particles to start the game with
        /// </summary>
        public IntegerSetting StartingParticles { get; } = new IntegerSetting(
            name: nameof(StartingParticles),
            displayName: "Starting Particles",
            category: "Particles",
            defaultValue: 20,
            min: 1,
            max: 100
        );



        /// <summary>
        /// Whether background music is enabled
        /// </summary>
        public BoolSetting MusicEnabled { get; } = new BoolSetting(
            name: nameof(MusicEnabled),
            displayName: "Music Enabled",
            category: "Audio",
            defaultValue: true,
            description: "Enable or disable background music"
        );
    }
}