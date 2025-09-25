using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using tkkn2025.DataAccess;
using tkkn2025.Settings.Models;
using tkkn2025.UI;

namespace tkkn2025.UI
{
    /// <summary>
    /// Interaction logic for GameOverScreen.xaml
    /// </summary>
    public partial class GameOverScreen : UserControl
    {
        private FireBaseConnector? firebaseConnector;
        private Session? currentSession;
        private Game? currentGame;
        
        public GameOverScreen()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the game over screen with game and session data
        /// </summary>
        public async Task InitializeAsync(Game? game, Session session, FireBaseConnector? connector)
        {
            firebaseConnector = connector;
            currentSession = session;
            currentGame = game;
            
            // Display current game stats
            if (game != null && CurrentGameTimeText != null)
            {
                CurrentGameTimeText.Text = $"Time: {game.DurationSeconds:F1}s";
                if (CurrentGameParticlesText != null)
                    CurrentGameParticlesText.Text = $"Final Particles: {game.FinalParticleCount}";
                if (CurrentGamePlayerText != null)
                    CurrentGamePlayerText.Text = $"Player: {game.PlayerName}";
            }

            // Display session stats
            if (session != null)
            {
                if (SessionGamesText != null)
                    SessionGamesText.Text = $"Games Played: {session.GamesPlayed}";
                if (SessionBestText != null)
                    SessionBestText.Text = $"Best Time: {session.LongestGame?.DurationSeconds:F1 ?? 0}s";
                if (SessionAverageText != null)
                    SessionAverageText.Text = $"Average: {session.AverageGameTime:F1}s";
                if (SessionTotalText != null)
                    SessionTotalText.Text = $"Total Play Time: {session.TotalPlayTime:mm\\:ss}";
            }

            // Load appropriate leaderboard based on current game mode
            await LoadLeaderboardAsync();
        }

