using System;

namespace tkkn2025
{
    /// <summary>
    /// Represents a single game instance with its duration, particle count, and settings
    /// Each game stores a copy of the configuration used to play that game
    /// </summary>
    public class Game
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
        public int FinalParticleCount { get; set; }
        
        /// <summary>
        /// The game configuration used for this specific game instance
        /// This is a copy taken when the game starts to preserve the settings used
        /// </summary>
        public GameConfig Settings { get; set; }
        
        public bool IsCompleted => EndTime.HasValue;
        public string PlayerName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the duration in seconds for easier comparison and display
        /// </summary>
        public double DurationSeconds => Duration.TotalSeconds;

        /// <summary>
        /// Creates a new game with current session settings
        /// </summary>
        public Game()
        {
            StartTime = DateTime.Now;
            PlayerName = Session.PlayerName;
            Settings = ConfigManager.CreateDefaultGameConfig();
        }

        /// <summary>
        /// Creates a new game with the specified configuration
        /// The game gets its own copy of the configuration to preserve the settings used
        /// </summary>
        /// <param name="config">The game configuration to use for this game</param>
        public Game(GameConfig config)
        {
            StartTime = DateTime.Now;
            PlayerName = Session.PlayerName;
            
            // Create a copy of the configuration for this game
            Settings = config.CreateCopy();
        }

        /// <summary>
        /// Marks the game as completed and records the final state
        /// /// <param name="finalParticleCount">The number of particles when the game ended</param>
        public void Complete(int finalParticleCount)
        {
            EndTime = DateTime.Now;
            FinalParticleCount = finalParticleCount;
        }

        /// <summary>
        /// Gets a summary of the game including configuration details
        /// </summary>
        /// <returns>Formatted game summary</returns>
        public string GetGameSummary()
        {
            var summary = $"Game: {DurationSeconds:F1}s, {FinalParticleCount} particles\n";
            summary += $"Player: {PlayerName}\n";
            summary += $"Config: {Settings.ConfigName}\n";
            summary += $"Started: {StartTime:yyyy-MM-dd HH:mm:ss}";
            
            if (IsCompleted)
            {
                summary += $"\nEnded: {EndTime:yyyy-MM-dd HH:mm:ss}";
            }
            
            return summary;
        }

        /// <summary>
        /// Gets the game settings in a format suitable for display
        /// </summary>
        /// <returns>Formatted settings display</returns>
        public string GetSettingsDisplay()
        {
            return $"Ship Speed: {Settings.ShipSpeed}, " +
                   $"Particle Speed: {Settings.ParticleSpeed}, " +
                   $"Starting Particles: {Settings.StartingParticles}, " +
                   $"Level Duration: {Settings.LevelDuration}s";
        }

        public override string ToString()
        {
            return $"Game: {DurationSeconds:F1}s, {FinalParticleCount} particles ({Settings.ConfigName})";
        }
    }
}