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
            defaultValue: true,
            description: "Enable special level mechanics like StraightSweep attacks"
        );
    }
}
