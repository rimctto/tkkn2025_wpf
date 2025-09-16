using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace tkkn2025
{
    /// <summary>
    /// Represents a gaming session that tracks multiple games and their statistics
    /// Also manages the current AppConfig and GameConfig for the session
    /// </summary>
    public class Session : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Static player name that persists across sessions
        /// </summary>
        public static string PlayerName 
        { 
            get => _playerName;
            set 
            {
                if (_playerName != value)
                {
                    _playerName = value;
                    // Update app config when player name changes
                    if (_instance != null)
                    {
                        _instance.AppConfig.PlayerName = value;
                        _instance.OnPropertyChanged(nameof(PlayerName));
                    }
                }
            }
        }
        private static string _playerName = "Anonymous";
        private static Session? _instance;

        /// <summary>
        /// Application configuration for this session
        /// </summary>
        public AppConfig AppConfig { get; private set; }

        /// <summary>
        /// Current game configuration for this session
        /// </summary>
        public GameConfig GameConfig { get; private set; }

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
            _instance = this;
            SessionStartTime = DateTime.Now;
            Games = new List<Game>();
            
            // Initialize configurations
            LoadConfigurations();
        }

        /// <summary>
        /// Loads both app and game configurations from ConfigManager
        /// </summary>
        private void LoadConfigurations()
        {
            try
            {
                // Load app configuration
                AppConfig = ConfigManager.LoadAppConfig();
                PlayerName = AppConfig.PlayerName;
                
                // Load the current default game configuration (auto-persisted working settings)
                GameConfig = ConfigManager.LoadDefaultConfig();
                
                System.Diagnostics.Debug.WriteLine($"Session configurations loaded - Player: {PlayerName}, Config: {GameConfig.ConfigName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configurations: {ex.Message}");
                
                // Use defaults if loading fails
                AppConfig = new AppConfig { PlayerName = PlayerName };
                GameConfig = ConfigManager.CreateDefaultGameConfig();
            }
        }

        /// <summary>
        /// Saves both app and game configurations using ConfigManager
        /// This saves the current working configuration as the new default
        /// </summary>
        public void SaveConfigurations()
        {
            try
            {
                // Update app config with current player name
                AppConfig.PlayerName = PlayerName;
                
                // Save both configurations
                bool appSaved = ConfigManager.SaveAppConfig(AppConfig);
                bool gameSaved = ConfigManager.SaveDefaultConfig(GameConfig);
                
                if (appSaved && gameSaved)
                {
                    System.Diagnostics.Debug.WriteLine("Session configurations saved successfully - current settings are now default");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Configuration save issues - App: {appSaved}, Game: {gameSaved}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving configurations: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the current game configuration and optionally saves it as the new default
        /// </summary>
        /// <param name="newConfig">The new game configuration</param>
        /// <param name="saveAsDefault">Whether to save the configuration as the new default</param>
        public void UpdateGameConfig(GameConfig newConfig, bool saveAsDefault = true)
        {
            try
            {
                GameConfig = newConfig;
                OnPropertyChanged(nameof(GameConfig));
                
                if (saveAsDefault)
                {
                    bool saved = ConfigManager.SaveDefaultConfig(GameConfig);
                    System.Diagnostics.Debug.WriteLine($"Game config updated and saved as default: {saved}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Game config updated (not saved as default)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating game config: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a new game with the current session game configuration
        /// Each game gets a copy of the current config to preserve settings used for that game
        /// </summary>
        /// <returns>The newly created game instance</returns>
        public Game StartNewGame()
        {
            // Create a copy of the current game config for this specific game
            var gameConfigCopy = GameConfig.CreateCopy();
            var game = new Game(gameConfigCopy);
            Games.Add(game);
            
            System.Diagnostics.Debug.WriteLine($"New game started with config: {gameConfigCopy.ConfigName}");
            return game;
        }

        /// <summary>
        /// Starts a new game with specific settings (overrides current session config for this game only)
        /// </summary>
        /// <param name="settings">Game settings to use for this game</param>
        /// <returns>The newly created game instance</returns>
        public Game StartNewGame(GameConfig settings)
        {
            // Use the provided settings for this specific game
            var game = new Game(settings);
            Games.Add(game);
            
            System.Diagnostics.Debug.WriteLine($"New game started with custom config: {settings.ConfigName}");
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
                System.Diagnostics.Debug.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {finalParticleCount} particles");
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
                   $"Player: {PlayerName}\n" +
                   $"Games Played: {GamesPlayed}\n" +
                   $"Longest Game: {LongestGame?.DurationSeconds:F1}s\n" +
                   $"Shortest Game: {ShortestGame?.DurationSeconds:F1}s\n" +
                   $"Average Time: {AverageGameTime:F1}s\n" +
                   $"Total Play Time: {TotalPlayTime:mm\\:ss}";
        }

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}