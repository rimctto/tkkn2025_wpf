using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using tkkn2025.Settings.Models;
using tkkn2025.DataAccess;
using Firebase.Database;

namespace tkkn2025
{
    /// <summary>
    /// Represents a gaming session that tracks multiple games and their statistics
    /// Also manages the current AppConfig and GameConfig for the session
    /// Now includes Firebase leaderboard management for Survival and Maze modes
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

        #region Leaderboards

        /// <summary>
        /// Top 10 Survival mode games from Firebase (ordered by duration descending)
        /// </summary>
        public List<Game> SurvivalLeaderboard { get; private set; } = new List<Game>();

        /// <summary>
        /// Top 10 Maze mode games from Firebase (ordered by duration descending)
        /// </summary>
        public List<Game> MazeLeaderboard { get; private set; } = new List<Game>();

        /// <summary>
        /// Maps Game objects to their Firebase keys for proper deletion
        /// </summary>
        private Dictionary<Game, string> gameToFirebaseKeyMap = new Dictionary<Game, string>();

        /// <summary>
        /// Firebase connector for leaderboard operations
        /// </summary>
        private FireBaseConnector? firebaseConnector;

        /// <summary>
        /// Whether leaderboards have been loaded from Firebase
        /// </summary>
        public bool LeaderboardsLoaded { get; private set; } = false;

        #endregion
        
        // Overall statistics (all game modes combined)
        public int GamesPlayed => Games.Count(g => g.IsCompleted);
        public Game? LongestGame => Games.Where(g => g.IsCompleted).OrderByDescending(g => g.DurationSeconds).FirstOrDefault();
        public Game? ShortestGame => Games.Where(g => g.IsCompleted).OrderBy(g => g.DurationSeconds).FirstOrDefault();
        public double AverageGameTime => Games.Where(g => g.IsCompleted).Select(g => g.DurationSeconds).DefaultIfEmpty(0).Average();
        
        // Game mode specific statistics
        public int SurvivalGamesPlayed => Games.Count(g => g.IsCompleted && g.GameMode == GameMode.Survival);
        public int MazeGamesPlayed => Games.Count(g => g.IsCompleted && g.GameMode == GameMode.Maze);
        
        public Game? BestSurvivalGame => Games.Where(g => g.IsCompleted && g.GameMode == GameMode.Survival)
            .OrderByDescending(g => g.DurationSeconds).FirstOrDefault();
        public Game? BestMazeGame => Games.Where(g => g.IsCompleted && g.GameMode == GameMode.Maze)
            .OrderByDescending(g => g.DurationSeconds).FirstOrDefault();
            
        public double SurvivalAverageTime => Games.Where(g => g.IsCompleted && g.GameMode == GameMode.Survival)
            .Select(g => g.DurationSeconds).DefaultIfEmpty(0).Average();
        public double MazeAverageTime => Games.Where(g => g.IsCompleted && g.GameMode == GameMode.Maze)
            .Select(g => g.DurationSeconds).DefaultIfEmpty(0).Average();
        
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
        /// Initialize Firebase connector and load leaderboards
        /// </summary>
        /// <param name="connector">Firebase connector instance</param>
        public async Task InitializeFirebaseAsync(FireBaseConnector connector)
        {
            firebaseConnector = connector;
            await LoadLeaderboardsFromFirebaseAsync();
        }

        #region Leaderboard Management

        /// <summary>
        /// Load leaderboards from Firebase for both Survival and Maze modes
        /// </summary>
        public async Task LoadLeaderboardsFromFirebaseAsync()
        {
            if (firebaseConnector == null)
            {
                System.Diagnostics.Debug.WriteLine("Firebase connector not available for leaderboard loading");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Loading leaderboards from Firebase...");

                // Clear existing mappings
                gameToFirebaseKeyMap.Clear();

                // Load both leaderboards in parallel
                var survivalTask = GeLeaderboardAsync("Games/Survival", GameMode.Survival);
                var mazeTask = GeLeaderboardAsync("Games/Maze", GameMode.Maze);

                await Task.WhenAll(survivalTask, mazeTask);

                SurvivalLeaderboard = survivalTask.Result;
                MazeLeaderboard = mazeTask.Result;

                LeaderboardsLoaded = true;

                System.Diagnostics.Debug.WriteLine($"Leaderboards loaded - Survival: {SurvivalLeaderboard.Count}, Maze: {MazeLeaderboard.Count}");
                OnPropertyChanged(nameof(SurvivalLeaderboard));
                OnPropertyChanged(nameof(MazeLeaderboard));
                OnPropertyChanged(nameof(LeaderboardsLoaded));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading leaderboards from Firebase: {ex.Message}");
            }
        }

