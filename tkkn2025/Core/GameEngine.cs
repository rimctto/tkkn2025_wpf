using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects;
using tkkn2025.GameObjects.PowerUps;
using tkkn2025.GameObjects.Ship;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.Settings;
using tkkn2025.DataAccess;
using tkkn2025.Helpers;

namespace tkkn2025.Core
{
    /// <summary>
    /// Core game engine that manages all game logic, initialization, and game loop
    /// Separated from MainWindow to maintain clean separation of concerns
    /// </summary>
    public class GameEngine
    {
        #region Private Fields

        // Game objects
        private ShipSprite ship = null!;
        private ParticleManager particleManager = null!;
        private PowerUpManager powerUpManager = null!;
        private LevelManager levelManager = null!;
        private Random random = null!;

        // Game state tracking
        private bool gameRunning = false;
        private bool[] keysPressed = new bool[7]; // Up, Down, Left, Right, LeftShift, RightShift, Space
        private Point centerScreen;
        private Point shipPosition;

        // Game loop timing
        private DateTime lastUpdate = DateTime.Now;
        private DateTime lastParticleGeneration = DateTime.Now;
        private DateTime gameStartTime;
        private DateTime lastUIUpdate = DateTime.Now;
        private DateTime lastFPSUpdate = DateTime.Now;
        private int frameCount = 0;

        // Active game settings - snapshot taken when game starts
        private double activeShipSpeed;
        private double activeShipBoost;
        private double activeShipSuperBoost;
        private double activeLevelDuration;
        private double activeNewParticlesPerLevel;

        // Power-up effects
        private double currentSpeedMultiplier = 1.0;

        // Super boost double-tap detection
        private DateTime lastSpaceKeyPress = DateTime.MinValue;
        private bool isSuperBoostActive = false;
        private const double DoubleTapThreshold = 0.15; // 150ms for double-tap detection

        // Session management
        private Session currentSession = null!;
        private Game? currentGame = null;

        // Firebase connector for saving game data
        private FireBaseConnector firebaseConnector = null!;

        // Canvas reference
        private Canvas gameCanvas = null!;

        #endregion

        #region Events

        /// <summary>
        /// Event fired when the UI needs to be updated
        /// </summary>
        public event Action<GameUIData>? UIUpdateRequested;

        /// <summary>
        /// Event fired when FPS needs to be updated
        /// </summary>
        public event Action<double>? FPSUpdateRequested;

        /// <summary>
        /// Event fired when a collision is detected and game should stop
        /// </summary>
        public event Action? GameStopped;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the game is currently running
        /// </summary>
        public bool IsGameRunning => gameRunning;

        /// <summary>
        /// Current ship position
        /// </summary>
        public Point ShipPosition => shipPosition;

        /// <summary>
        /// Current game session
        /// </summary>
        public Session CurrentSession => currentSession;

        /// <summary>
        /// Current game instance
        /// </summary>
        public Game? CurrentGame => currentGame;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the game engine with required dependencies
        /// </summary>
        /// <param name="canvas">Game canvas for rendering</param>
        /// <param name="session">Current game session</param>
        /// <param name="firebaseConnector">Firebase connector for saving data</param>
        public void Initialize(Canvas canvas, Session session, FireBaseConnector firebaseConnector)
        {
            gameCanvas = canvas;
            currentSession = session;
            this.firebaseConnector = firebaseConnector;

            random = new Random();

            // Initialize particle controller - no direct event wiring needed
            particleManager = new ParticleManager(gameCanvas);

            // Initialize power-up manager - no direct event wiring needed
            powerUpManager = new PowerUpManager(gameCanvas, random);

            CreateShip();

            // Subscribe to game events
            SubscribeToGameEvents();

            // Debug initialization info
            DebugHelper.WriteLine("Game Engine initialization completed");
        }

