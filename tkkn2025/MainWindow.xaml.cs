using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects;
using tkkn2025.GameObjects.PowerUps;
using tkkn2025.Settings;
using tkkn2025.DataAccess;

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Game objects
        private Polygon ship = null!;
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

        public MainWindow()
        {

            DataContext = SettingsManager;
            InitializeComponent();

            LoadGameSettings();
            InitializeSession();
            InitializeFirebase();

            // Ensure window can receive keyboard input
            this.Loaded += (s, e) => {
                this.Focus();
                UpdateMusicButtonText(); // Update button text when window is loaded
                
                // Initialize player name TextBox after controls are loaded
                PlayerNameTextBox.Text = Session.PlayerName;
                PlayerNameTextBox.TextChanged += PlayerNameTextBox_TextChanged;
            };
            
           
            // Save settings when window is closing
            this.Closing += (s, e) => SaveGameSettings();
            
            InitializeGame();
        }

        private void InitializeSession()
        {
            currentSession = new Session();
            System.Diagnostics.Debug.WriteLine("New session started");
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
        private void LoadGameSettings()
        {
            try
            {
                // Load application config (player name, etc.) first
                LoadAppConfig();
                
                // Try to load from existing config file for backward compatibility
                if (ConfigManager.ConfigFileExists())
                {
                    var legacyConfig = ConfigManager.LoadConfig();
                    SettingsManager.GameSettings.LoadFromConfig(legacyConfig);
                    UpdateMessage("Settings loaded from legacy config file", Brushes.LightGreen);
                }
                else
                {
                    UpdateMessage("Using default settings", Brushes.LightBlue);
                }

            }
            catch (Exception ex)
            {
                UpdateMessage($"Error loading config: {ex.Message}", Brushes.LightCoral);
                // Use defaults if loading fails
                
            }
        }
         
        private void SaveGameSettings()
        {
            try
            {
                // Save application config (player name, etc.) first
                SaveAppConfig();

                // Save settings using the new system
                var gameConfig = SettingsManager.ToGameConfig();
                bool success = ConfigManager.SaveConfig(gameConfig);
                if (success)
                {
                    UpdateMessage("Settings saved successfully", Brushes.LightGreen);
                }
                else
                {
                    UpdateMessage("Failed to save settings", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error saving settings: {ex.Message}", Brushes.LightCoral);
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Load application configuration including player name
        /// </summary>
        private void LoadAppConfig()
        {
            try
            {
                var appConfig = ConfigManager.LoadAppConfig();
                Session.PlayerName = appConfig.PlayerName;
                
                System.Diagnostics.Debug.WriteLine($"App config loaded. Player name: {Session.PlayerName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading app config: {ex.Message}");
                // Use default if loading fails
                Session.PlayerName = "Anonymous";
            }
        }

        /// <summary>
        /// Save application configuration including player name
        /// </summary>
        private void SaveAppConfig()
        {
            try
            {
                var appConfig = new AppConfig
                {
                    PlayerName = Session.PlayerName
                };
                
                bool success = ConfigManager.SaveAppConfig(appConfig);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"App config saved successfully. Player name: {Session.PlayerName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to save app config");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving app config: {ex.Message}");
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
            
            // Set initial music state from settings
            if (GameSettings.MusicEnabled != null)
            {
                audioManager.MusicEnabled = GameSettings.MusicEnabled.Value;
                UpdateMusicButtonText();
            }

            random = new Random();
            
            // Initialize particle controller
            particleManager = new ParticleManager(GameCanvas);
            particleManager.CollisionDetected += OnCollisionDetected;
            
            // Initialize power-up manager
            powerUpManager = new PowerUpManager(GameCanvas, random);
            powerUpManager.PowerUpCollected += OnPowerUpCollected;
            powerUpManager.PowerUpEffectStarted += OnPowerUpEffectStarted;
            powerUpManager.PowerUpEffectEnded += OnPowerUpEffectEnded;
            powerUpManager.PowerUpStored += OnPowerUpStored;
            powerUpManager.SingularityActivated += OnSingularityActivated;
            
            // Use CompositionTarget.Rendering for smooth game loop
            CompositionTarget.Rendering += GameLoop;

            CreateShip();
        }
        
        private async void OnCollisionDetected()
        {
            StopGame();
            
            // Show the game over screen instead of MessageBox
            await ShowGameOverScreenAsync();
        }

        private void OnPowerUpCollected(string powerUpType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up collected: {powerUpType}");
            if (powerUpType == "Singularity")
            {
                UpdateMessage($"Singularity stored! Click to activate ({powerUpManager.GetStoredPowerUpCount("Singularity")})", Brushes.Purple);
            }
            else if (powerUpType == "Repulsor")
            {
                UpdateMessage($"Repulsor stored! Right-click to activate ({powerUpManager.GetStoredPowerUpCount("Repulsor")})", Brushes.Green);
            }
            else
            {
                UpdateMessage($"Collected {powerUpType}!", Brushes.Gold);
            }
        }

        private void OnPowerUpEffectStarted(string effectType, double duration)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up effect started: {effectType} for {duration} seconds");
            UpdateMessage($"{effectType} activated for {duration:F1}s!", Brushes.CornflowerBlue);
        }

        private void OnPowerUpEffectEnded(string effectType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up effect ended: {effectType}");
            UpdateMessage($"{effectType} effect ended", Brushes.LightGray);
        }

        private void OnPowerUpStored(string powerUpType)
        {
            System.Diagnostics.Debug.WriteLine($"Power-up stored: {powerUpType}");
            int count = powerUpManager.GetStoredPowerUpCount(powerUpType);
            UpdateMessage($"{powerUpType} stored! Total: {count}", Brushes.MediumPurple);
        }

        private void OnSingularityActivated(Vector2 position)
        {
            System.Diagnostics.Debug.WriteLine($"Singularity activated at {position}");
            UpdateMessage("Singularity created! Gravity well active for 5 seconds", Brushes.DarkViolet);
        }
      
        private void CreateShip()
        {
            ship = new Polygon();
            ship.Fill = Brushes.Cyan;
            ship.Stroke = Brushes.White;
            ship.StrokeThickness = 1;
            
            // Create triangle shape pointing up
            PointCollection points = new PointCollection();
            points.Add(new Point(0, -15));  // Top point
            points.Add(new Point(-10, 10)); // Bottom left
            points.Add(new Point(10, 10));  // Bottom right
            ship.Points = points;
            
            // Position ship in center of game area (wait for canvas to load)
            this.Loaded += (s, e) => {
                centerScreen = new Point(GameCanvas.ActualWidth / 2, GameCanvas.ActualHeight / 2);
                shipPosition = centerScreen;
                Canvas.SetLeft(ship, shipPosition.X);
                Canvas.SetTop(ship, shipPosition.Y);
                
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
            
            // Snapshot current settings as active settings for this game
            
            activeShipSpeed = GameSettings.ShipSpeed.Value;
            activeLevelDuration = GameSettings.LevelDuration.Value;
            activeNewParticlesPerLevel = GameSettings.NewParticlesPerLevel.Value;

            // Initialize particle controller with game settings
            ParticleManager.InitializeGameSettings();
            
            // Create game configuration for this game
            var gameConfig = SettingsManager.ToGameConfig();
            
            // Start a new game in the session
            currentGame = currentSession.StartNewGame(gameConfig);
           
            
            // Reset all key states to prevent ship from moving automatically
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            // Reset ship position to current center
            shipPosition = centerScreen;
            Canvas.SetLeft(ship, shipPosition.X);
            Canvas.SetTop(ship, shipPosition.Y);
            
            // Start new game through particle controller
            particleManager.StartNewGame();
            
            // Start new game through power-up manager
            powerUpManager.StartNewGame();
            
            
            // Show that settings are locked during game
            UpdateMessage("Settings locked during game", Brushes.Yellow);
        }
        
        private async void StopGame()
        {
            gameRunning = false;
            
            // Don't show start screen immediately - will be shown after game over screen
            
            // Complete the current game
            if (currentGame != null)
            {
                currentSession.CompleteCurrentGame(particleManager.ParticleCount);
                System.Diagnostics.Debug.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {currentGame.FinalParticleCount} particles");
                System.Diagnostics.Debug.WriteLine($"Session stats: {currentSession.GetSessionStats()}");
                
                // Save game data to Firebase
                await SaveGameToFirebase(currentGame);
            }
            
            // Reset all key states when game stops
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            audioManager.SetVolume(0.5);
            
            // Clear settings locked message
            UpdateMessage("Game ended - settings can be changed", Brushes.LightGreen);
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
                UpdateMessage("Game saved to database", Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save game data to Firebase: {ex.Message}");
                UpdateMessage("Failed to save game data", Brushes.LightCoral);
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
                double newX = Math.Max(10, Math.Min(GameCanvas.ActualWidth - 10, shipPosition.X + deltaX));
                double newY = Math.Max(10, Math.Min(GameCanvas.ActualHeight - 10, shipPosition.Y + deltaY));
                
                shipPosition = new Point(newX, newY);
                Canvas.SetLeft(ship, shipPosition.X);
                Canvas.SetTop(ship, shipPosition.Y);
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
        
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle escape key for GameConfigScreen
            if (e.Key == Key.Escape && GameConfigScreen.Visibility == Visibility.Visible)
            {
                HideGameConfigScreen();
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Space:
                    if (gameOverScreenVisible)
                    {
                        // Transition from game over screen to start screen
                        ShowStartScreen();
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
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.S:
                    if (gameRunning)
                    {
                        keysPressed[1] = true;
                    }
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.A:
                    if (gameRunning)
                    {
                        keysPressed[2] = true;
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.D:
                    if (gameRunning)
                    {
                        keysPressed[3] = true;
                    }
                    e.Handled = true;
                    break;
            }
        }
        
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
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
                        UpdateMessage("No Singularity power-up available!", Brushes.Red);
                    }
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    // Right click - try to activate repulsor at ship position
                    var shipVector = new Vector2((float)shipPosition.X, (float)shipPosition.Y);
                    
                    bool activated = powerUpManager.TryActivateRepulsor(shipVector);
                    if (!activated)
                    {
                        UpdateMessage("No Repulsor power-up available!", Brushes.Red);
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
                    UpdateMessage("Settings reset to defaults", Brushes.Orange);
                    System.Diagnostics.Debug.WriteLine("Settings reset to default values");
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error resetting settings: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in ResetSettingsButton_Click: {ex.Message}");
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show the GameConfigScreen instead of directly saving
                ShowGameConfigScreen();
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error opening save screen: {ex.Message}", Brushes.Red);
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

                    // Update audio manager with loaded music setting
                    audioManager.MusicEnabled = GameSettings.MusicEnabled.Value;
                    UpdateMusicButtonText();

                    UpdateMessage("Settings loaded successfully", Brushes.LightGreen);
                    System.Diagnostics.Debug.WriteLine("Settings loaded from file successfully");
                }
                else
                {
                    UpdateMessage("Load canceled or failed", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error loading settings: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in LoadSettingsButton_Click: {ex.Message}");
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
                UpdateMessage($"Error showing config screen: {ex.Message}", Brushes.Red);
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
            HideGameConfigScreen();
        }

        /// <summary>
        /// Handle when a configuration is saved from GameConfigScreen
        /// </summary>
        private void GameConfigScreen_ConfigSaved(object sender, GameConfig e)
        {
            try
            {
                UpdateMessage($"Configuration '{e.ConfigName}' saved successfully!", Brushes.LightGreen);
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
                // Toggle the music setting
                GameSettings.MusicEnabled.Value = !GameSettings.MusicEnabled.Value;
                
                // Update the audio manager
                audioManager.MusicEnabled = GameSettings.MusicEnabled.Value;
                
                // Update button text
                UpdateMusicButtonText();
                
                // Save the settings immediately
                SaveGameSettings();
                
                System.Diagnostics.Debug.WriteLine($"Music toggled to: {GameSettings.MusicEnabled.Value}");
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error toggling music: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in MusicToggleButton_Click: {ex.Message}");
            }
        }

        private void UpdateMusicButtonText()
        {
            if (MusicToggleButton != null && GameSettings.MusicEnabled != null)
            {
                bool isEnabled = GameSettings.MusicEnabled.Value;
                MusicToggleButton.Content = isEnabled ? "🎵 Music: ON" : "🔇 Music: OFF";
                MusicToggleButton.Background = isEnabled ? new SolidColorBrush(Colors.DarkSlateBlue) : new SolidColorBrush(Colors.DarkRed);
            }
        }
    }
}