        /// <summary>
        /// Load the appropriate leaderboard based on the game mode
        /// </summary>
        private async Task LoadLeaderboardAsync()
        {
            if (currentSession == null || !currentSession.LeaderboardsLoaded)
            {
                ShowLeaderboardError("Leaderboards not available");
                return;
            }

            try
            {
                if (LoadingText != null)
                {
                    LoadingText.Text = "Loading leaderboard...";
                    LoadingText.Visibility = Visibility.Visible;
                }

                // Determine which leaderboard to show based on the current game mode
                List<Game> leaderboardGames = new List<Game>();
                string leaderboardTitle = "Leaderboard";

                if (currentGame != null)
                {
                    switch (currentGame.GameMode)
                    {
                        case GameMode.Survival:
                            leaderboardGames = new List<Game>(currentSession.SurvivalLeaderboard);
                            leaderboardTitle = "Survival Mode Leaderboard";
                            break;
                        case GameMode.Maze:
                            leaderboardGames = new List<Game>(currentSession.MazeLeaderboard);
                            leaderboardTitle = "Maze Mode Leaderboard";
                            break;
                        default:
                            // For Standard mode, show a message that it's not tracked
                            ShowLeaderboardMessage("Standard mode games are not tracked on leaderboards");
                            return;
                    }

                    // Check if the current game should be included in the leaderboard
                    // This handles the case where the game was just completed but hasn't been
                    // added to the session leaderboard lists yet due to async timing
                    bool shouldIncludeCurrentGame = ShouldIncludeCurrentGameInLeaderboard(leaderboardGames, currentGame);
                    
                    if (shouldIncludeCurrentGame)
                    {
                        leaderboardGames.Add(currentGame);
                        System.Diagnostics.Debug.WriteLine($"Added current game to leaderboard display: {currentGame.DurationSeconds:F1}s");
                    }
                }
                else
                {
                    // No current game, show survival leaderboard by default
                    leaderboardGames = new List<Game>(currentSession.SurvivalLeaderboard);
                    leaderboardTitle = "Survival Mode Leaderboard";
                }

                // Update the leaderboard header (we'll need to find it in the XAML)
                UpdateLeaderboardTitle(leaderboardTitle);

                if (!leaderboardGames.Any())
                {
                    ShowLeaderboardMessage($"No games found for {(currentGame?.GameMode.ToString() ?? "Survival")} mode");
                    return;
                }

                // Sort and take top 10 for display
                var sortedGames = leaderboardGames
                    .OrderByDescending(g => g.DurationSeconds)
                    .Take(10)
                    .ToList();

                DisplayLeaderboard(sortedGames);
            }
            catch (Exception ex)
            {
                ShowLeaderboardError($"Error loading leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine if the current game should be included in the leaderboard display
        /// </summary>
        /// <param name="existingGames">Current leaderboard games</param>
        /// <param name="currentGame">The game that was just completed</param>
        /// <returns>True if the current game should be added to the display</returns>
        private bool ShouldIncludeCurrentGameInLeaderboard(List<Game> existingGames, Game currentGame)
        {
            // Only include Survival and Maze games in leaderboards
            if (currentGame.GameMode != GameMode.Survival && currentGame.GameMode != GameMode.Maze)
            {
                return false;
            }

            // Check if the current game is already in the leaderboard
            // Compare by game properties since the object reference might be different
            bool gameAlreadyExists = existingGames.Any(g => 
                g.PlayerName == currentGame.PlayerName &&
                Math.Abs(g.DurationSeconds - currentGame.DurationSeconds) < 0.1 && // Allow small floating point differences
                g.FinalParticleCount == currentGame.FinalParticleCount &&
                Math.Abs((g.StartTime - currentGame.StartTime).TotalSeconds) < 5); // Allow small time differences

            if (gameAlreadyExists)
            {
                return false; // Game is already in the leaderboard
            }

            // If we have fewer than 10 games, the current game should be included
            if (existingGames.Count < 10)
            {
                return true;
            }

            // Check if the current game's duration is better than the worst game in the leaderboard
            var worstGame = existingGames.OrderBy(g => g.DurationSeconds).First();
            return currentGame.DurationSeconds > worstGame.DurationSeconds;
        }

        /// <summary>
        /// Update the leaderboard title
        /// </summary>
        private void UpdateLeaderboardTitle(string title)
        {
            // Find the leaderboard header TextBlock and update its text
            // We'll need to locate it by traversing the visual tree or give it a name
            var headerTextBlocks = FindVisualChildren<TextBlock>(this)
                .Where(tb => tb.Text == "Leaderboard" && tb.FontSize == 24)
                .FirstOrDefault();

            if (headerTextBlocks != null)
            {
                headerTextBlocks.Text = title;
            }
        }

        /// <summary>
        /// Helper method to find child elements of a specific type
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T t)
                    yield return t;

                foreach (T childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        /// <summary>
        /// Display the leaderboard entries
        /// </summary>
        private void DisplayLeaderboard(List<Game> games)
        {
            if (LoadingText != null)
                LoadingText.Visibility = Visibility.Collapsed;
            
            if (LeaderboardPanel == null) return;
            
            LeaderboardPanel.Children.Clear();

            for (int i = 0; i < Math.Min(10, games.Count); i++)
            {
                var game = games[i];
                var rank = i + 1;
                
                var entryGrid = new Grid();
                entryGrid.Margin = new Thickness(0, 2, 0, 2);
                
                // Define columns to match the header
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Highlight the current game if it's in the leaderboard
                bool isCurrentGame = currentGame != null && 
                    game.PlayerName == currentGame.PlayerName &&
                    Math.Abs(game.DurationSeconds - currentGame.DurationSeconds) < 0.1 &&
                    game.FinalParticleCount == currentGame.FinalParticleCount;

                if (isCurrentGame)
                {
                    entryGrid.Background = new SolidColorBrush(Color.FromArgb(60, 255, 215, 0)); // Semi-transparent gold highlight
                }

                // Rank with special formatting for top 3
                var rankText = new TextBlock
                {
                    Text = rank.ToString(),
                    Foreground = GetRankColor(rank),
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(rankText, 0);
                entryGrid.Children.Add(rankText);

                // Player Name
                var nameText = new TextBlock
                {
                    Text = game.PlayerName,
                    Foreground = isCurrentGame ? Brushes.Gold : Brushes.White,
                    FontWeight = isCurrentGame ? FontWeights.Bold : FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameText, 1);
                entryGrid.Children.Add(nameText);

                // Time with formatting
                var timeText = new TextBlock
                {
                    Text = FormatTime(game.DurationSeconds),
                    Foreground = isCurrentGame ? Brushes.Gold : Brushes.LightGray,
                    FontWeight = isCurrentGame ? FontWeights.Bold : FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(timeText, 2);
                entryGrid.Children.Add(timeText);

                // Particles
                var particlesText = new TextBlock
                {
                    Text = game.FinalParticleCount.ToString(),
                    Foreground = isCurrentGame ? Brushes.Gold : Brushes.LightGray,
                    FontWeight = isCurrentGame ? FontWeights.Bold : FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(particlesText, 3);
                entryGrid.Children.Add(particlesText);
               
                LeaderboardPanel.Children.Add(entryGrid);
            }
        }


        /// <summary>
        /// Get rank color based on position
        /// </summary>
        private Brush GetRankColor(int rank)
        {
            return rank switch
            {
                1 => Brushes.Gold,
                2 => Brushes.Silver,
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)), // Bronze
                _ => Brushes.White
            };
        }

        /// <summary>
        /// Format time duration for display
        /// </summary>
        private string FormatTime(double seconds)
        {
            if (seconds >= 60)
            {
                var minutes = (int)(seconds / 60);
                var remainingSeconds = seconds % 60;
                return $"{minutes}:{remainingSeconds:00.0}";
            }
            return $"{seconds:F1}s";
        }

        /// <summary>
        /// Show an error message in the leaderboard area
        /// </summary>
        private void ShowLeaderboardError(string message)
        {
            if (LoadingText != null)
            {
                LoadingText.Text = message;
                LoadingText.Foreground = Brushes.Red;
                LoadingText.Visibility = Visibility.Visible;
            }

            LeaderboardPanel?.Children.Clear();
        }

        /// <summary>
        /// Show a regular message in the leaderboard area
        /// </summary>
        private void ShowLeaderboardMessage(string message)
        {
            if (LoadingText != null)
            {
                LoadingText.Text = message;
                LoadingText.Foreground = Brushes.Yellow;
                LoadingText.Visibility = Visibility.Visible;
            }

            LeaderboardPanel?.Children.Clear();
        }

        /// <summary>
        /// Handle mouse clicks on the game over screen to focus the game canvas
        /// </summary>
        private void GameOverScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Find the parent window
            var window = Window.GetWindow(this);
            if (window is MainWindow mainWindow)
            {
                // Focus the game canvas when the game over screen is clicked
                mainWindow.GameCanvas.Focus();
            }
            
            e.Handled = true;
        }
    }

    /// <summary>
    /// Represents a leaderboard entry (kept for compatibility but now we use Game objects directly)
    /// </summary>
    public class LeaderboardEntry
    {
        public string PlayerName { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int FinalParticleCount { get; set; }
    }
}