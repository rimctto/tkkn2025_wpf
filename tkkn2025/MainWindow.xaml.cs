using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects;
using tkkn2025.GameObjects.PowerUps;
using tkkn2025.GameObjects.Ship;
using tkkn2025.Settings;
using tkkn2025.DataAccess;
using tkkn2025.UI.Windows;
using tkkn2025.Helpers;

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Game objects
        private ShipSprite ship = null!;
        private ParticleManager particleManager = null!;
        private PowerUpManager powerUpManager = null!;
        private Random random = null!;
        
        // Game state tracking
        private bool gameRunning = false;
        private bool gameOverScreenVisible = false;
        private bool[] keysPressed = new bool[4]; // Up, Down, Left, Right
        private Point centerScreen;
        private Point shipPosition;
        
        // Game loop timing
        private DateTime lastUpdate = DateTime.Now;
        private DateTime lastParticleGeneration = DateTime.Now;
        private DateTime gameStartTime;

        // Settings Manager for MVVM data binding
        
        public SettingsManager SettingsManager { get ; set ; } = new SettingsManager();
        

        // Active game settings - snapshot taken when game starts
        private double activeShipSpeed;
        private double activeLevelDuration;
        private double activeNewParticlesPerLevel;
        
        // Power-up effects
        private double currentSpeedMultiplier = 1.0;
        
        // Session management
        private Session currentSession = null!;
        private Game? currentGame = null;
        
        // Audio
        AudioManager audioManager = new AudioManager();
        
        // UI Update timing
        private DateTime lastUIUpdate = DateTime.Now;
        private DateTime lastFPSUpdate = DateTime.Now;
        private int frameCount = 0;

        // Firebase connector for saving game data
        private FireBaseConnector firebaseConnector = null!;

        // Debug window
        private DebugWindow? debugWindow = null;
        
        // Firebase editor window
        private FireBaseEditor? firebaseEditorWindow = null;

        public MainWindow()
        {

            DataContext = SettingsManager;
            InitializeComponent();

            InitializeSession();
            InitializeFirebase();
            SubscribeToGameEvents();

            // Ensure window can receive keyboard input
            this.Loaded += (s, e) => {
                this.Focus();
                UpdateMusicButtonText(); // Update button text when window is loaded
                
                // Initialize player name TextBox after controls are loaded
                PlayerNameTextBox.Text = Session.PlayerName;
                PlayerNameTextBox.TextChanged += PlayerNameTextBox_TextChanged;
            };
            
            // Save settings when window is closing
            this.Closing += (s, e) => {
                SaveGameSettings();
                UnsubscribeFromGameEvents();
                
                // Close debug window if open
                if (debugWindow != null)
                {
                    debugWindow.Close();
                    debugWindow = null;
                }
                
                // Close firebase editor window if open
                if (firebaseEditorWindow != null)
                {
                    firebaseEditorWindow.Close();
                    firebaseEditorWindow = null;
                }
            };
            
            InitializeGame();
        }

        #region GameEvents Subscription Management
        
        /// <summary>
        /// Subscribe to all relevant GameEvents
        /// </summary>
        private void SubscribeToGameEvents()
        {
            // Game state events
            GameEvents.CollisionDetected += OnParticleShipCollisionDetected;
            GameEvents.GameCompleted += OnGameCompleted;
            
            // Power-up events
            GameEvents.PowerUpCollected += OnPowerUpCollected;
            GameEvents.PowerUpEffectStarted += OnPowerUpEffectStarted;
            GameEvents.PowerUpEffectEnded += OnPowerUpEffectEnded;
            GameEvents.PowerUpStored += OnPowerUpStored;
            GameEvents.SingularityActivated += OnSingularityActivated;
            
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
            // Game state events
            GameEvents.CollisionDetected -= OnParticleShipCollisionDetected;
            GameEvents.GameCompleted -= OnGameCompleted;
            
            // Power-up events
            GameEvents.PowerUpCollected -= OnPowerUpCollected;
            GameEvents.PowerUpEffectStarted -= OnPowerUpEffectStarted;
            GameEvents.PowerUpEffectEnded -= OnPowerUpEffectEnded;
            GameEvents.PowerUpStored -= OnPowerUpStored;
            GameEvents.SingularityActivated -= OnSingularityActivated;
            
            // UI events
            GameEvents.MessageRequested -= UpdateMessage;
            
            // Configuration events
            GameEvents.ConfigurationSaved -= OnConfigurationSaved;
            
            // Screen navigation events
            GameEvents.ShowStartScreen -= ShowStartScreen;
            GameEvents.ShowConfigScreen -= ShowGameConfigScreen;
            GameEvents.HideConfigScreen -= HideGameConfigScreen;
        }
        
        #endregion

        private void InitializeSession()
        {
            // Check if default config file exists before creating session (which loads/creates config)
            bool hadExistingConfig = ConfigManager.DefaultConfigFileExists();
            
            currentSession = new Session();
            
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

                // Load music setting from app config
                if (currentSession?.AppConfig != null)
                {
                    audioManager.MusicEnabled = currentSession.AppConfig.MusicEnabled;
                    DebugHelper.WriteLine($"Music setting loaded from app config: {currentSession.AppConfig.MusicEnabled}");
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
                
                // Update app config with current music setting
                if (currentSession?.AppConfig != null)
                {
                    currentSession.AppConfig.MusicEnabled = audioManager.MusicEnabled;
                }
                
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
        /// Load application configuration including player name
        /// This is now handled by the Session class, but kept for compatibility
        /// </summary>
        private void LoadAppConfig()
        {
            try
            {
                // Session handles this automatically now
                System.Diagnostics.Debug.WriteLine($"App config loaded via session. Player name: {Session.PlayerName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadAppConfig: {ex.Message}");
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
        
        private void InitializeGame()
        {
            audioManager.Initialize();
            
            // Set initial music state from app config after session is loaded
            if (currentSession?.AppConfig != null)
            {
                audioManager.MusicEnabled = currentSession.AppConfig.MusicEnabled;
                UpdateMusicButtonText();
            }

            random = new Random();
            
            // Initialize particle controller - no direct event wiring needed
            particleManager = new ParticleManager(GameCanvas);
            
            // Initialize power-up manager - no direct event wiring needed
            powerUpManager = new PowerUpManager(GameCanvas, random);
            
            // Use CompositionTarget.Rendering for smooth game loop
            CompositionTarget.Rendering += GameLoop;

            CreateShip();
            
            // Debug initialization info
            DebugHelper.WriteLine("Game initialization completed");
            DebugHelper.WriteLine($"Audio Manager initialized, Music enabled: {audioManager.MusicEnabled}");
            DebugHelper.WriteLine($"Player name: {Session.PlayerName}");
        }
        
        private async void OnParticleShipCollisionDetected()
        {
            StopGame();
            
            await ShowGameOverScreenAsync();
        }

        private void OnPowerUpCollected(string powerUpType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up collected: {powerUpType}");
            if (powerUpType == "Singularity")
            {
                GameEvents.RaiseMessageRequested($"Singularity stored! Click to activate ({powerUpManager.GetStoredPowerUpCount("Singularity")})", Brushes.Purple);
            }
            else if (powerUpType == "Repulsor")
            {
                GameEvents.RaiseMessageRequested($"Repulsor stored! Right-click to activate ({powerUpManager.GetStoredPowerUpCount("Repulsor")})", Brushes.Green);
            }
            else
            {
                GameEvents.RaiseMessageRequested($"Collected {powerUpType}!", Brushes.Gold);
            }
        }

        private void OnPowerUpEffectStarted(string effectType, double duration)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up effect started: {effectType} for {duration} seconds");
            GameEvents.RaiseMessageRequested($"{effectType} activated for {duration:F1}s!", Brushes.CornflowerBlue);
        }

        private void OnPowerUpEffectEnded(string effectType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up effect ended: {effectType}");
            GameEvents.RaiseMessageRequested($"{effectType} effect ended", Brushes.LightGray);
        }

        private void OnPowerUpStored(string powerUpType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up stored: {powerUpType}");
            int count = powerUpManager.GetStoredPowerUpCount(powerUpType);
            GameEvents.RaiseMessageRequested($"{powerUpType} stored! Total: {count}", Brushes.MediumPurple);
        }

        private void OnSingularityActivated(Vector2 position)
        {
            System.Diagnostics.Debug.WriteLine($"Singularity activated at {position}");
            GameEvents.RaiseMessageRequested("Singularity created! Gravity well active for 5 seconds", Brushes.DarkViolet);
        }

        private void OnGameCompleted(Game game)
        {
            // Save game data to Firebase
            _ = SaveGameToFirebase(game);
        }

        private void OnConfigurationSaved()
        {
            GameEvents.RaiseMessageRequested("Configuration saved successfully!", Brushes.LightGreen);
        }
      
        private void CreateShip()
        {
            ship = new ShipSprite();
            
            // Position ship in center of game area (wait for canvas to load)
            this.Loaded += (s, e) => {
                centerScreen = new Point(GameCanvas.ActualWidth / 2, GameCanvas.ActualHeight / 2);
                shipPosition = centerScreen;
                Canvas.SetLeft(ship, shipPosition.X - ship.Width / 2);
                Canvas.SetTop(ship, shipPosition.Y - ship.Height / 2);
                
                // Update particle controller with canvas dimensions
                ParticleManager.UpdateCanvasDimensions();
                
                // Update power-up manager with canvas dimensions
                powerUpManager.UpdateCanvasDimensions();
         
            };
            
            GameCanvas.Children.Add(ship);
        }
     
        
        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameRunning) return;

            var now = DateTime.Now;
            var deltaTime = (now - lastUpdate).TotalSeconds;
            lastUpdate = now;
            
            // Limit delta time to prevent huge jumps
            deltaTime = Math.Min(deltaTime, 1.0 / 30.0); // Max 30 FPS equivalent
            
            // Update current speed multiplier from power-ups
            currentSpeedMultiplier = powerUpManager.GetSpeedMultiplier();
            
            // Apply speed multiplier to delta time for time-based effects
            var effectiveDeltaTime = deltaTime * currentSpeedMultiplier;
            
            UpdateFPS();
            UpdateShipPosition(effectiveDeltaTime);
            
            // Update power-ups (use normal delta time for power-up timing)
            var shipVector = new Vector2((float)shipPosition.X, (float)shipPosition.Y);
            powerUpManager.Update(deltaTime, shipVector);
            
            // Check power-up collisions
            powerUpManager.CheckCollisions(shipPosition);
            
            // Update particles through controller (with speed multiplier and power-up manager)
            particleManager.UpdateParticles(effectiveDeltaTime, shipPosition, powerUpManager);
            
            // Check collisions through controller
            particleManager.CheckCollisions(shipPosition);
            
            // Handle particle generation timing (use normal delta time for consistent spawning)
            if ((now - lastParticleGeneration).TotalSeconds >= activeLevelDuration)
            {
                particleManager.GenerateMoreParticles(activeNewParticlesPerLevel);
                lastParticleGeneration = now;
            }
            
            // Update UI periodically (not every frame)
            if ((now - lastUIUpdate).TotalSeconds >= 0.1) // 10 times per second
            {
                UpdateUI();
                lastUIUpdate = now;
            }
        }
        
        private void StartGame()
        {
            if (gameRunning) return;

            audioManager.SetVolume(1);

            gameRunning = true;
            gameOverScreenVisible = false;
            gameStartTime = DateTime.Now;
            lastUpdate = DateTime.Now;
            lastParticleGeneration = DateTime.Now;
            lastUIUpdate = DateTime.Now;
            lastFPSUpdate = DateTime.Now;
            frameCount = 0;
            currentSpeedMultiplier = 1.0;
            
            // Hide both screens when game starts
            StartScreen.Visibility = Visibility.Hidden;
            GameOverScreen.Visibility = Visibility.Hidden;
            
            // Update canvas dimensions for all managers at the start of each game
            ParticleManager.UpdateCanvasDimensions();
            powerUpManager.UpdateCanvasDimensions();
            
            // Update center screen position based on current canvas size
            centerScreen = new Point(GameCanvas.ActualWidth / 2, GameCanvas.ActualHeight / 2);
            
            // Update session's game config with current UI settings and save as default
            var currentUIConfig = SettingsManager.ToGameConfig();
            currentSession.UpdateGameConfig(currentUIConfig, true); // Save as new default
            
            // Snapshot current settings as active settings for this game
            activeShipSpeed = GameSettings.ShipSpeed.Value;
            activeLevelDuration = GameSettings.LevelDuration.Value;
            activeNewParticlesPerLevel = GameSettings.NewParticlesPerLevel.Value;

            // Initialize particle controller with game settings
            ParticleManager.InitializeGameSettings();
            
            // Start a new game in the session (this creates a copy of the current config)
            currentGame = currentSession.StartNewGame();
            
            // Reset all key states to prevent ship from moving automatically
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            // Reset ship position to current center and show neutral
            shipPosition = centerScreen;
            Canvas.SetLeft(ship, shipPosition.X - ship.Width / 2);
            Canvas.SetTop(ship, shipPosition.Y - ship.Height / 2);
            ship.ShowNeutral();
            
            // Start new game through particle controller
            particleManager.StartNewGame();
            
            // Start new game through power-up manager
            powerUpManager.StartNewGame();
            
            // Raise game started event
            GameEvents.RaiseGameStarted();
            
            // Show that settings are locked during game
            GameEvents.RaiseMessageRequested("Settings locked during game (saved as default)", Brushes.Yellow);
            
            // Debug output for game start
            DebugHelper.WriteLine($"Game started with {GameSettings.StartingParticles.Value} particles, Ship Speed: {activeShipSpeed}");
        }
        
        private async void StopGame()
        {
            gameRunning = false;
            
            // Don't show start screen immediately - will be shown after game over screen
            
            // Complete the current game
            if (currentGame != null)
            {
                currentSession.CompleteCurrentGame(particleManager.ParticleCount);
                DebugHelper.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {currentGame.FinalParticleCount} particles");
                DebugHelper.WriteLine($"Session stats: {currentSession.GetSessionStats()}");
                
                // Raise game completed event
                GameEvents.RaiseGameCompleted(currentGame);
            }
            
            // Reset all key states when game stops
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            audioManager.SetVolume(0.5);
            
            // Raise game ended event
            GameEvents.RaiseGameEnded();
            
            // Clear settings locked message
            GameEvents.RaiseMessageRequested("Game ended - settings can be changed", Brushes.LightGreen);
        }

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
                await GameOverScreen.InitializeAsync(currentGame, currentSession, firebaseConnector);
                
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

        private async Task SaveGameToFirebase(Game game)
        {
            if (firebaseConnector == null) return;

            try
            {
                // Create a data object to save to Firebase
                var gameData = new
                {
                    PlayerName = game.PlayerName,
                    StartTime = game.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    EndTime = game.EndTime?.ToString("yyyy-MM-dd HH:mm:ss"),
                    DurationSeconds = game.DurationSeconds,
                    FinalParticleCount = game.FinalParticleCount,
                    Settings = new
                    {
                        ShipSpeed = game.Settings.ShipSpeed,
                        ParticleSpeed = game.Settings.ParticleSpeed,
                        ParticleTurnSpeed = game.Settings.ParticleTurnSpeed,
                        StartingParticles = game.Settings.StartingParticles,
                        LevelDuration = game.Settings.LevelDuration,
                        NewParticlesPerLevel = game.Settings.NewParticlesPerLevel,
                        ParticleSpeedVariance = game.Settings.ParticleSpeedVariance,
                        ParticleRandomizerPercentage = game.Settings.ParticleRandomizerPercentage,
                        IsParticleSpawnVectorTowardsShip = game.Settings.IsParticleSpawnVectorTowardsShip
                    }
                };

                string gameKey = await firebaseConnector.WriteDataAsync("games", gameData);
                System.Diagnostics.Debug.WriteLine($"Game data saved to Firebase with key: {gameKey}");
                
                // Show a brief success message
                GameEvents.RaiseMessageRequested("Game saved to database", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save game data to Firebase: {ex.Message}");
                GameEvents.RaiseMessageRequested("Failed to save game data", Brushes.LightCoral);
            }
        }

        private void UpdateFPS()
        {
            frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - lastFPSUpdate).TotalSeconds;
            
            if (elapsed >= 1.0) // Update FPS every second
            {
                double fps = frameCount / elapsed;
                FpsText.Text = $"FPS: {fps:F0}";

                // Change color based on FPS performance
                FpsText.Foreground = fps switch
                {
                    >= 55 => Brushes.LimeGreen,
                    >= 45 => Brushes.Yellow,
                    >= 30 => Brushes.Orange,
                    _ => Brushes.Red
                };
                
                frameCount = 0;
                lastFPSUpdate = now;
            }
        }
        
        private void UpdateShipPosition(double deltaTime)
        {
            double deltaX = 0, deltaY = 0;
            
            // Calculate movement based on time and speed
            if (keysPressed[0]) deltaY -= activeShipSpeed * deltaTime; // Up
            if (keysPressed[1]) deltaY += activeShipSpeed * deltaTime; // Down
            if (keysPressed[2]) deltaX -= activeShipSpeed * deltaTime; // Left
            if (keysPressed[3]) deltaX += activeShipSpeed * deltaTime; // Right
            
            if (deltaX != 0 || deltaY != 0)
            {
                // Keep ship within bounds
                double newX = Math.Max(20, Math.Min(GameCanvas.ActualWidth - 20, shipPosition.X + deltaX));
                double newY = Math.Max(20, Math.Min(GameCanvas.ActualHeight - 20, shipPosition.Y + deltaY));
                
                shipPosition = new Point(newX, newY);
                Canvas.SetLeft(ship, shipPosition.X - ship.Width / 2);
                Canvas.SetTop(ship, shipPosition.Y - ship.Height / 2);
                
                // Update ship visual based on movement direction
                if (deltaX < 0) // Moving left
                {
                    ship.ShowLeftTilt();
                }
                else if (deltaX > 0) // Moving right
                {
                    ship.ShowRightTilt();
                }
                else // No horizontal movement
                {
                    ship.ShowNeutral();
                }
            }
            else
            {
                // No movement - show neutral position
                ship.ShowNeutral();
            }
        }

        private void UpdateUI()
        {
            ParticleCountText.Text = $"Particles: {particleManager.ParticleCount}";

            if (gameRunning)
            {
                var elapsed = DateTime.Now - gameStartTime;
                GameTimeText.Text = $"Time: {elapsed.TotalSeconds:F0}s";

                // Show active power-up effects
                if (powerUpManager.IsEffectActive("TimeWarp"))
                {
                    var remaining = powerUpManager.GetEffectRemainingTime("TimeWarp");
                    GameTimeText.Text += $" | TimeWarp: {remaining:F1}s";
                }

                // Show stored power-ups
                var singularityCount = powerUpManager.GetStoredPowerUpCount("Singularity");
                var repulsorCount = powerUpManager.GetStoredPowerUpCount("Repulsor");
                if (singularityCount > 0 || repulsorCount > 0)
                {
                    GameTimeText.Text += " |";
                    if (singularityCount > 0)
                    {
                        GameTimeText.Text += $" Singularity: {singularityCount}";
                    }
                    if (repulsorCount > 0)
                    {
                        GameTimeText.Text += $" Repulsor: {repulsorCount}";
                    }
                }

                // Update session stats
                if (currentSession != null && SessionStatsText != null)
                {
                    if (currentSession.GamesPlayed == 0)
                    {
                        SessionStatsText.Text = "No games completed yet";
                    }
                    else
                    {
                        SessionStatsText.Text = $"Games: {currentSession.GamesPlayed} | " +
                                              $"Best: {currentSession.LongestGame?.DurationSeconds:F1}s | " +
                                              $"Avg: {currentSession.AverageGameTime:F1}s";
                    }
                }
            }
        }
        
        // Canvas event handler for focus management
        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Make the canvas take focus when clicked
            GameCanvas.Focus();
            
            if (gameRunning)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // Left click - try to activate singularity at click position
                    var clickPosition = e.GetPosition(GameCanvas);
                    var clickVector = new Vector2((float)clickPosition.X, (float)clickPosition.Y);
                    
                    bool activated = powerUpManager.TryActivateSingularity(clickVector);
                    if (!activated)
                    {
                        GameEvents.RaiseMessageRequested("No Singularity power-up available!", Brushes.Red);
                    }
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    // Right click - try to activate repulsor at ship position
                    var shipVector = new Vector2((float)shipPosition.X, (float)shipPosition.Y);
                    
                    bool activated = powerUpManager.TryActivateRepulsor(shipVector);
                    if (!activated)
                    {
                        GameEvents.RaiseMessageRequested("No Repulsor power-up available!", Brushes.Red);
                    }
                }
            }
            
            e.Handled = true;
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

        private void MusicToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Store the currently focused element before the button click
                var previouslyFocusedElement = FocusManager.GetFocusedElement(this);
                
                // Toggle the music setting in the audio manager
                audioManager.MusicEnabled = !audioManager.MusicEnabled;
                
                // Update the app config
                if (currentSession?.AppConfig != null)
                {
                    currentSession.AppConfig.MusicEnabled = audioManager.MusicEnabled;
                }
                
                // Update button text
                UpdateMusicButtonText();
                
                // Save the settings immediately
                SaveGameSettings();
                
                // Raise music toggled event
                GameEvents.RaiseMusicToggled(audioManager.MusicEnabled);
                
                // Restore focus to the previously focused element or the game canvas
                if (previouslyFocusedElement is UIElement previousElement && previousElement.IsEnabled && previousElement.Focusable)
                {
                    previousElement.Focus();
                }
                else if (gameRunning)
                {
                    // If game is running, ensure the game canvas has focus for key input
                    GameCanvas.Focus();
                }
                else
                {
                    // Return focus to the main window
                    this.Focus();
                }
                
                System.Diagnostics.Debug.WriteLine($"Music toggled to: {audioManager.MusicEnabled}");
            }
            catch (Exception ex)
            {
                GameEvents.RaiseMessageRequested($"Error toggling music: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in MusicToggleButton_Click: {ex.Message}");
            }
        }

        private void UpdateMusicButtonText()
        {
            if (MusicToggleButton != null)
            {
                bool isEnabled = audioManager.MusicEnabled;
                MusicToggleButton.Content = isEnabled ? "🎵 Music: ON" : "🔇 Music: OFF";
                MusicToggleButton.Background = isEnabled ? new SolidColorBrush(Colors.DarkSlateBlue) : new SolidColorBrush(Colors.DarkRed);
            }
        }

        // Key event handlers for ship movement
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
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
                        else if (!gameRunning)
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
                    else if (!gameRunning)
                    {
                        StartGame();
                    }
                    e.Handled = true;
                    break;
                case Key.Up:
                case Key.W:
                    if (gameRunning)
                    {
                        keysPressed[0] = true;
                        e.Handled = true;
                    }
                    break;
                case Key.Down:
                case Key.S:
                    if (gameRunning)
                    {
                        keysPressed[1] = true;
                        e.Handled = true;
                    }
                    break;
                case Key.Left:
                case Key.A:
                    if (gameRunning)
                    {
                        keysPressed[2] = true;
                        e.Handled = true;
                    }
                    break;
                case Key.Right:
                case Key.D:
                    if (gameRunning)
                    {
                        keysPressed[3] = true;
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

            switch (e.Key)
            {
                case Key.Up:
                case Key.W:
                    keysPressed[0] = false;
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = false;
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = false;
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = false;
                    e.Handled = true;
                    break;
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
                    debugWindow = new DebugWindow
                    {
                        //Owner = this
                    };
                    
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
    }
}