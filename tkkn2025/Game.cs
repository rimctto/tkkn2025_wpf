using System;

namespace tkkn2025
{
    /// <summary>
    /// Represents a single game instance with its duration, particle count, and settings
    /// </summary>
    public class Game
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
        public int FinalParticleCount { get; set; }
        public GameConfig Settings { get; set; } = new GameConfig();
        public bool IsCompleted => EndTime.HasValue;
        public string PlayerName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the duration in seconds for easier comparison and display
        /// </summary>
        public double DurationSeconds => Duration.TotalSeconds;

        public Game()
        {
            StartTime = DateTime.Now;
            PlayerName = Session.PlayerName; // Assign current player name
        }

        public Game(GameConfig settings)
        {
            StartTime = DateTime.Now;
            PlayerName = Session.PlayerName; // Assign current player name
            Settings = new GameConfig
            {
                ShipSpeed = settings.ShipSpeed,
                ParticleSpeed = settings.ParticleSpeed,
                ParticleTurnSpeed = settings.ParticleTurnSpeed,
                StartingParticles = settings.StartingParticles,
                LevelDuration = settings.LevelDuration,
                NewParticlesPerLevel = settings.NewParticlesPerLevel,
                ParticleSpeedVariance = settings.ParticleSpeedVariance,
                ParticleRandomizerPercentage = settings.ParticleRandomizerPercentage,
                IsParticleSpawnVectorTowardsShip = settings.IsParticleSpawnVectorTowardsShip
            };
        }

        /// <summary>
        /// Marks the game as completed and records the final state
        /// /// <param name="finalParticleCount">The number of particles when the game ended</param>
        public void Complete(int finalParticleCount)
        {
            EndTime = DateTime.Now;
            FinalParticleCount = finalParticleCount;
        }

        public override string ToString()
        {
            return $"Game: {DurationSeconds:F1}s, {FinalParticleCount} particles";
        }
    }
}