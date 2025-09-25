using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using tkkn2025.Settings.Models;

namespace tkkn2025.UI
{
    /// <summary>
    /// Leaderboard for Maze Mode games
    /// </summary>
    public partial class Leaderboard_Maze : UserControl
    {
        public Leaderboard_Maze()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the leaderboard with games data
        /// </summary>
        /// <param name="games">List of maze games ordered by duration (best first)</param>
        public void Initialize(List<Game> games)
        {
            try
            {
                LoadingText.Visibility = Visibility.Collapsed;
                ErrorText.Visibility = Visibility.Collapsed;

                if (games == null || !games.Any())
                {
                    ShowError("No Maze games found");
                    return;
                }

                HeaderGrid.Visibility = Visibility.Visible;
                DisplayGames(games);
            }
            catch (Exception ex)
            {
                ShowError($"Error displaying leaderboard: {ex.Message}");
            }
        }

        /// <summary>
        /// Show loading state
        /// </summary>
        public void ShowLoading()
        {
            LoadingText.Visibility = Visibility.Visible;
            LoadingText.Text = "Loading Maze leaderboard...";
            HeaderGrid.Visibility = Visibility.Collapsed;
            LeaderboardPanel.Children.Clear();
            ErrorText.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Show error message
        /// </summary>
        /// <param name="message">Error message to display</param>
        public void ShowError(string message)
        {
            LoadingText.Visibility = Visibility.Collapsed;
            HeaderGrid.Visibility = Visibility.Collapsed;
            LeaderboardPanel.Children.Clear();
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Display the games in the leaderboard
        /// </summary>
        /// <param name="games">Games to display (already ordered by duration desc)</param>
        private void DisplayGames(List<Game> games)
        {
            LeaderboardPanel.Children.Clear();

            for (int i = 0; i < Math.Min(10, games.Count); i++)
            {
                var game = games[i];
                var rank = i + 1;

                var entryGrid = new Grid();
                entryGrid.Margin = new Thickness(0, 2, 0, 2);

                // Define columns to match header
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                entryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

                // Rank with special formatting for top 3
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
                    Text = game.PlayerName,
                    Foreground = Brushes.White,
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
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(timeText, 2);
                entryGrid.Children.Add(timeText);

                // Level reached (estimated from duration and level duration)
                var levelText = new TextBlock
                {
                    Text = EstimateLevel(game).ToString(),
                    Foreground = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(levelText, 3);
                entryGrid.Children.Add(levelText);

                // Add background for better readability
                //if (rank <= 3)
                //{
                //    entryGrid.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                //}

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
                1 => "1",
                2 => "2", 
                3 => "3",
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
        /// Estimate the level reached based on game duration and level duration setting
        /// </summary>
        private int EstimateLevel(Game game)
        {
            try
            {
                double levelDuration = game.Settings.LevelDuration;
                return Math.Max(1, (int)(game.DurationSeconds / levelDuration) + 1);
            }
            catch
            {
                // Fallback calculation if settings are not available
                return Math.Max(1, (int)(game.DurationSeconds / 30.0) + 1); // Assume 30s per level
            }
        }
    }
}