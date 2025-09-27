using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects.PowerUps;
using tkkn2025.GameObjects.Ship;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.Settings;
using tkkn2025.DataAccess;
using tkkn2025.Helpers;
using tkkn2025.Settings.Models;
using tkkn2025.Core.GameModes.SurvivalMode;
using tkkn2025.Core.GameModes.MazeMode;
using tkkn2025.Core.Entities.Ship;

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
        private Ship ship = null!;
        private ParticleManager particleManager = null!;
        private PowerUpManager powerUpManager = null!;
        private Maze mazeGame = null!;
        private Random random = null!;

        // Game state tracking
        private bool gameRunning = false;
        private Point centerScreen;

        // Game loop timing
        private DateTime lastUpdate = DateTime.Now;
        private DateTime lastParticleGeneration = DateTime.Now;
        private DateTime gameStartTime;
        private DateTime lastUIUpdate = DateTime.Now;
        private DateTime lastFPSUpdate = DateTime.Now;
        private int frameCount = 0;

        // Active game settings - snapshot taken when game starts
        private double activeLevelDuration;
        private double activeNewParticlesPerLevel;

        // Power-up effects
        private double currentSpeedMultiplier = 1.0;

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

            particleManager = new ParticleManager(gameCanvas);
            powerUpManager = new PowerUpManager(gameCanvas, random);

            CreateShip();

            // Subscribe to game events
            SubscribeToGameEvents();

            // Debug initialization info
            DebugHelper.WriteLine("Game Engine initialization completed");
        }

        private void CreateShip()
        {
            centerScreen = new Point(gameCanvas.ActualWidth / 2, gameCanvas.ActualHeight / 2);
            ship = new Ship(gameCanvas, new Vector2((float)centerScreen.X, (float)centerScreen.Y));
        }

        #endregion

        #region Game State Management

        public GameMode GameMode { get; set; } = GameSettings.GameMode;

        /// <summary>
        /// Start a new game with current settings
        /// </summary>
        public void StartGame()
        {

            if (gameRunning) return;
            GameMode = GameSettings.GameMode;


            gameRunning = true;
            gameStartTime = DateTime.Now;
            lastUpdate = DateTime.Now;
            lastParticleGeneration = DateTime.Now;
            lastUIUpdate = DateTime.Now;
            lastFPSUpdate = DateTime.Now;
            frameCount = 0;
            currentSpeedMultiplier = 1.0;


            // Snapshot current settings as active settings for this game
            activeLevelDuration = GameSettings.LevelDuration.Value;
            activeNewParticlesPerLevel = GameSettings.NewParticlesPerLevel.Value;


            // Start a new game in the session (this creates a copy of the current config)
            currentGame = currentSession.StartNewGame();

            // Reset ship position to current center and show neutral
            centerScreen = new Point(gameCanvas.ActualWidth / 2, gameCanvas.ActualHeight / 2);
            ship.UpdateGameSettings(); // Update ship settings from current game settings
            ship.UpdateCanvasDimensions(); // Update canvas bounds
            ship.Reset(centerScreen);

            UpdateCanvasDimensions();


            // Initialize
            if (GameMode == GameMode.Survival)
            {

                // Initialize particle controller with game settings
                ParticleManager.InitializeGameSettings();
                // Start new game through particle controller
                particleManager.StartNewGame();

                // Start new game through power-up manager
                powerUpManager.StartNewGame();

            }

            else if (GameMode == GameMode.Maze)
            {
                mazeGame = new Maze(MazeGenerator.GenerateMaze_Medium(ship, gameCanvas.ActualWidth, gameCanvas.ActualHeight), gameCanvas, ship);
                UpdateCanvasDimensions();
                //mazeGame.Start();
            }

            // Raise game started event
            GameEvents.RaiseGameStarted();
        }


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


            #region Survival Mode
            // Update current speed multiplier from power-ups
            currentSpeedMultiplier = powerUpManager.GetSpeedMultiplier();

            // Apply speed multiplier to delta time for time-based effects
            var effectiveDeltaTime = deltaTime * currentSpeedMultiplier;

            UpdateFPS();
            
            // Update ship position
            ship.UpdatePosition(effectiveDeltaTime);

            // Update power-ups (use normal delta time for power-up timing)
            var shipVector = new Vector2((float)ship.ShipPosition.X, (float)ship.ShipPosition.Y);
            powerUpManager.Update(deltaTime, shipVector);

            // Check power-up collisions
            powerUpManager.CheckCollisions(ship.ShipPosition);


            // Update particles through controller (with speed multiplier and power-up manager)
            particleManager.UpdateParticles(effectiveDeltaTime, ship.ShipPosition, powerUpManager);

            // Check collisions through controller
            particleManager.CheckCollisions(ship.ShipPosition);

            // Handle particle generation timing (use normal delta time for consistent spawning)
            if ((now - lastParticleGeneration).TotalSeconds >= activeLevelDuration)
            {
                particleManager.GenerateMoreParticles(activeNewParticlesPerLevel);
                lastParticleGeneration = now;
            }


            #endregion


            #region Maze Mode
            // Update level manager (use normal delta time for level progression)
            mazeGame.Update(deltaTime);

            // Check level mechanic collisions
            if (mazeGame.CheckParticleMechanicsCollisions(ship.ShipPosition))
            {
                // Collision with level mechanic particle detected
                OnParticleShipCollisionDetected();
                return;
            }
            #endregion


            // Update UI periodically (not every frame)
            if ((now - lastUIUpdate).TotalSeconds >= 0.1) // 10 times per second
            {
                UpdateUI();
                lastUIUpdate = now;
            }
        }

        #endregion

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
                totalParticles += mazeGame.GetActiveLevelMechanicParticleCount();

                currentSession.CompleteCurrentGame(totalParticles);
                DebugHelper.WriteLine($"Game completed: {currentGame.DurationSeconds:F1}s with {currentGame.FinalParticleCount} particles (Level {mazeGame.CurrentLevel})");
                DebugHelper.WriteLine($"Session stats: {currentSession.GetSessionStats()}");

                // Raise game completed event
                GameEvents.RaiseGameCompleted(currentGame);
            }

            // Stop all level mechanics
            mazeGame.StopAllMechanics();

            // Fire event to notify UI
            GameStopped?.Invoke();
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

            // Let ship handle movement and boost keys
            if (ship.HandleKeyDown(key))
            {
                return true;
            }

            // Handle non-ship keys
            switch (key)
            {
                case Key.T:
                    // Debug key to manually trigger level 3 mechanics for testing
                    mazeGame.TriggerMechanicsForLevel(3);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle key up events
        /// </summary>
        /// <param name="key">The key that was released</param>
        /// <returns>True if the key was handled</returns>
        public bool HandleKeyUp(Key key)
        {
            // Let ship handle movement and boost keys
            return ship.HandleKeyUp(key);
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
                var shipVector = new Vector2((float)ship.ShipPosition.X, (float)ship.ShipPosition.Y);

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
            if (mazeGame is not null)
            {
                mazeGame.Reset(gameCanvas);
            }
            // Update center screen position based on current canvas size
            centerScreen = new Point(gameCanvas.ActualWidth / 2, gameCanvas.ActualHeight / 2);
        }

        private void UpdateFPS()
        {
            frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - lastFPSUpdate).TotalSeconds;

            if (elapsed >= 0.5) // Update interval in seconds
            {
                double fps = frameCount / elapsed;

                // Fire event to update UI
                FPSUpdateRequested?.Invoke(fps);

                frameCount = 0;
                lastFPSUpdate = now;
            }
        }

        private void UpdateUI()
        {
            int totalParticles = particleManager.ParticleCount;

            // Add level mechanic particles to the count
            totalParticles += mazeGame.GetActiveLevelMechanicParticleCount();

            var uiData = new GameUIData();
            uiData.ParticleCount = totalParticles;

            if (gameRunning)
            {
                var elapsed = DateTime.Now - gameStartTime;
                uiData.GameTime = elapsed;
                uiData.LevelStatus = mazeGame.GetLevelStatus();

                // Show active power-up effects
                if (powerUpManager.IsEffectActive("TimeWarp"))
                {
                    uiData.TimeWarpRemaining = powerUpManager.GetEffectRemainingTime("TimeWarp");
                }

                // Show super boost status from ship
                uiData.IsSuperBoostActive = ship.IsSuperBoostActive;

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
            ship?.Dispose();
            mazeGame?.Dispose();
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