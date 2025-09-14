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

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Game objects
        private Polygon ship = null!;
        private ParticleManager particleController = null!;
        private PowerUpManager powerUpManager = null!;
        private Random random = null!;
        
        // Game state
        private bool gameRunning = false;
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

        public MainWindow()
        {

            DataContext = SettingsManager;
            InitializeComponent();

            LoadGameSettings();
            InitializeSession();

            // Ensure window can receive keyboard input
            this.Loaded += (s, e) => {
                this.Focus();
                UpdateMusicButtonText(); // Update button text when window is loaded
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


        //App start and close
        private void LoadGameSettings()
        {
            try
            {
                
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
            if (SettingsManager.GameSettings?.MusicEnabled != null)
            {
                audioManager.MusicEnabled = SettingsManager.GameSettings.MusicEnabled.Value;
                UpdateMusicButtonText();
            }

            random = new Random();
            
            // Initialize particle controller
            particleController = new ParticleManager(GameCanvas);
            particleController.CollisionDetected += OnCollisionDetected;
            
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
        
        private void OnCollisionDetected()
        {
            StopGame();
            var survivedTime = (DateTime.Now - gameStartTime).TotalSeconds;
            
            // Show game over message with session stats
            var sessionStats = currentSession.GetSessionStats();
            MessageBox.Show($"Game Over! You survived for {survivedTime:F1} seconds!\n\n{sessionStats}", 
                           "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
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
                particleController.UpdateCanvasDimensions();
                
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
            particleController.UpdateParticles(effectiveDeltaTime, shipPosition, powerUpManager);
            
            // Check collisions through controller
            particleController.CheckCollisions(shipPosition);
            
            // Handle particle generation timing (use normal delta time for consistent spawning)
            if ((now - lastParticleGeneration).TotalSeconds >= activeLevelDuration)
            {
                particleController.GenerateMoreParticles(activeNewParticlesPerLevel);
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
            gameStartTime = DateTime.Now;
            lastUpdate = DateTime.Now;
            lastParticleGeneration = DateTime.Now;
            lastUIUpdate = DateTime.Now;
            lastFPSUpdate = DateTime.Now;
            frameCount = 0;
            currentSpeedMultiplier = 1.0;
            
            // Hide the start screen when game starts
            StartScreen.Visibility = Visibility.Hidden;
            
            // Update canvas dimensions for all managers at the start of each game
            particleController.UpdateCanvasDimensions();
            powerUpManager.UpdateCanvasDimensions();
            
            // Update center screen position based on current canvas size
            centerScreen = new Point(GameCanvas.ActualWidth / 2, GameCanvas.ActualHeight / 2);
            
            // Snapshot current settings as active settings for this game
            var gameSettings = SettingsManager.GameSettings;
            
            activeShipSpeed = gameSettings.ShipSpeed.Value;
            activeLevelDuration = gameSettings.LevelDuration.Value;
            activeNewParticlesPerLevel = gameSettings.NewParticlesPerLevel.Value;
            
            // Initialize particle controller with game settings
            particleController.InitializeGameSettings(SettingsManager);
            
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
            particleController.StartNewGame();
            
            // Start new game through power-up manager
            powerUpManager.StartNewGame();
            
            StartButton.Content = "Game Running";
            
            // Show that settings are locked during game
            UpdateMessage("Settings locked during game", Brushes.Yellow);
        }
        
        private void StopGame()
        {
            gameRunning = false;
            
            // Show the start screen when game stops
            StartScreen.Visibility = Visibility.Visible;
            
            // Complete the current game
            if (currentGame != null)
            {
                currentSession.CompleteCurrentGame(particleController.ParticleCount);
                System.Diagnostics.Debug.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {currentGame.FinalParticleCount} particles");
                System.Diagnostics.Debug.WriteLine($"Session stats: {currentSession.GetSessionStats()}");
            }
            
            // Reset all key states when game stops
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            StartButton.Content = "Press SPACE to Start";
            audioManager.SetVolume(0.5);
            
            // Clear settings locked message
            UpdateMessage("Game ended - settings can be changed", Brushes.LightGreen);
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
            ParticleCountText.Text = $"Particles: {particleController.ParticleCount}";
            
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
            switch (e.Key)
            {
                case Key.Space:
                    if (!gameRunning) StartGame();
                    e.Handled = true;
                    break;
                case Key.Up:
                case Key.W:
                    keysPressed[0] = true;
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = true;
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = true;
                    e.Handled = true;
                    break;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = true;
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
                var currentSettings = SettingsManager.ToGameConfig();
                var validated = SettingsManager.ValidateSavedOrLoadedSettings(currentSettings);

                bool success = SettingsManager.SaveSettings(validated);
                
                if (success)
                {
                    UpdateMessage("Settings saved successfully", Brushes.LightGreen);
                    System.Diagnostics.Debug.WriteLine("Settings saved to file successfully");
                }
                else
                {
                    UpdateMessage("Save cancelled or failed", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error saving settings: {ex.Message}", Brushes.Red);
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
                    audioManager.MusicEnabled = SettingsManager.GameSettings.MusicEnabled.Value;
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

        private void MusicToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle the music setting
                SettingsManager.GameSettings.MusicEnabled.Value = !SettingsManager.GameSettings.MusicEnabled.Value;
                
                // Update the audio manager
                audioManager.MusicEnabled = SettingsManager.GameSettings.MusicEnabled.Value;
                
                // Update button text
                UpdateMusicButtonText();
                
                // Save the settings immediately
                SaveGameSettings();
                
                System.Diagnostics.Debug.WriteLine($"Music toggled to: {SettingsManager.GameSettings.MusicEnabled.Value}");
            }
            catch (Exception ex)
            {
                UpdateMessage($"Error toggling music: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in MusicToggleButton_Click: {ex.Message}");
            }
        }

        private void UpdateMusicButtonText()
        {
            if (MusicToggleButton != null && SettingsManager.GameSettings?.MusicEnabled != null)
            {
                bool isEnabled = SettingsManager.GameSettings.MusicEnabled.Value;
                MusicToggleButton.Content = isEnabled ? "🎵 Music: ON" : "🔇 Music: OFF";
                MusicToggleButton.Background = isEnabled ? new SolidColorBrush(Colors.DarkSlateBlue) : new SolidColorBrush(Colors.DarkRed);
            }
        }
    }
}