        private void CreateShip()
        {
            ship = new ShipSprite();
            gameCanvas.Children.Add(ship);
            ship.Reset(new Point(-100, -100));

        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Start a new game with current settings
        /// </summary>
        public void StartGame()
        {
            if (gameRunning) return;

            // Initialize level manager based on settings
            if (GameSettings.LevelMechanicsEnabled.Value)
            {
                levelManager = new LevelManager(LevelManager.GenerateMaze_Medium(), gameCanvas);
            }
            else 
            { 
                levelManager = new LevelManager(); 
            }

            gameRunning = true;
            gameStartTime = DateTime.Now;
            lastUpdate = DateTime.Now;
            lastParticleGeneration = DateTime.Now;
            lastUIUpdate = DateTime.Now;
            lastFPSUpdate = DateTime.Now;
            frameCount = 0;
            currentSpeedMultiplier = 1.0;

            // Update canvas dimensions FIRST before creating level manager
            UpdateCanvasDimensions();

            // Snapshot current settings as active settings for this game
            activeShipSpeed = GameSettings.ShipSpeed.Value;
            activeShipBoost = GameSettings.ShipBoost.Value;
            activeShipSuperBoost = GameSettings.ShipSuperBoost.Value;
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

            // Reset super boost state
            lastSpaceKeyPress = DateTime.MinValue;
            isSuperBoostActive = false;

            // Reset ship position to current center and show neutral
            centerScreen = new Point(gameCanvas.ActualWidth / 2, gameCanvas.ActualHeight / 2);
            shipPosition = centerScreen;
            ship.Reset(centerScreen);
        
            // Start new game through particle controller
            particleManager.StartNewGame();

            // Start new game through power-up manager
            powerUpManager.StartNewGame();

            // Raise game started event
            GameEvents.RaiseGameStarted();

            // Debug output for game start
            DebugHelper.WriteLine($"Game started with {GameSettings.StartingParticles.Value} particles, Ship Speed: {activeShipSpeed}");
            DebugHelper.WriteLine($"Level mechanics enabled: {GameSettings.LevelMechanicsEnabled.Value}");
            DebugHelper.WriteLine($"Ship Super Boost: {activeShipSuperBoost}");
            
            levelManager.ActivateMechanics();
        }

        /// <summary>
        /// Stop the current game
        /// </summary>
        public void StopGame()
        {
            gameRunning = false;

            // Complete the current game
            if (currentGame != null)
            {
                // Include level mechanic particles in the count
                int totalParticles = particleManager.ParticleCount;
                totalParticles += levelManager.GetActiveLevelMechanicParticleCount();

                currentSession.CompleteCurrentGame(totalParticles);
                DebugHelper.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {currentGame.FinalParticleCount} particles (Level {levelManager.CurrentLevel})");
                DebugHelper.WriteLine($"Session stats: {currentSession.GetSessionStats()}");

                // Raise game completed event
                GameEvents.RaiseGameCompleted(currentGame);
            }

            // Stop all level mechanics
            levelManager.StopAllMechanics();

            // Fire event to notify UI
            GameStopped?.Invoke();
        }

        #endregion

        #region Game Loop

        /// <summary>
        /// Main game loop - call this from CompositionTarget.Rendering
        /// </summary>
        public void Update()
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

            // Update level manager (use normal delta time for level progression)
            levelManager.Update(deltaTime);

            // Check level mechanic collisions
            if (levelManager.CheckParticleMechanicsCollisions(shipPosition))
            {
                // Collision with level mechanic particle detected
                OnParticleShipCollisionDetected();
                return;
            }

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

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle key down events
        /// </summary>
        /// <param name="key">The key that was pressed</param>
        /// <returns>True if the key was handled</returns>
        public bool HandleKeyDown(Key key)
        {
            if (!gameRunning) return false;

            switch (key)
            {
                case Key.Up:
                case Key.W:
                    keysPressed[0] = true;
                    return true;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = true;
                    return true;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = true;
                    return true;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = true;
                    return true;
                case Key.LeftShift:
                    keysPressed[4] = true;
                    return true;
                case Key.RightShift:
                    keysPressed[5] = true;
                    return true;
                case Key.Space:
                    // Handle double-tap detection for super boost
                    var now = DateTime.Now;
                    var timeSinceLastPress = (now - lastSpaceKeyPress).TotalSeconds;
                    
                    if (timeSinceLastPress <= DoubleTapThreshold && !isSuperBoostActive)
                    {
                        // Double-tap detected - activate super boost
                        isSuperBoostActive = true;
                        GameEvents.RaiseMessageRequested("Super Boost Activated!", Brushes.Cyan);
                        DebugHelper.WriteLine("Super boost activated via double-tap");
                    }
                    
                    lastSpaceKeyPress = now;
                    
                    // Space key for boost when game is running
                    keysPressed[6] = true;
                    return true;
                case Key.T:
                    // Debug key to manually trigger level 3 mechanics for testing
                    levelManager.TriggerMechanicsForLevel(3);
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Handle key up events
        /// </summary>
        /// <param name="key">The key that was released</param>
        /// <returns>True if the key was handled</returns>
        public bool HandleKeyUp(Key key)
        {
            switch (key)
            {
                case Key.Up:
                case Key.W:
                    keysPressed[0] = false;
                    return true;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = false;
                    return true;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = false;
                    return true;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = false;
                    return true;
                case Key.LeftShift:
                    keysPressed[4] = false;
                    return true;
                case Key.RightShift:
                    keysPressed[5] = false;
                    return true;
                case Key.Space:
                    keysPressed[6] = false;
                    
                    // Deactivate super boost when space key is released
                    if (isSuperBoostActive)
                    {
                        isSuperBoostActive = false;
                        GameEvents.RaiseMessageRequested("Super Boost Deactivated", Brushes.LightGray);
                        DebugHelper.WriteLine("Super boost deactivated");
                    }
                    
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Handle mouse click on game canvas
        /// </summary>
        /// <param name="e">Mouse button event args</param>
        public void HandleCanvasMouseClick(MouseButtonEventArgs e)
        {
            if (!gameRunning) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Left click - try to activate singularity at click position
                var clickPosition = e.GetPosition(gameCanvas);
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

        #endregion

        #region Private Methods

        private void UpdateCanvasDimensions()
        {
            // Update canvas dimensions for all managers
            ParticleManager.UpdateCanvasDimensions();
            powerUpManager.UpdateCanvasDimensions();
            levelManager.Reset(gameCanvas);
            // Update center screen position based on current canvas size
            centerScreen = new Point(gameCanvas.ActualWidth / 2, gameCanvas.ActualHeight / 2);
        }

        private void UpdateFPS()
        {
            frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - lastFPSUpdate).TotalSeconds;

            if (elapsed >= 1.0) // Update FPS every second
            {
                double fps = frameCount / elapsed;
                
                // Fire event to update UI
                FPSUpdateRequested?.Invoke(fps);

                frameCount = 0;
                lastFPSUpdate = now;
            }
        }

        private void UpdateShipPosition(double deltaTime)
        {
            double deltaX = 0, deltaY = 0;

            // Check for super boost (double-tap space key detection)
            bool isSuperBoostPressed = isSuperBoostActive && keysPressed[6]; // Space key held down after double-tap

            // Check if either shift key or space key is pressed for regular boost using the keysPressed array
            bool isRegularBoostActive = (keysPressed[4] || keysPressed[5] || keysPressed[6]) && !isSuperBoostPressed; // LeftShift or RightShift or Space (but not super boost)

            // Calculate effective ship speed with appropriate boost level
            double effectiveShipSpeed;
            if (isSuperBoostPressed)
            {
                effectiveShipSpeed = activeShipSpeed + activeShipSuperBoost;
            }
            else if (isRegularBoostActive)
            {
                effectiveShipSpeed = activeShipSpeed + activeShipBoost;
            }
            else
            {
                effectiveShipSpeed = activeShipSpeed;
            }

            // Calculate movement based on time and speed
            if (keysPressed[0]) deltaY -= effectiveShipSpeed * deltaTime; // Up
            if (keysPressed[1]) deltaY += effectiveShipSpeed * deltaTime; // Down
            if (keysPressed[2]) deltaX -= effectiveShipSpeed * deltaTime; // Left
            if (keysPressed[3]) deltaX += effectiveShipSpeed * deltaTime; // Right

            if (deltaX != 0 || deltaY != 0)
            {
                // Keep ship within bounds
                double newX = Math.Max(20, Math.Min(gameCanvas.ActualWidth - 20, shipPosition.X + deltaX));
                double newY = Math.Max(20, Math.Min(gameCanvas.ActualHeight - 20, shipPosition.Y + deltaY));

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
            int totalParticles = particleManager.ParticleCount;

            // Add level mechanic particles to the count
            totalParticles += levelManager.GetActiveLevelMechanicParticleCount();

            var uiData = new GameUIData();
            uiData.ParticleCount = totalParticles;

            if (gameRunning)
            {
                var elapsed = DateTime.Now - gameStartTime;
                uiData.GameTime = elapsed;
                uiData.LevelStatus = levelManager.GetLevelStatus();

                // Show active power-up effects
                if (powerUpManager.IsEffectActive("TimeWarp"))
                {
                    uiData.TimeWarpRemaining = powerUpManager.GetEffectRemainingTime("TimeWarp");
                }

                // Show super boost status
                uiData.IsSuperBoostActive = isSuperBoostActive;

                // Show stored power-ups
                uiData.SingularityCount = powerUpManager.GetStoredPowerUpCount("Singularity");
                uiData.RepulsorCount = powerUpManager.GetStoredPowerUpCount("Repulsor");

                // Update session stats
                if (currentSession != null)
                {
                    uiData.SessionStats = GetSessionStatsText();
                }
            }

            // Fire event to update UI
            UIUpdateRequested?.Invoke(uiData);
        }

        private string GetSessionStatsText()
        {
            if (currentSession.GamesPlayed == 0)
            {
                return "No games completed yet";
            }
            else
            {
                return $"Games: {currentSession.GamesPlayed} | " +
                       $"Best: {currentSession.LongestGame?.DurationSeconds:F1}s | " +
                       $"Avg: {currentSession.AverageGameTime:F1}s";
            }
        }

        #endregion

        #region Event Handlers

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
        }

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
        }

        private void OnParticleShipCollisionDetected()
        {
            StopGame();
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

        private async void OnGameCompleted(Game game)
        {
            // Save game to Firebase leaderboard if it's a Survival or Maze game
            if (game.GameMode == Settings.Models.GameMode.Survival || game.GameMode == Settings.Models.GameMode.Maze)
            {
                // Use the new leaderboard system which handles both saving to Firebase and maintaining top 10
                string? firebaseKey = await currentSession.AddGameToLeaderboardAsync(game);
                
                if (firebaseKey != null)
                {
                    DebugHelper.WriteLine($"Game saved to {game.GameMode} leaderboard with Firebase key: {firebaseKey}");
                    GameEvents.RaiseMessageRequested($"{game.GameMode} game saved to leaderboard", Brushes.LightGreen);
                }
                else
                {
                    // Game either didn't qualify for leaderboard or Firebase error occurred
                    DebugHelper.WriteLine($"Game not saved to leaderboard - may not qualify for top 10 or Firebase unavailable");
                    GameEvents.RaiseMessageRequested("Game completed but not saved to leaderboard", Brushes.Orange);
                }
            }
            else
            {
                // For Standard mode games, just log completion (no leaderboard)
                DebugHelper.WriteLine($"Standard mode game completed: {game.DurationSeconds:F1}s - not saved to Firebase leaderboard");
            }
        }

        #endregion

        #region Firebase Integration

        // Firebase integration is now handled by Session.AddGameToLeaderboardAsync()
        // This section is kept for any future direct Firebase operations if needed

        #endregion

        #region Disposal

        /// <summary>
        /// Clean up resources when disposing the game engine
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromGameEvents();
            levelManager?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Data structure for passing UI update information from GameEngine to MainWindow
    /// </summary>
    public class GameUIData
    {
        public int ParticleCount { get; set; }
        public TimeSpan GameTime { get; set; }
        public string LevelStatus { get; set; } = string.Empty;
        public double? TimeWarpRemaining { get; set; }
        public bool IsSuperBoostActive { get; set; }
        public int SingularityCount { get; set; }
        public int RepulsorCount { get; set; }
        public string SessionStats { get; set; } = string.Empty;
    }
}