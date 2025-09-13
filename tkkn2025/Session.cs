using System;
using System.Collections.Generic;
using System.Linq;

namespace tkkn2025
{
    /// <summary>
    /// Represents a gaming session that tracks multiple games and their statistics
    /// </summary>
    public class Session
    {
        public DateTime SessionStartTime { get; private set; }
        public List<Game> Games { get; private set; }
        
        public int GamesPlayed => Games.Count(g => g.IsCompleted);
        public Game? LongestGame => Games.Where(g => g.IsCompleted).OrderByDescending(g => g.DurationSeconds).FirstOrDefault();
        public Game? ShortestGame => Games.Where(g => g.IsCompleted).OrderBy(g => g.DurationSeconds).FirstOrDefault();
        public double AverageGameTime => Games.Where(g => g.IsCompleted).Select(g => g.DurationSeconds).DefaultIfEmpty(0).Average();
        
        /// <summary>
        /// Gets the total time spent playing games in this session
        /// </summary>
        public TimeSpan TotalPlayTime 
        { 
            get 
            { 
                var totalSeconds = Games.Where(g => g.IsCompleted).Sum(g => g.DurationSeconds);
                return TimeSpan.FromSeconds(totalSeconds);
            } 
        }

        /// <summary>
        /// Gets the current session duration (time since session started)
        /// </summary>
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;

        public Session()
        {
            SessionStartTime = DateTime.Now;
            Games = new List<Game>();
        }

        /// <summary>
        /// Starts a new game with the specified settings
        /// </summary>
        /// <param name="settings">Game settings to use for this game</param>
        /// <returns>The newly created game instance</returns>
        public Game StartNewGame(GameConfig settings)
        {
            var game = new Game(settings);
            Games.Add(game);
            return game;
        }

        /// <summary>
        /// Completes the most recent game
        /// </summary>
        /// <param name="finalParticleCount">The number of particles when the game ended</param>
        public void CompleteCurrentGame(int finalParticleCount)
        {
            var currentGame = Games.LastOrDefault();
            if (currentGame != null && !currentGame.IsCompleted)
            {
                currentGame.Complete(finalParticleCount);
            }
        }

        /// <summary>
        /// Gets the currently active (incomplete) game
        /// </summary>
        /// <returns>The current game or null if no game is active</returns>
        public Game? GetCurrentGame()
        {
            return Games.LastOrDefault(g => !g.IsCompleted);
        }

        /// <summary>
        /// Gets formatted statistics for display
        /// </summary>
        /// <returns>Formatted session statistics</returns>
        public string GetSessionStats()
        {
            if (GamesPlayed == 0)
                return "No games completed yet";

            return $"Session Stats:\n" +
                   $"Games Played: {GamesPlayed}\n" +
                   $"Longest Game: {LongestGame?.DurationSeconds:F1}s\n" +
                   $"Shortest Game: {ShortestGame?.DurationSeconds:F1}s\n" +
                   $"Average Time: {AverageGameTime:F1}s\n" +
                   $"Total Play Time: {TotalPlayTime:mm\\:ss}";
        }
    }
}