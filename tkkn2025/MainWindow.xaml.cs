using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace tkkn2025
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Game objects
        private Polygon ship = null!;
        private List<Patricle> particles = null!;
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

        // Game settings - Current slider values (can be changed anytime)
        private double currentShipSpeed;
        private double currentParticleSpeed;
        private int currentStartingParticles;
        private double currentGenerationRate;
        private double currentIncreaseRate;
        private double currentParticleSpeedVariance;
        private double currentParticleRandomizerPercentage;
        private bool currentParticleChase_Initial;

        // Active game settings - Only applied when game starts
        private double activeShipSpeed;
        private double activeParticleSpeed;
        private int activeStartingParticles;
        private double activeGenerationRate;
        private double activeIncreaseRate;
        private double activeParticleSpeedVariance;
        private double activeParticleRandomizerPercentage;
        private bool activeParticleChase_Initial;
        
        // Configuration and session management
        private GameConfig gameConfig = null!;
        private Session currentSession = null!;
        private Game? currentGame = null;
        
        // Audio
        AudioManager audioManager = new AudioManager();
        
        // UI Update timing
        private DateTime lastUIUpdate = DateTime.Now;
        private DateTime lastFPSUpdate = DateTime.Now;
        private int frameCount = 0;
        
        // Current game values
        private int currentParticleCount = 0;
        
        // Performance optimizations
        private readonly Queue<Patricle> particlePool = new Queue<Patricle>();
        
        public MainWindow()
        {
            InitializeComponent();

            LoadGameSettings();
            InitializeSession();

            // Ensure window can receive keyboard input
            this.Loaded += (s, e) => this.Focus();
            
            // Save settings when window is closing
            this.Closing += (s, e) => SaveGameSettings();
            
            InitializeGame();
        }

        private void InitializeSession()
        {
            currentSession = new Session();
            System.Diagnostics.Debug.WriteLine("New session started");
        }

        private void LoadGameSettings()
        {
            try
            {
                // Load configuration from file or use defaults
                gameConfig = ConfigManager.LoadConfig();
                
                // Apply loaded settings to current settings
                ApplyConfigToCurrentSettings(gameConfig);

                // Show status
                if (ConfigManager.ConfigFileExists())
                {
                    UpdateConfigStatus("Settings loaded from config file", Brushes.LightGreen);
                }
                else
                {
                    UpdateConfigStatus("Using default settings", Brushes.LightBlue);
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error loading config: {ex.Message}", Brushes.LightCoral);
                // Use defaults if loading fails
                gameConfig = SettingsManager.GetDefaultSettings();
                ApplyConfigToCurrentSettings(gameConfig);
            }
        }

        /// <summary>
        /// Applies a GameConfig to the current settings variables
        /// </summary>
        /// <param name="config">Configuration to apply</param>
        private void ApplyConfigToCurrentSettings(GameConfig config)
        {
            currentShipSpeed = config.ShipSpeed;
            currentParticleSpeed = config.ParticleSpeed;
            currentStartingParticles = config.StartingParticles;
            currentGenerationRate = config.GenerationRate;
            currentIncreaseRate = config.IncreaseRate;
            currentParticleSpeedVariance = config.ParticleSpeedVariance;
            currentParticleRandomizerPercentage = config.ParticleRandomizerPercentage;
            currentParticleChase_Initial = config.ParticleChase_Initial;
        }

        /// <summary>
        /// Creates a GameConfig from current settings variables
        /// </summary>
        /// <returns>GameConfig with current values</returns>
        private GameConfig CreateConfigFromCurrentSettings()
        {
            return new GameConfig
            {
                ShipSpeed = currentShipSpeed,
                ParticleSpeed = currentParticleSpeed,
                StartingParticles = currentStartingParticles,
                GenerationRate = currentGenerationRate,
                IncreaseRate = currentIncreaseRate,
                ParticleSpeedVariance = currentParticleSpeedVariance,
                ParticleRandomizerPercentage = currentParticleRandomizerPercentage,
                ParticleChase_Initial = currentParticleChase_Initial
            };
        }

        /// <summary>
        /// Updates all UI controls with current settings values
        /// </summary>
        private void UpdateAllUIControls()
        {
            // Update sliders
            ShipSpeedSlider.Value = currentShipSpeed;
            ParticleSpeedSlider.Value = currentParticleSpeed;
            ParticleSpeedVarianceSlider.Value = currentParticleSpeedVariance;
            ParticleRandomizerPercentageSlider.Value = currentParticleRandomizerPercentage;
            StartingParticlesSlider.Value = currentStartingParticles;
            GenerationRateSlider.Value = currentGenerationRate;
            IncreaseRateSlider.Value = currentIncreaseRate;

            // Update checkbox
            ParticleChaseCheckBox.IsChecked = currentParticleChase_Initial;

            // Update text boxes
            UpdateDisplayValues();
        }
        
        private void SaveGameSettings()
        {
            try
            {
                // Update config with current slider values
                gameConfig.ShipSpeed = currentShipSpeed;
                gameConfig.ParticleSpeed = currentParticleSpeed;
                gameConfig.StartingParticles = currentStartingParticles;
                gameConfig.GenerationRate = currentGenerationRate;
                gameConfig.IncreaseRate = currentIncreaseRate;
                gameConfig.ParticleSpeedVariance = currentParticleSpeedVariance;
                gameConfig.ParticleRandomizerPercentage = currentParticleRandomizerPercentage;
                gameConfig.ParticleChase_Initial = currentParticleChase_Initial;
                
                // Save to file
                bool success = ConfigManager.SaveConfig(gameConfig);
                if (success)
                {
                    UpdateConfigStatus("Settings saved successfully", Brushes.LightGreen);
                }
                else
                {
                    UpdateConfigStatus("Failed to save settings", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error saving settings: {ex.Message}", Brushes.LightCoral);
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void UpdateConfigStatus(string message, Brush color)
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

            particles = new List<Patricle>();
            random = new Random();
            
            // Use CompositionTarget.Rendering for smooth game loop
            CompositionTarget.Rendering += GameLoop;

            // Update all UI controls with current settings
            UpdateAllUIControls();

            CreateShip();
        }

        private void UpdateDisplayValues()
        {
            ShipSpeedTextBox.Text = currentShipSpeed.ToString("F0");
            ParticleSpeedTextBox.Text = currentParticleSpeed.ToString("F0");
            ParticleSpeedVarianceTextBox.Text = currentParticleSpeedVariance.ToString("F0");
            ParticleRandomizerPercentageTextBox.Text = currentParticleRandomizerPercentage.ToString("F0");
            StartingParticlesTextBox.Text = currentStartingParticles.ToString();
            GenerationRateTextBox.Text = currentGenerationRate.ToString("F0");
            IncreaseRateTextBox.Text = currentIncreaseRate.ToString("F0");
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
            
            UpdateFPS();
            UpdateShipPosition(deltaTime);
            UpdateParticles(deltaTime);
            CheckCollisions();
            
            // Handle particle generation timing using ACTIVE settings
            if ((now - lastParticleGeneration).TotalSeconds >= activeGenerationRate)
            {
                GenerateMoreParticles();
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
            
            // Apply current slider values to active game settings (settings are locked for this game)
            activeShipSpeed = currentShipSpeed;
            activeParticleSpeed = currentParticleSpeed;
            activeStartingParticles = currentStartingParticles;
            activeGenerationRate = currentGenerationRate;
            activeIncreaseRate = currentIncreaseRate;
            activeParticleSpeedVariance = currentParticleSpeedVariance;
            activeParticleRandomizerPercentage = currentParticleRandomizerPercentage;
            activeParticleChase_Initial = currentParticleChase_Initial;
            
            // Create game configuration for this game
            var gameSettings = new GameConfig
            {
                ShipSpeed = activeShipSpeed,
                ParticleSpeed = activeParticleSpeed,
                StartingParticles = activeStartingParticles,
                GenerationRate = activeGenerationRate,
                IncreaseRate = activeIncreaseRate,
                ParticleSpeedVariance = activeParticleSpeedVariance,
                ParticleRandomizerPercentage = activeParticleRandomizerPercentage,
                ParticleChase_Initial = activeParticleChase_Initial
            };
            
            // Start a new game in the session
            currentGame = currentSession.StartNewGame(gameSettings);
            
            System.Diagnostics.Debug.WriteLine($"Started new game #{currentSession.Games.Count} with settings: " +
                                             $"Ship:{activeShipSpeed}, Particles:{activeParticleSpeed}, " +
                                             $"Starting:{activeStartingParticles}");
            
            // Reset all key states to prevent ship from moving automatically
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            // Reset ship position
            shipPosition = centerScreen;
            Canvas.SetLeft(ship, shipPosition.X);
            Canvas.SetTop(ship, shipPosition.Y);
            
            // Clear existing particles efficiently
            ClearAllParticles();
            
            // Create initial particles using ACTIVE settings
            currentParticleCount = activeStartingParticles;
            CreateParticles(currentParticleCount);
            
            StartButton.Content = "Game Running";
            
            // Show that settings are locked during game
            UpdateConfigStatus("Settings locked during game", Brushes.Yellow);
        }
        
        private void StopGame()
        {
            gameRunning = false;
            
            // Complete the current game
            if (currentGame != null)
            {
                currentSession.CompleteCurrentGame(particles.Count);
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
            UpdateConfigStatus("Game ended - settings can be changed", Brushes.LightGreen);
        }
        
        private void ClearAllParticles()
        {
            foreach (var particle in particles)
            {
                if (particle.IsActive)
                {
                    GameCanvas.Children.Remove(particle.Visual);
                    particle.IsActive = false;
                    particlePool.Enqueue(particle);
                }
            }
            particles.Clear();
        }
        
        private void CreateParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateParticle();
            }
        }
        
        private Patricle GetPooledParticle()
        {
            if (particlePool.Count > 0)
            {
                var pooled = particlePool.Dequeue();
                pooled.IsActive = true;
                return pooled;
            }
            
            return new Patricle
            {
                Visual = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White // Default color
                },
                IsActive = true
            };
        }
        
        private void CreateParticle()
        {
            var particle = GetPooledParticle();
            
            // Spawn particle outside game area
            Point spawnPosition = GetRandomSpawnPosition();
            particle.X = spawnPosition.X;
            particle.Y = spawnPosition.Y;
            
            // Calculate base speed for this particle using ACTIVE settings
            double actualParticleSpeed = activeParticleSpeed;
            
            // Apply speed randomization based on ACTIVE settings
            int randomValue = random.Next(1, 101); // 1 to 100
            if (randomValue <= activeParticleRandomizerPercentage)
            {
                // Apply speed variance (randomly faster or slower)
                double varianceMultiplier = (random.NextDouble() * 2 - 1) * (activeParticleSpeedVariance / 100.0);
                actualParticleSpeed = activeParticleSpeed * (1 + varianceMultiplier);
                
                // Ensure speed doesn't go negative or too slow
                actualParticleSpeed = Math.Max(actualParticleSpeed, activeParticleSpeed * 0.1);
            }
            
            // Store the actual speed in the particle
            particle.Speed = actualParticleSpeed;
            
            // Set particle color based on speed relative to default
            if (actualParticleSpeed > activeParticleSpeed)
            {
                // Faster than default - Red
                particle.Visual.Fill = Brushes.Red;
            }
            else if (actualParticleSpeed < activeParticleSpeed)
            {
                // Slower than default - Blue
                particle.Visual.Fill = Brushes.Blue;
            }
            else
            {
                // Default speed - White
                particle.Visual.Fill = Brushes.White;
            }
            
            // Calculate velocity based on ParticleChase_Initial setting
            Vector direction;
            if (activeParticleChase_Initial)
            {
                // Chase the ship's current position
                direction = new Vector(
                    shipPosition.X - spawnPosition.X,
                    shipPosition.Y - spawnPosition.Y);
            }
            else
            {
                // Move toward the center screen (initial position)
                direction = new Vector(
                    centerScreen.X - spawnPosition.X,
                    centerScreen.Y - spawnPosition.Y);
            }
            direction.Normalize();
            
            // Set velocity in pixels per second using the actual speed
            particle.VelocityX = direction.X * actualParticleSpeed;
            particle.VelocityY = direction.Y * actualParticleSpeed;
            
            // Set visual position
            Canvas.SetLeft(particle.Visual, particle.X);
            Canvas.SetTop(particle.Visual, particle.Y);
            
            particles.Add(particle);
            GameCanvas.Children.Add(particle.Visual);
        }
        
        private Point GetRandomSpawnPosition()
        {
            double canvasWidth = GameCanvas.ActualWidth > 0 ? GameCanvas.ActualWidth : 800;
            double canvasHeight = GameCanvas.ActualHeight > 0 ? GameCanvas.ActualHeight : 600;
            
            int side = random.Next(4); // 0: top, 1: right, 2: bottom, 3: left
            
            return side switch
            {
                0 => new Point(random.NextDouble() * canvasWidth, -20), // Top
                1 => new Point(canvasWidth + 20, random.NextDouble() * canvasHeight), // Right
                2 => new Point(random.NextDouble() * canvasWidth, canvasHeight + 20), // Bottom
                3 => new Point(-20, random.NextDouble() * canvasHeight), // Left
                _ => new Point(0, 0)
            };
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
            
            // Calculate movement based on time and speed using ACTIVE settings
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
        
        private void UpdateParticles(double deltaTime)
        {
            double canvasWidth = GameCanvas.ActualWidth;
            double canvasHeight = GameCanvas.ActualHeight;
            
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                
                // Update position using velocity and delta time
                particle.X += particle.VelocityX * deltaTime;
                particle.Y += particle.VelocityY * deltaTime;
                
                // Check bounds
                if (particle.X < -50 || particle.X > canvasWidth + 50 ||
                    particle.Y < -50 || particle.Y > canvasHeight + 50)
                {
                    // Remove and pool particle
                    GameCanvas.Children.Remove(particle.Visual);
                    particle.IsActive = false;
                    particlePool.Enqueue(particle);
                    particles.RemoveAt(i);
                    
                    // Spawn a new particle to replace it
                    CreateParticle();
                }
                else
                {
                    // Update visual position (sub-pixel precision)
                    Canvas.SetLeft(particle.Visual, particle.X);
                    Canvas.SetTop(particle.Visual, particle.Y);
                }
            }
        }
        
        private void CheckCollisions()
        {
            double shipX = shipPosition.X;
            double shipY = shipPosition.Y;
            const double maxCheckDistance = 50; // Only check nearby particles
            
            foreach (var particle in particles)
            {
                // Quick distance check first (cheaper than full calculation)
                double roughDeltaX = Math.Abs(shipX - particle.X);
                double roughDeltaY = Math.Abs(shipY - particle.Y);
                
                // Skip if obviously too far away
                if (roughDeltaX > maxCheckDistance || roughDeltaY > maxCheckDistance)
                    continue;
                    
                // Now do precise collision detection
                double particleX = particle.X + 4;
                double particleY = particle.Y + 4;
                double deltaX = shipX - particleX;
                double deltaY = shipY - particleY;
                double distanceSquared = deltaX * deltaX + deltaY * deltaY;
                
                if (distanceSquared < 225) // 15^2
                {
                    StopGame();
                    var survivedTime = (DateTime.Now - gameStartTime).TotalSeconds;
                    
                    // Show game over message with session stats
                    var sessionStats = currentSession.GetSessionStats();
                    MessageBox.Show($"Game Over! You survived for {survivedTime:F1} seconds!\n\n{sessionStats}", 
                                   "Game Over", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
        }
        
        private void GenerateMoreParticles()
        {
            // Increase particle count by the specified percentage using ACTIVE settings
            currentParticleCount = (int)(currentParticleCount * (1 + activeIncreaseRate / 100.0));
            CreateParticles(Math.Max(1, currentParticleCount / 10)); // Add some particles
        }
        
        private void UpdateUI()
        {
            ParticleCountText.Text = $"Particles: {particles.Count}";
            
            if (gameRunning)
            {
                var elapsed = DateTime.Now - gameStartTime;
                GameTimeText.Text = $"Time: {elapsed.TotalSeconds:F0}s";
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
        
        // Settings event handlers - These update CURRENT settings (displayed values) but don't affect running games
        private void ShipSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentShipSpeed = (int)e.NewValue;
            if (ShipSpeedTextBox != null)
                ShipSpeedTextBox.Text = currentShipSpeed.ToString("F0");
        }
        
        private void ParticleSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentParticleSpeed = (int)e.NewValue;
            if (ParticleSpeedTextBox != null)
                ParticleSpeedTextBox.Text = currentParticleSpeed.ToString("F0");
        }
        
        private void ParticleSpeedVarianceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentParticleSpeedVariance = (int)e.NewValue;
            if (ParticleSpeedVarianceTextBox != null)
                ParticleSpeedVarianceTextBox.Text = currentParticleSpeedVariance.ToString("F0");
        }
        
        private void ParticleRandomizerPercentageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentParticleRandomizerPercentage = (int)e.NewValue;
            if (ParticleRandomizerPercentageTextBox != null)
                ParticleRandomizerPercentageTextBox.Text = currentParticleRandomizerPercentage.ToString("F0");
        }
        
        private void StartingParticlesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentStartingParticles = (int)e.NewValue;
            if (StartingParticlesTextBox != null)
                StartingParticlesTextBox.Text = currentStartingParticles.ToString();
        }
        
        private void GenerationRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentGenerationRate = (int)e.NewValue;
            if (GenerationRateTextBox != null)
                GenerationRateTextBox.Text = currentGenerationRate.ToString("F0");
        }
        
        private void IncreaseRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            currentIncreaseRate = (int)e.NewValue;
            if (IncreaseRateTextBox != null)
                IncreaseRateTextBox.Text = currentIncreaseRate.ToString("F0");
        }

        // TextBox event handlers - Allow direct text input
        private void ShipSpeedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ShipSpeedTextBox.Text, out double value))
            {
                value = Math.Max(50, Math.Min(500, value)); // Clamp to slider range
                currentShipSpeed = value;
                if (ShipSpeedSlider != null)
                    ShipSpeedSlider.Value = value;
            }
        }

        private void ParticleSpeedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ParticleSpeedTextBox.Text, out double value))
            {
                value = Math.Max(25, Math.Min(300, value)); // Clamp to slider range
                currentParticleSpeed = value;
                if (ParticleSpeedSlider != null)
                    ParticleSpeedSlider.Value = value;
            }
        }

        private void ParticleSpeedVarianceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ParticleSpeedVarianceTextBox.Text, out double value))
            {
                value = Math.Max(0, Math.Min(100, value)); // Clamp to slider range
                currentParticleSpeedVariance = value;
                if (ParticleSpeedVarianceSlider != null)
                    ParticleSpeedVarianceSlider.Value = value;
            }
        }

        private void ParticleRandomizerPercentageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(ParticleRandomizerPercentageTextBox.Text, out double value))
            {
                value = Math.Max(0, Math.Min(100, value)); // Clamp to slider range
                currentParticleRandomizerPercentage = value;
                if (ParticleRandomizerPercentageSlider != null)
                    ParticleRandomizerPercentageSlider.Value = value;
            }
        }

        private void StartingParticlesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(StartingParticlesTextBox.Text, out int value))
            {
                value = Math.Max(1, Math.Min(100, value)); // Clamp to slider range
                currentStartingParticles = value;
                if (StartingParticlesSlider != null)
                    StartingParticlesSlider.Value = value;
            }
        }

        private void GenerationRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(GenerationRateTextBox.Text, out double value))
            {
                value = Math.Max(1, Math.Min(20, value)); // Clamp to slider range
                currentGenerationRate = value;
                if (GenerationRateSlider != null)
                    GenerationRateSlider.Value = value;
            }
        }

        private void IncreaseRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(IncreaseRateTextBox.Text, out double value))
            {
                value = Math.Max(1, Math.Min(50, value)); // Clamp to slider range
                currentIncreaseRate = value;
                if (IncreaseRateSlider != null)
                    IncreaseRateSlider.Value = value;
            }
        }

        // Canvas event handler for focus management
        private void GameCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Make the canvas take focus when clicked
            GameCanvas.Focus();
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
                    // Reset to default settings
                    var defaultSettings = SettingsManager.GetDefaultSettings();
                    ApplyConfigToCurrentSettings(defaultSettings);
                    UpdateAllUIControls();

                    UpdateConfigStatus("Settings reset to defaults", Brushes.Orange);
                    System.Diagnostics.Debug.WriteLine("Settings reset to default values");
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error resetting settings: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in ResetSettingsButton_Click: {ex.Message}");
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentSettings = CreateConfigFromCurrentSettings();
                var validated = SettingsManager.ValidateSettings(currentSettings);

                bool success = SettingsManager.SaveSettingsWithDialog(validated);
                
                if (success)
                {
                    UpdateConfigStatus("Settings saved successfully", Brushes.LightGreen);
                    System.Diagnostics.Debug.WriteLine("Settings saved to file successfully");
                }
                else
                {
                    UpdateConfigStatus("Save cancelled or failed", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error saving settings: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in SaveSettingsButton_Click: {ex.Message}");
            }
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loadedSettings = SettingsManager.LoadSettingsWithDialog();
                
                if (loadedSettings != null)
                {
                    // Validate and apply the loaded settings
                    var validated = SettingsManager.ValidateSettings(loadedSettings);
                    ApplyConfigToCurrentSettings(validated);
                    UpdateAllUIControls();

                    UpdateConfigStatus("Settings loaded successfully", Brushes.LightGreen);
                    System.Diagnostics.Debug.WriteLine("Settings loaded from file successfully");
                }
                else
                {
                    UpdateConfigStatus("Load cancelled or failed", Brushes.LightCoral);
                }
            }
            catch (Exception ex)
            {
                UpdateConfigStatus($"Error loading settings: {ex.Message}", Brushes.Red);
                System.Diagnostics.Debug.WriteLine($"Error in LoadSettingsButton_Click: {ex.Message}");
            }
        }

        // Particle Chase CheckBox Event Handler
        private void ParticleChaseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            currentParticleChase_Initial = ParticleChaseCheckBox.IsChecked ?? true;
        }
    }
}