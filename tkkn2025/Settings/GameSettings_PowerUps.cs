using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {

        public DoubleSetting PowerUpSpawnRate { get; } = new DoubleSetting(
            name: nameof(PowerUpSpawnRate),
            displayName: "PowerUp Spawn Rate (s)",
            category: "PowerUps",
            defaultValue: 7.5,
            min: 3,
            max: 30,
            description: "How often power ups are created."
        );

    }

}