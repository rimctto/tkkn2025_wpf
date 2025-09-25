namespace tkkn2025.Settings.Models
{
    /// <summary>
    /// Represents different game modes with predefined configurations
    /// </summary>
    public enum GameMode
    {
        /// <summary>
        /// Standard game mode with all default settings
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Survival mode - Uses default settings except LevelMechanicsEnabled = false
        /// </summary>
        Survival = 1,
        
        /// <summary>
        /// Maze mode - NewParticlesPerLevel = 0, StartingParticles = 0, all power-ups disabled
        /// </summary>
        Maze = 2
    }
}