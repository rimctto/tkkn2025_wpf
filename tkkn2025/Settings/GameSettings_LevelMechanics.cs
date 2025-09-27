using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains level mechanic settings including StraightSweep parameters
    /// </summary>
    public partial class GameSettings
    {
        /// <summary>
        /// Whether level mechanics are enabled
        /// </summary>
        public static BoolSetting AreMazeWallsEnabled { get; } = new BoolSetting(
            name: nameof(AreMazeWallsEnabled),
            displayName: "Enable Maze Generation",
            category: "Maze Mode",
            defaultValue: false,
            description: "Enable special level mechanics like StraightSweep attacks"
        );

        /// <summary>
        /// Initial speed for level mechanics when the game starts
        /// </summary>
        public static DoubleSetting MazeMode_InitialParticleSpeed { get; } = new DoubleSetting(
            name: nameof(MazeMode_InitialParticleSpeed),
            displayName: "Initial Level Speed",
            category: "Maze Mode",
            defaultValue: 175.0,
            min: 50.0,
            max: 500.0,
            description: "Initial speed for level mechanic particles at the start of the game"
        );

        /// <summary>
        /// Speed increase per level for level mechanics
        /// </summary>
        public static DoubleSetting MazeMode_SpeedIncreasePerLevel { get; } = new DoubleSetting(
            name: nameof(MazeMode_SpeedIncreasePerLevel),
            displayName: "Speed Increase Per Level",
            category: "Maze Mode",
            defaultValue: 50.0,
            min: 0.0,
            max: 200.0,
            description: "How much the speed increases with each level progression"
        );

        /// <summary>
        /// Speed increase per level for level mechanics
        /// </summary>
        public static DoubleSetting MazeMode_SpeedRamprate { get; } = new DoubleSetting(
            name: nameof(MazeMode_SpeedIncreasePerLevel),
            displayName: "Speed Increase Rate (s)",
            category: "Maze Mode",
            defaultValue: 50.0,
            min: 0.0,
            max: 200.0,
            description: "How fast the speed ramps up"
        );

    }
}
