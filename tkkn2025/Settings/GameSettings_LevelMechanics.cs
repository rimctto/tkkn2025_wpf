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
        public static BoolSetting LevelMechanicsEnabled { get; } = new BoolSetting(
            name: nameof(LevelMechanicsEnabled),
            displayName: "Enable Level Mechanics",
            category: "Level Mechanics",
            defaultValue: false,
            description: "Enable special level mechanics like StraightSweep attacks"
        );

        /// <summary>
        /// Speed increase per level for level mechanics
        /// </summary>
        public static DoubleSetting SpeedIncreasePerLevel { get; } = new DoubleSetting(
            name: nameof(SpeedIncreasePerLevel),
            displayName: "Speed Increase Per Level",
            category: "Level Mechanics",
            defaultValue: 50.0,
            min: 0.0,
            max: 200.0,
            description: "How much the speed increases with each level progression"
        );

        /// <summary>
        /// Initial speed for level mechanics when the game starts
        /// </summary>
        public static DoubleSetting InitialLevelSpeed { get; } = new DoubleSetting(
            name: nameof(InitialLevelSpeed),
            displayName: "Initial Level Speed",
            category: "Level Mechanics",
            defaultValue: 175.0,
            min: 50.0,
            max: 500.0,
            description: "Initial speed for level mechanic particles at the start of the game"
        );
    }
}
