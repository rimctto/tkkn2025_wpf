using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using tkkn2025.DataAccess;
using Firebase.Database;

namespace tkkn2025.UI
{
    /// <summary>
    /// Interaction logic for GameOverScreen.xaml
    /// </summary>
    public partial class GameOverScreen : UserControl
    {
        private FireBaseConnector? firebaseConnector;
        
        public GameOverScreen()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the game over screen with game and session data
        /// </summary>
        public async Task InitializeAsync(Game currentGame, Session currentSession, FireBaseConnector? connector)
        {
            firebaseConnector = connector;
            
            // Display current game stats
            if (currentGame != null && CurrentGameTimeText != null)
            {
                CurrentGameTimeText.Text = $"Time: {currentGame.DurationSeconds:F1}s";
                if (CurrentGameParticlesText != null)
                    CurrentGameParticlesText.Text = $"Final Particles: {currentGame.FinalParticleCount}";
                if (CurrentGamePlayerText != null)
                    CurrentGamePlayerText.Text = $"Player: {currentGame.PlayerName}";
            }

            // Display session stats
            if (currentSession != null)
            {
                if (SessionGamesText != null)
                    SessionGamesText.Text = $"Games Played: {currentSession.GamesPlayed}";
                if (SessionBestText != null)
                    SessionBestText.Text = $"Best Time: {currentSession.LongestGame?.DurationSeconds:F1 ?? 0}s";
                if (SessionAverageText != null)
                    SessionAverageText.Text = $"Average: {currentSession.AverageGameTime:F1}s";
                if (SessionTotalText != null)
                    SessionTotalText.Text = $"Total Play Time: {currentSession.TotalPlayTime:mm\\:ss}";
            }

            // Load leaderboard
            await LoadLeaderboardAsync();
        }

        /// <summary>
        /// Load the top 3 games from Firebase
        /// </summary>
        private async Task LoadLeaderboardAsync()
        {
            if (firebaseConnector == null)
            {
                ShowLeaderboardError("No database connection");
                return;
            }

            try
            {
                if (LoadingText != null)
                {
                    LoadingText.Text = "Loading leaderboard...";
                    LoadingText.Visibility = Visibility.Visible;
                }

                // Get all games from Firebase
                var games = await firebaseConnector.ReadAllDataAsync<dynamic>("games");
                
                if (games == null || !games.Any())
                {
                    ShowLeaderboardError("No games found in database");
                    return;
                }

                // Parse and sort games by duration
                var leaderboardEntries = games
                    .Select(game => ParseGameData(game))
                    .Where(entry => entry != null)
                    .OrderByDescending(entry => entry.DurationSeconds)
                    .Take(3)
                    .ToList();

                if (!leaderboardEntries.Any())
                {
                    ShowLeaderboardError("No valid games found");
                    return;
                }

                DisplayLeaderboard(leaderboardEntries);
            }
            catch (Exception ex)
            {
                ShowLeaderboardError($"Error loading leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse game data from Firebase
        /// </summary>
        private LeaderboardEntry? ParseGameData(FirebaseObject<dynamic> firebaseGame)
        {
            try
            {
                var gameData = firebaseGame.Object;
                
                // Handle both direct properties and nested structure
                string playerName = "Unknown";
                double durationSeconds = 0;
                int finalParticleCount = 0;

                // Try to extract player name
                if (gameData.PlayerName != null)
                    playerName = gameData.PlayerName.ToString();

                // Try to extract duration
                if (gameData.DurationSeconds != null)
                    double.TryParse(gameData.DurationSeconds.ToString(), out durationSeconds);

                // Try to extract particle count
                if (gameData.FinalParticleCount != null)
                    int.TryParse(gameData.FinalParticleCount.ToString(), out finalParticleCount);

                if (durationSeconds > 0) // Only include valid games
                {
                    return new LeaderboardEntry
                    {
                        PlayerName = playerName,
                        DurationSeconds = durationSeconds,
                        FinalParticleCount = finalParticleCount
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing game data: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Display the leaderboard entries
        /// </summary>
        private void DisplayLeaderboard(List<LeaderboardEntry> entries)
        {
            if (LoadingText != null)
                LoadingText.Visibility = Visibility.Collapsed;
            
            if (LeaderboardPanel == null) return;
            
            LeaderboardPanel.Children.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var rank = i + 1;
                
                var entryGrid = new Grid();
                entryGrid.Margin = new Thickness(0, 5, 0, 5);
                
                // Define columns
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Rank
                var rankText = new TextBlock
                {
                    Text = GetRankText(rank),
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
                    Text = entry.PlayerName,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(nameText, 1);
                entryGrid.Children.Add(nameText);

                // Time
                var timeText = new TextBlock
                {
                    Text = $"{entry.DurationSeconds:F1}s",
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(timeText, 2);
                entryGrid.Children.Add(timeText);

                // Particles
                var particlesText = new TextBlock
                {
                    Text = entry.FinalParticleCount.ToString(),
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(particlesText, 3);
                entryGrid.Children.Add(particlesText);

                LeaderboardPanel.Children.Add(entryGrid);
            }
        }

        /// <summary>
        /// Get rank text with special formatting for top positions
        /// </summary>
        private string GetRankText(int rank)
        {
            return rank switch
            {
                1 => "??",
                2 => "??",
                3 => "??",
                _ => rank.ToString()
            };
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
    /// Represents a leaderboard entry
    /// </summary>
    public class LeaderboardEntry
    {
        public string PlayerName { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int FinalParticleCount { get; set; }
    }
}