using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using tkkn2025.Settings;
using tkkn2025.DataAccess;
using tkkn2025.UI.Windows;
using tkkn2025.Helpers;
using tkkn2025.Core;

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Main window handles UI interactions, while GameEngine handles all game logic
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        // Game engine handles all game logic
        private GameEngine gameEngine = null!;

        // UI state tracking
        private bool gameOverScreenVisible = false;

        // Session management
        private Session currentSession = null!;

        // Settings Manager for MVVM data binding
        public SettingsManager SettingsManager { get; set; } = new SettingsManager();

        // Firebase connector for saving game data
        private FireBaseConnector firebaseConnector = null!;

        // Debug windows
        private DebugWindow? debugWindow = null;
        private FireBaseEditor? firebaseEditorWindow = null;
        private FirebaseEditorWindow_MVVM? firebaseEditorMVVMWindow = null;
        private UI.SandboxWindow? sandboxWindow = null;

        #endregion

        #region Constructor and Initialization

        public MainWindow()
        {
            DataContext = SettingsManager;
            InitializeComponent();

            InitializeFirebase();
            _ = InitializeSessionAsync(); // Fire and forget async initialization
            InitializeGameEngine();
            SubscribeToGameEvents();

            // Ensure window can receive keyboard input
            this.Loaded += (s, e) =>
            {
                this.Focus();

                // Initialize player name TextBox after controls are loaded
                PlayerNameTextBox.Text = Session.PlayerName;
                PlayerNameTextBox.TextChanged += PlayerNameTextBox_TextChanged;

                // Initialize game mode button styles
                UpdateGameModeButtonStyles();

                // Music player control will auto-initialize when loaded
                DebugHelper.WriteLine("Music Player will initialize automatically");
            };

            // Subscribe to GameMode changes to update button styles
            GameSettings.GameMode.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GameSettings.GameMode.Value))
                {
                    UpdateGameModeButtonStyles();
                }
            };

            // Save settings when window is closing
            this.Closing += (s, e) =>
            {
                SaveGameSettings();
                UnsubscribeFromGameEvents();
                gameEngine?.Dispose();

                // Close debug windows if open
                debugWindow?.Close();
                firebaseEditorWindow?.Close();
                firebaseEditorMVVMWindow?.Close();
                sandboxWindow?.Close();
            };
        }

        private async Task InitializeSessionAsync()
        {
            // Check if default config file exists before creating session (which loads/creates config)
            bool hadExistingConfig = ConfigManager.DefaultConfigFileExists();

            currentSession = new Session();

            // Initialize Firebase leaderboards
            if (firebaseConnector != null)
            {
                await currentSession.InitializeFirebaseAsync(firebaseConnector);
            }

            // Register the session with the App for automatic config saving on exit
            App.CurrentSession = currentSession;

            // Load the current configurations into the UI, passing whether we had existing config
            LoadGameSettings(hadExistingConfig);

            System.Diagnostics.Debug.WriteLine("New session started and registered with App");
        }

        private void InitializeFirebase()
        {
            try
            {
                firebaseConnector = new FireBaseConnector();
                System.Diagnostics.Debug.WriteLine("Firebase connector initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize Firebase connector: {ex.Message}");
                // Continue without Firebase - game will still work
            }
        }

        private void InitializeGameEngine()
        {
            gameEngine = new GameEngine();
            gameEngine.Initialize(GameCanvas, currentSession, firebaseConnector);

            // Subscribe to game engine events
            gameEngine.UIUpdateRequested += OnGameEngineUIUpdateRequested;
            gameEngine.FPSUpdateRequested += OnGameEngineFPSUpdateRequested;
            gameEngine.GameStopped += OnGameEngineStopped;

            // Use CompositionTarget.Rendering for smooth game loop
            CompositionTarget.Rendering += (s, e) => gameEngine.Update();

            DebugHelper.WriteLine("Game Engine initialization completed");
            DebugHelper.WriteLine($"Player name: {Session.PlayerName}");
        }

        #endregion

        #region Game Control

        private void StartGame()
        {
            if (gameEngine.IsGameRunning) return;

            // Hide both screens when game starts
            StartScreen.Visibility = Visibility.Hidden;
            GameOverScreen.Visibility = Visibility.Hidden;
            gameOverScreenVisible = false;

            // Update session's game config with current UI settings and save as default
            var currentUIConfig = SettingsManager.ToGameConfig();
            currentSession.UpdateGameConfig(currentUIConfig, true); // Save as new default

            // Start the game through the engine
            gameEngine.StartGame();

            // Show that settings are locked during game
            GameEvents.RaiseMessageRequested("Settings locked during game (saved as default)", Brushes.Yellow);
        }

        #endregion

        #region Game Engine Event Handlers

        private void OnGameEngineUIUpdateRequested(GameUIData uiData)
        {
            // Update particle count
            ParticleCountText.Text = $"Particles: {uiData.ParticleCount}";

            if (gameEngine.IsGameRunning)
            {
                // Update game time display
                string gameTimeDisplay = $"Time: {uiData.GameTime.TotalSeconds:F0}s";
                gameTimeDisplay += $" | {uiData.LevelStatus}";

                GameTimeText.Text = gameTimeDisplay;

                // Show active power-up effects
                if (uiData.TimeWarpRemaining.HasValue)
                {
                    GameTimeText.Text += $" | TimeWarp: {uiData.TimeWarpRemaining.Value:F1}s";
                }

                // Show super boost status
                if (uiData.IsSuperBoostActive)
                {
                    GameTimeText.Text += " | SUPER BOOST!";
                }

                // Show stored power-ups
                if (uiData.SingularityCount > 0 || uiData.RepulsorCount > 0)
                {
                    GameTimeText.Text += " |";
                    if (uiData.SingularityCount > 0)
                    {
                        GameTimeText.Text += $" Singularity: {uiData.SingularityCount}";
                    }
                    if (uiData.RepulsorCount > 0)
                    {
                        GameTimeText.Text += $" Repulsor: {uiData.RepulsorCount}";
                    }
                }

                // Update session stats
                if (SessionStatsText != null)
                {
                    SessionStatsText.Text = uiData.SessionStats;
                }
            }
        }

        private void OnGameEngineFPSUpdateRequested(double fps)
        {
            FpsText.Text = $"FPS: {fps:F0}";

            // Change color based on FPS performance
            FpsText.Foreground = fps switch
            {
                >= 55 => Brushes.LimeGreen,
                >= 45 => Brushes.Yellow,
                >= 30 => Brushes.Orange,
                _ => Brushes.Red
            };
        }

        private async void OnGameEngineStopped()
        {
            await ShowGameOverScreenAsync();
        }

        #endregion

        #region Game Events

        /// <summary>
        /// Subscribe to all relevant GameEvents
        /// </summary>
        private void SubscribeToGameEvents()
        {
            // UI events
            GameEvents.MessageRequested += UpdateMessage;

            // Configuration events
            GameEvents.ConfigurationSaved += OnConfigurationSaved;

            // Screen navigation events
            GameEvents.ShowStartScreen += ShowStartScreen;
            GameEvents.ShowGameOverScreen += async () => await ShowGameOverScreenAsync();
            GameEvents.ShowConfigScreen += ShowGameConfigScreen;
            GameEvents.HideConfigScreen += HideGameConfigScreen;
        }

        /// <summary>
        /// Unsubscribe from all GameEvents to prevent memory leaks
        /// </summary>
        private void UnsubscribeFromGameEvents()
        {
            // UI events
            GameEvents.MessageRequested -= UpdateMessage;

            // Configuration events
            GameEvents.ConfigurationSaved -= OnConfigurationSaved;

            // Screen navigation events
            GameEvents.ShowStartScreen -= ShowStartScreen;
            GameEvents.ShowConfigScreen -= ShowGameConfigScreen;
            GameEvents.HideConfigScreen -= HideGameConfigScreen;
        }

        private void OnConfigurationSaved()
        {
            GameEvents.RaiseMessageRequested("Configuration saved successfully!", Brushes.LightGreen);
        }

        #endregion

        #region Input Handling

        // Key event handlers for ship movement
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle escape key for leaderboard overlay
            if (e.Key == Key.Escape && LeaderboardOverlay.Visibility == Visibility.Visible)
            {
                LeaderboardOverlay.Visibility = Visibility.Collapsed;
                SurvivalLeaderboard.Visibility = Visibility.Collapsed;
                MazeLeaderboard.Visibility = Visibility.Collapsed;
                e.Handled = true;
                return;
            }

            // Handle escape key for GameConfigScreen
            if (e.Key == Key.Escape && GameConfigScreen.Visibility == Visibility.Visible)
            {
                GameEvents.RaiseHideConfigScreen();
                e.Handled = true;
                return;
            }

            // Check if a text input control has focus - if so, don't handle movement keys
            if (IsTextInputControlFocused())
            {
                // Only handle non-text keys like Space and Escape
                switch (e.Key)
                {
                    case Key.Space:
                        if (gameOverScreenVisible)
                        {
                            // Transition from game over screen to start screen
                            GameEvents.RaiseShowStartScreen();
                        }
                        else if (!gameEngine.IsGameRunning)
                        {
                            StartGame();
                        }
                        e.Handled = true;
                        break;
                }
                return; // Don't handle movement keys when text input has focus
            }

            switch (e.Key)
            {
                case Key.Space:
                    if (gameOverScreenVisible)
                    {
                        // Transition from game over screen to start screen
                        GameEvents.RaiseShowStartScreen();
                    }
                    else if (!gameEngine.IsGameRunning)
                    {
                        StartGame();
                    }
                    else if (gameEngine.IsGameRunning)
                    {
                        // Let the game engine handle the key
                        if (gameEngine.HandleKeyDown(e.Key))
                            e.Handled = true;
                    }
                    break;
                default:
                    // For all other keys, let the game engine handle them if game is running
                    if (gameEngine.IsGameRunning && gameEngine.HandleKeyDown(e.Key))
                    {
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // Check if a text input control has focus - if so, don't handle movement keys
            if (IsTextInputControlFocused())
            {
                return; // Don't handle movement keys when text input has focus
            }

            // Let the game engine handle key up events
            if (gameEngine.HandleKeyUp(e.Key))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Check if a text input control currently has focus
        /// </summary>
        /// <returns>True if a text input control has focus</returns>
        private bool IsTextInputControlFocused()
        {
            var focusedElement = FocusManager.GetFocusedElement(this);

            return focusedElement is TextBox ||
                   focusedElement is PasswordBox ||
                   focusedElement is RichTextBox ||
                   focusedElement is ComboBox ||
                   (focusedElement is Control control && control.IsTabStop && control.Focusable);
        }

        #endregion

        #region Canvas Events

        // Canvas event handler for focus management and power-up activation
        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Make the canvas take focus when clicked
            GameCanvas.Focus();

            // Let the game engine handle the mouse click
            gameEngine.HandleCanvasMouseClick(e);

            e.Handled = true;
        }

        #endregion

        #region Settings Management

        private void PlayerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the static player name when the TextBox changes
            if (sender is TextBox textBox)
            {
                Session.PlayerName = string.IsNullOrWhiteSpace(textBox.Text) ? "Anonymous" : textBox.Text.Trim();

                // Auto-save the player name when it changes
                SaveAppConfig();
            }
        }

        //App start and close
        private void LoadGameSettings(bool hadExistingConfig = true)
        {
            try
            {
                // Load game settings from the session
                if (currentSession?.GameConfig != null)
                {
                    SettingsManager.GameSettings.LoadFromConfig(currentSession.GameConfig);

                    // Determine appropriate message based on whether we had an existing config
                    if (!hadExistingConfig || currentSession.GameConfig.ConfigName == "Default Configuration")
                    {
                        GameEvents.RaiseMessageRequested("Initialized with default settings", Brushes.LightBlue);
                        DebugHelper.WriteLine("UI initialized with default game settings");
                    }
                    else
                    {
                        GameEvents.RaiseMessageRequested($"Settings loaded: {currentSession.GameConfig.ConfigName}", Brushes.LightGreen);
                        DebugHelper.WriteLine($"UI loaded with saved settings: '{currentSession.GameConfig.ConfigName}'");
                        DebugHelper.WriteLine($"Config path: {ConfigManager.GetDefaultConfigFilePath()}");
                    }
                }
                else
                {
                    // Fallback: Initialize with defaults if no config available
                    SettingsManager.InitializeWithDefaults();
                    GameEvents.RaiseMessageRequested("Using default settings", Brushes.LightBlue);
                    DebugHelper.WriteLine("UI fallback to default settings");
                }

                // App config is now loaded by the Session class automatically
                if (currentSession?.AppConfig != null)
                {
                    DebugHelper.WriteLine($"App config loaded automatically by Session");
                    DebugHelper.WriteLine($"App config path: {ConfigManager.GetAppConfigFilePath()}");
                }

                // Update player name TextBox if it exists
                if (PlayerNameTextBox != null)
                {
                    PlayerNameTextBox.Text = Session.PlayerName;
                }
            }
            catch (Exception ex)
            {
                // On any error, ensure we have default settings loaded
                SettingsManager.InitializeWithDefaults();
                GameEvents.RaiseMessageRequested($"Error loading config, using defaults: {ex.Message}", Brushes.LightCoral);
                DebugHelper.WriteLine($"Error loading game settings: {ex.Message}");
            }
        }

        private void SaveGameSettings()
        {
            try
            {
                // Update the session's game config with current UI settings
                var currentConfig = SettingsManager.ToGameConfig();
                currentSession?.UpdateGameConfig(currentConfig, true); // Save as new default

                GameEvents.RaiseMessageRequested("Settings saved as new default", Brushes.LightGreen);
                DebugHelper.WriteLine($"Game settings auto-saved as default to: {ConfigManager.GetDefaultConfigFilePath()}");
                DebugHelper.WriteLine($"App config auto-saved to: {ConfigManager.GetAppConfigFilePath()}");
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error saving settings: {ex.Message}", Brushes.LightCoral);
                DebugHelper.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save application configuration including player name
        /// This is now handled by the Session class, but kept for compatibility
        /// </summary>
        private void SaveAppConfig()
        {
            try
            {
                // Session handles this automatically when SaveConfigurations is called
                currentSession?.SaveConfigurations();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveAppConfig: {ex.Message}");
            }
        }

        private void UpdateMessage(string message, Brush color)
        {
            if (ConfigStatusText != null)
            {
                ConfigStatusText.Text = message;
                ConfigStatusText.Foreground = color;

                // Clear the message after 3 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (ConfigStatusText != null)
                    {
                        ConfigStatusText.Text = "";
                    }
                };
                timer.Start();
            }
        }

        // Settings Management Button Event Handlers
        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show confirmation dialog
                var result = MessageBox.Show(
                    "Are you sure you want to reset all settings to their default values?",
                    "Reset Settings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Reset to default settings using the new system
                    SettingsManager.ResetToDefaults();
                    GameEvents.RaiseMessageRequested("Settings reset to defaults", Brushes.Orange);
                    DebugHelper.WriteLine("Settings reset to default values by user");
                }
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error resetting settings: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in ResetSettingsButton_Click: {ex.Message}");
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Use GameEvents to show the GameConfigScreen
                GameEvents.RaiseShowConfigScreen();
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error opening save screen: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in SaveSettingsButton_Click: {ex.Message}");
            }
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loadedSettings = SettingsManager.LoadSettings();

                if (loadedSettings != null)
                {
                    // Validate and apply the loaded settings using the new system
                    var validated = SettingsManager.ValidateSavedOrLoadedSettings(loadedSettings);
                    SettingsManager.FromGameConfig(validated);

                    // Update the session config with the loaded settings
                    currentSession?.UpdateGameConfig(validated, true);

                    GameEvents.RaiseMessageRequested("Settings loaded successfully", Brushes.LightGreen);
                    DebugHelper.WriteLine($"Settings loaded from file: {loadedSettings.ConfigName}");
                }
                else
                {
                    GameEvents.RaiseMessageRequested("Load canceled or failed", Brushes.LightCoral);
                    DebugHelper.WriteLine("Settings load was canceled or failed");
                }
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error loading settings: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in LoadSettingsButton_Click: {ex.Message}");
            }
        }

        #endregion

        #region Screen Management

        /// <summary>
        /// Show the game over screen with current game and session data
        /// </summary>
        private async Task ShowGameOverScreenAsync()
        {
            try
            {
                gameOverScreenVisible = true;

                // Hide start screen and show game over screen
                StartScreen.Visibility = Visibility.Hidden;
                GameOverScreen.Visibility = Visibility.Visible;

                // Initialize the game over screen with current data
                await GameOverScreen.InitializeAsync(gameEngine.CurrentGame, gameEngine.CurrentSession, firebaseConnector);

                System.Diagnostics.Debug.WriteLine("Game over screen displayed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing game over screen: {ex.Message}");
                // Fallback to showing start screen
                ShowStartScreen();
            }
        }

        /// <summary>
        /// Show the start screen and hide other screens
        /// </summary>
        private void ShowStartScreen()
        {
            gameOverScreenVisible = false;

            // Show start screen and hide game over screen
            StartScreen.Visibility = Visibility.Visible;
            GameOverScreen.Visibility = Visibility.Hidden;

            System.Diagnostics.Debug.WriteLine("Start screen displayed");
        }

        /// <summary>
        /// Show the GameConfigScreen overlay
        /// </summary>
        private void ShowGameConfigScreen()
        {
            try
            {
                // Get current settings
                var currentSettings = SettingsManager.ToGameConfig();

                // Initialize and show the GameConfigScreen
                GameConfigScreen.Initialize(currentSettings);
                GameConfigScreen.Visibility = Visibility.Visible;

                // Focus on the GameConfigScreen
                GameConfigScreen.Focus();

                System.Diagnostics.Debug.WriteLine("GameConfigScreen shown");
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error showing config screen: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in ShowGameConfigScreen: {ex.Message}");
            }
        }

        /// <summary>
        /// Hide the GameConfigScreen overlay
        /// </summary>
        private void HideGameConfigScreen()
        {
            try
            {
                GameConfigScreen.Visibility = Visibility.Hidden;

                // Return focus to main window
                this.Focus();

                System.Diagnostics.Debug.WriteLine("GameConfigScreen hidden");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HideGameConfigScreen: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle the back button or escape key from GameConfigScreen
        /// </summary>
        private void GameConfigScreen_BackRequested(object sender, EventArgs e)
        {
            GameEvents.RaiseHideConfigScreen();
        }

        /// <summary>
        /// Handle when a configuration is saved from GameConfigScreen
        /// </summary>
        private void GameConfigScreen_ConfigSaved(object sender, GameConfig e)
        {
            try
            {
                GameEvents.RaiseConfigurationSaved();
                System.Diagnostics.Debug.WriteLine($"Configuration saved: {e.ConfigName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GameConfigScreen_ConfigSaved: {ex.Message}");
            }
        }

        #endregion

        #region Debug and Utility Windows

        /// <summary>
        /// Open or focus the debug log window
        /// </summary>
        private void LogButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (debugWindow == null || !debugWindow.IsLoaded)
                {
                    // Create new debug window
                    debugWindow = new DebugWindow();

                    // Handle window closed event
                    debugWindow.Closed += (s, args) => debugWindow = null;

                    debugWindow.Show();

                    // Add a welcome message using our debug helper
                    DebugHelper.WriteLine("Debug window opened - all debug output will appear here");
                    DebugHelper.WriteLine($"Application started at: {DateTime.Now}");
                    DebugHelper.WriteLine("=" + new string('=', 50));
                }
                else
                {
                    // Window exists, just bring it to front
                    debugWindow.Activate();
                    debugWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open debug window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DebugHelper.WriteLine($"Error opening debug window: {ex.Message}");
            }
        }

        /// <summary>
        /// Open or focus the Firebase database editor window
        /// </summary>
        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (firebaseEditorWindow == null || !firebaseEditorWindow.IsLoaded)
                {
                    // Create new Firebase editor window
                    firebaseEditorWindow = new FireBaseEditor
                    {
                        Owner = this
                    };

                    // Handle window closed event
                    firebaseEditorWindow.Closed += (s, args) => firebaseEditorWindow = null;

                    firebaseEditorWindow.Show();

                    DebugHelper.WriteLine("Firebase Database Editor opened");
                }
                else
                {
                    // Window exists, just bring it to front
                    firebaseEditorWindow.Activate();
                    firebaseEditorWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Firebase Database Editor: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DebugHelper.WriteLine($"Error opening Firebase Database Editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Open or focus the MVVM Firebase database editor window
        /// </summary>
        private void DatabaseMVVMButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (firebaseEditorMVVMWindow == null || !firebaseEditorMVVMWindow.IsLoaded)
                {
                    // Create new MVVM Firebase editor window
                    firebaseEditorMVVMWindow = new FirebaseEditorWindow_MVVM
                    {
                        Owner = this
                    };

                    // Handle window closed event
                    firebaseEditorMVVMWindow.Closed += (s, args) => firebaseEditorMVVMWindow = null;

                    firebaseEditorMVVMWindow.Show();

                    DebugHelper.WriteLine("Firebase Database Editor (MVVM) opened");
                }
                else
                {
                    // Window exists, just bring it to front
                    firebaseEditorMVVMWindow.Activate();
                    firebaseEditorMVVMWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Firebase Database Editor (MVVM): {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DebugHelper.WriteLine($"Error opening Firebase Database Editor (MVVM): {ex.Message}");
            }
        }

        /// <summary>
        /// Open or focus the Sandbox window
        /// </summary>
        private void SandboxButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sandboxWindow == null || !sandboxWindow.IsLoaded)
                {
                    // Create new Sandbox window
                    sandboxWindow = new UI.SandboxWindow
                    {
                        Owner = this
                    };

                    // Handle window closed event
                    sandboxWindow.Closed += (s, args) => sandboxWindow = null;

                    sandboxWindow.Show();

                    DebugHelper.WriteLine("Sandbox window opened");
                }
                else
                {
                    // Window exists, just bring it to front
                    sandboxWindow.Activate();
                    sandboxWindow.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Sandbox window: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DebugHelper.WriteLine($"Error opening Sandbox window: {ex.Message}");
            }
        }

        #endregion

        #region Leaderboard Management

        /// <summary>
        /// Show the Survival Mode leaderboard
        /// </summary>
        private void SurvivalLeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentSession?.LeaderboardsLoaded == true)
                {
                    // Initialize the Survival leaderboard
                    SurvivalLeaderboard.Initialize(currentSession.SurvivalLeaderboard);
                    
                    // Show the leaderboard overlay with only Survival visible
                    MazeLeaderboard.Visibility = Visibility.Collapsed;
                    SurvivalLeaderboard.Visibility = Visibility.Visible;
                    LeaderboardOverlay.Visibility = Visibility.Visible;
                    
                    DebugHelper.WriteLine("Survival leaderboard displayed");
                }
                else
                {
                    GameEvents.RaiseMessageRequested("Leaderboards not loaded yet", Brushes.Orange);
                    DebugHelper.WriteLine("Attempted to show Survival leaderboard but leaderboards not loaded");
                }
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error showing Survival leaderboard: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in SurvivalLeaderboardButton_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Show the Maze Mode leaderboard
        /// </summary>
        private void MazeLeaderboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentSession?.LeaderboardsLoaded == true)
                {
                    // Initialize the Maze leaderboard
                    MazeLeaderboard.Initialize(currentSession.MazeLeaderboard);
                    
                    // Show the leaderboard overlay with only Maze visible
                    SurvivalLeaderboard.Visibility = Visibility.Collapsed;
                    MazeLeaderboard.Visibility = Visibility.Visible;
                    LeaderboardOverlay.Visibility = Visibility.Visible;
                    
                    DebugHelper.WriteLine("Maze leaderboard displayed");
                }
                else
                {
                    GameEvents.RaiseMessageRequested("Leaderboards not loaded yet", Brushes.Orange);
                    DebugHelper.WriteLine("Attempted to show Maze leaderboard but leaderboards not loaded");
                }
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error showing Maze leaderboard: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in MazeLeaderboardButton_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Hide the leaderboard overlay when clicking outside the leaderboard
        /// </summary>
        private void LeaderboardOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only hide if clicking directly on the overlay (not on child elements)
            if (e.Source == LeaderboardOverlay)
            {
                LeaderboardOverlay.Visibility = Visibility.Collapsed;
                SurvivalLeaderboard.Visibility = Visibility.Collapsed;
                MazeLeaderboard.Visibility = Visibility.Collapsed;
                
                DebugHelper.WriteLine("Leaderboard overlay hidden");
            }
        }

        #endregion
        
        #region Game Mode Selection

        private void SurvivalModeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set the game mode to Survival
                GameSettings.GameMode.Value = Settings.Models.GameMode.Survival;
                SettingsManager.FromGameConfig(ConfigManager.CreateConfigForGameMode(Settings.Models.GameMode.Survival));

                // Show feedback message
                GameEvents.RaiseMessageRequested("Survival Mode activated - Level mechanics disabled", Brushes.Orange);
                DebugHelper.WriteLine("User selected Survival game mode");
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error setting Survival mode: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in SurvivalModeButton_Click: {ex.Message}");
            }
        }

        private void MazeModeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set the game mode to Maze
                GameSettings.GameMode.Value = Settings.Models.GameMode.Maze;

                SettingsManager.FromGameConfig(ConfigManager.CreateConfigForGameMode(Settings.Models.GameMode.Maze));

                // Show feedback message
                GameEvents.RaiseMessageRequested("Maze Mode activated - No particles, no power-ups", Brushes.Orange);
                DebugHelper.WriteLine("User selected Maze game mode");
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error setting Maze mode: {ex.Message}", Brushes.Red);
                DebugHelper.WriteLine($"Error in MazeModeButton_Click: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the visual style of game mode buttons based on current selection
        /// </summary>
        private void UpdateGameModeButtonStyles()
        {
            try
            {
                var currentGameMode = GameSettings.GameMode.Value;

                // Reset all buttons to default style
                var defaultColor = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)); // Default dark gray
                SurvivalModeButton.Background = defaultColor;
                MazeModeButton.Background = defaultColor;

                // Set active button to purple
                switch (currentGameMode)
                {
                    case Settings.Models.GameMode.Survival:
                        SurvivalModeButton.Background = Brushes.MediumPurple;
                        break;
                    case Settings.Models.GameMode.Maze:
                        MazeModeButton.Background = Brushes.MediumPurple;
                        break;
                }

                DebugHelper.WriteLine($"Updated game mode button styles for: {currentGameMode}");
            }
            catch (Exception ex)
            {
                DebugHelper.WriteLine($"Error updating game mode button styles: {ex.Message}");
            }
        }

        #endregion
    }
}