        /// <summary>
        /// Load leaderboard for a specific game mode from Firebase
        /// </summary>
        /// <param name="firebasePath">Firebase path for the game mode</param>
        /// <param name="gameMode">Game mode being loaded</param>
        /// <returns>Top 10 games ordered by duration (descending)</returns>
        private async Task<List<Game>> GeLeaderboardAsync(string firebasePath, GameMode gameMode)
        {
            try
            {
                var firebaseGames = await firebaseConnector!.ReadAllDataAsync<dynamic>(firebasePath);
                
                if (firebaseGames == null || !firebaseGames.Any())
                {
                    return new List<Game>();
                }

                var games = new List<Game>();

                foreach (var firebaseGame in firebaseGames)
                {
                    try
                    {
                        var game = ParseFirebaseGameData(firebaseGame, gameMode);
                        if (game != null)
                        {
                            games.Add(game);
                            // Store the Firebase key for this game
                            gameToFirebaseKeyMap[game] = firebaseGame.Key;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing game from Firebase: {ex.Message}");
                    }
                }

                // Return top 10 games ordered by duration (descending)
                var topGames = games
                    .OrderByDescending(g => g.DurationSeconds)
                    .Take(10)
                    .ToList();

                // Clean up games that didn't make the top 10 from Firebase
                var gamesToRemove = games
                    .OrderByDescending(g => g.DurationSeconds)
                    .Skip(10)
                    .ToList();

                foreach (var gameToRemove in gamesToRemove)
                {
                    await RemoveGameFromFirebaseAsync(gameToRemove);
                }

                return topGames;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {gameMode} leaderboard from Firebase: {ex.Message}");
                return new List<Game>();
            }
        }

        /// <summary>
        /// Parse Firebase game data into a Game object
        /// </summary>
        /// <param name="firebaseGame">Firebase game data</param>
        /// <param name="gameMode">Game mode</param>
        /// <returns>Parsed Game object or null if parsing failed</returns>
        private Game? ParseFirebaseGameData(FirebaseObject<dynamic> firebaseGame, GameMode gameMode)
        {
            try
            {
                var gameData = firebaseGame.Object;
                
                var game = new Game();
                
                // Parse basic game data
                game.PlayerName = gameData.PlayerName?.ToString() ?? "Unknown";
                game.GameMode = gameMode;
                
                // Parse dates
                if (DateTime.TryParse(gameData.StartTime?.ToString(), out DateTime startTime))
                {
                    game.StartTime = startTime;
                }
                
                if (DateTime.TryParse(gameData.EndTime?.ToString(), out DateTime endTime))
                {
                    game.EndTime = endTime;
                }

                // Parse duration
                if (double.TryParse(gameData.DurationSeconds?.ToString(), out double durationSeconds))
                {
                    // Validate duration makes sense
                    if (durationSeconds > 0 && durationSeconds < 86400) // Less than 24 hours
                    {
                        // If we have both start/end times, use the calculated duration for consistency
                        if (game.EndTime.HasValue)
                        {
                            // Keep the firebase duration as it might be more precise
                        }
                        else
                        {
                            // If no end time, create one based on duration
                            game.EndTime = game.StartTime.AddSeconds(durationSeconds);
                        }
                    }
                    else
                    {
                        return null; // Invalid duration
                    }
                }
                else
                {
                    return null; // No valid duration
                }

                // Parse particle count
                if (int.TryParse(gameData.FinalParticleCount?.ToString(), out int particleCount))
                {
                    game.FinalParticleCount = particleCount;
                }

                // Parse settings if available
                if (gameData.Settings != null)
                {
                    var settings = ConfigManager.CreateDefaultGameConfig();
                    settings.GameMode = gameMode;
                    
                    // Parse individual settings with fallbacks
                    if (double.TryParse(gameData.Settings.ShipSpeed?.ToString(), out double shipSpeed))
                        settings.ShipSpeed = shipSpeed;
                    if (double.TryParse(gameData.Settings.ParticleSpeed?.ToString(), out double particleSpeed))
                        settings.ParticleSpeed = particleSpeed;
                    if (double.TryParse(gameData.Settings.LevelDuration?.ToString(), out double levelDuration))
                        settings.LevelDuration = levelDuration;
                    if (int.TryParse(gameData.Settings.StartingParticles?.ToString(), out int startingParticles))
                        settings.StartingParticles = startingParticles;
                    if (bool.TryParse(gameData.Settings.LevelMechanicsEnabled?.ToString(), out bool levelMechanicsEnabled))
                        settings.LevelMechanicsEnabled = levelMechanicsEnabled;
                    
                    game.Settings = settings;
                }
                else
                {
                    // Use default settings for the game mode
                    game.Settings = ConfigManager.CreateConfigForGameMode(gameMode);
                }

                return game;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Firebase game data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Add a completed game to the appropriate leaderboard and save to Firebase
        /// This method handles both adding the game to Firebase and maintaining the top 10
        /// </summary>
        /// <param name="completedGame">The completed game to add</param>
        /// <returns>The Firebase key for the saved game</returns>
        public async Task<string?> AddGameToLeaderboardAsync(Game completedGame)
        {
            if (firebaseConnector == null || !LeaderboardsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("Firebase not available or leaderboards not loaded");
                return null;
            }

            // Only save Survival and Maze games to Firebase leaderboards
            if (completedGame.GameMode != GameMode.Survival && completedGame.GameMode != GameMode.Maze)
            {
                return null;
            }

            try
            {
                // Save the game to Firebase first
                string firebasePath = completedGame.GameMode == GameMode.Survival ? "Games/Survival" : "Games/Maze";
                string firebaseKey = await firebaseConnector.WriteDataAsync(firebasePath, completedGame);
                
                // Store the mapping
                gameToFirebaseKeyMap[completedGame] = firebaseKey;

                // Update the appropriate leaderboard
                await UpdateLeaderboardAsync(completedGame);

                System.Diagnostics.Debug.WriteLine($"Game saved to Firebase with key: {firebaseKey}");
                return firebaseKey;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding game to leaderboard: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update leaderboard when a game is completed
        /// This should be called after a game is saved to Firebase
        /// </summary>
        /// <param name="completedGame">The completed game</param>
        private async Task UpdateLeaderboardAsync(Game completedGame)
        {
            if (firebaseConnector == null || !LeaderboardsLoaded) return;

            try
            {
                List<Game> targetLeaderboard;
                string propertyName;

                switch (completedGame.GameMode)
                {
                    case GameMode.Survival:
                        targetLeaderboard = SurvivalLeaderboard;
                        propertyName = nameof(SurvivalLeaderboard);
                        break;
                    case GameMode.Maze:
                        targetLeaderboard = MazeLeaderboard;
                        propertyName = nameof(MazeLeaderboard);
                        break;
                    default:
                        return; // Only track Survival and Maze modes
                }

                // Add the new game to the leaderboard
                targetLeaderboard.Add(completedGame);

                // Sort by duration (descending) and keep only top 10
                var sortedGames = targetLeaderboard
                    .OrderByDescending(g => g.DurationSeconds)
                    .ToList();

                // If we have more than 10 games, remove the worst ones from Firebase
                if (sortedGames.Count > 10)
                {
                    var gamesToRemove = sortedGames.Skip(10).ToList();
                    
                    foreach (var gameToRemove in gamesToRemove)
                    {
                        await RemoveGameFromFirebaseAsync(gameToRemove);
                        targetLeaderboard.Remove(gameToRemove);
                    }
                }

                // Update the leaderboard with only the top 10
                if (completedGame.GameMode == GameMode.Survival)
                {
                    SurvivalLeaderboard = sortedGames.Take(10).ToList();
                }
                else if (completedGame.GameMode == GameMode.Maze)
                {
                    MazeLeaderboard = sortedGames.Take(10).ToList();
                }

                OnPropertyChanged(propertyName);

                System.Diagnostics.Debug.WriteLine($"Updated {completedGame.GameMode} leaderboard - now has {Math.Min(10, sortedGames.Count)} games");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a game from Firebase using its stored key
        /// </summary>
        /// <param name="game">Game to remove</param>
        private async Task RemoveGameFromFirebaseAsync(Game game)
        {
            if (firebaseConnector == null) return;

            try
            {
                // Check if we have a Firebase key for this game
                if (gameToFirebaseKeyMap.TryGetValue(game, out string firebaseKey))
                {
                    string firebasePath = game.GameMode switch
                    {
                        GameMode.Survival => "Games/Survival",
                        GameMode.Maze => "Games/Maze",
                        _ => null
                    };

                    if (firebasePath != null)
                    {
                        await firebaseConnector.DeleteDataAsync(firebasePath, firebaseKey);
                        gameToFirebaseKeyMap.Remove(game);
                        System.Diagnostics.Debug.WriteLine($"Removed game from Firebase: {firebasePath}/{firebaseKey}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No Firebase key found for game, cannot remove from Firebase");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing game from Firebase: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a game would make it to the leaderboard (useful for determining if we should save it)
        /// </summary>
        /// <param name="game">Game to check</param>
        /// <returns>True if the game would be in the top 10</returns>
        public bool WouldGameMakeLeaderboard(Game game)
        {
            List<Game> targetLeaderboard = game.GameMode switch
            {
                GameMode.Survival => SurvivalLeaderboard,
                GameMode.Maze => MazeLeaderboard,
                _ => new List<Game>()
            };

            // If we have fewer than 10 games, it automatically makes the leaderboard
            if (targetLeaderboard.Count < 10)
                return true;

            // Check if this game's duration is better than the worst game in the leaderboard
            var worstGame = targetLeaderboard.OrderBy(g => g.DurationSeconds).First();
            return game.DurationSeconds > worstGame.DurationSeconds;
        }

        #endregion

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
        /// Gets games filtered by game mode
        /// </summary>
        /// <param name="gameMode">The game mode to filter by</param>
        /// <returns>List of games for the specified mode</returns>
        public List<Game> GetGamesByMode(GameMode gameMode)
        {
            return Games.Where(g => g.IsCompleted && g.GameMode == gameMode).ToList();
        }

        /// <summary>
        /// Gets statistics for a specific game mode
        /// </summary>
        /// <param name="gameMode">The game mode to get statistics for</param>
        /// <returns>Formatted statistics for the specified mode</returns>
        public string GetGameModeStats(GameMode gameMode)
        {
            var modeGames = GetGamesByMode(gameMode);
            if (!modeGames.Any())
                return $"No {gameMode} games completed yet";

            var bestGame = modeGames.OrderByDescending(g => g.DurationSeconds).First();
            var averageTime = modeGames.Select(g => g.DurationSeconds).Average();
            var totalTime = TimeSpan.FromSeconds(modeGames.Sum(g => g.DurationSeconds));

            return $"{gameMode} Mode Stats:\n" +
                   $"Games Played: {modeGames.Count}\n" +
                   $"Best Time: {bestGame.DurationSeconds:F1}s\n" +
                   $"Average Time: {averageTime:F1}s\n" +
                   $"Total Play Time: {totalTime:mm\\:ss}";
        }

        /// <summary>
        /// Gets formatted statistics for display including mode-specific stats
        /// </summary>
        /// <returns>Formatted session statistics</returns>
        public string GetSessionStats()
        {
            if (GamesPlayed == 0)
                return "No games completed yet";

            var stats = $"Session Stats:\n" +
                       $"Player: {PlayerName}\n" +
                       $"Total Games: {GamesPlayed}\n" +
                       $"Overall Best: {LongestGame?.DurationSeconds:F1}s\n" +
                       $"Overall Average: {AverageGameTime:F1}s\n" +
                       $"Total Play Time: {TotalPlayTime:mm\\:ss}\n\n";

            
            if (SurvivalGamesPlayed > 0)
            {
                stats += $"Survival: {SurvivalGamesPlayed} games, Best: {BestSurvivalGame?.DurationSeconds:F1}s, Avg: {SurvivalAverageTime:F1}s\n";
            }
            if (MazeGamesPlayed > 0)
            {
                stats += $"Maze: {MazeGamesPlayed} games, Best: {BestMazeGame?.DurationSeconds:F1}s, Avg: {MazeAverageTime:F1}s\n";
            }

            return stats.TrimEnd();
        }

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}