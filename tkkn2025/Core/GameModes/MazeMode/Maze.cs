using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using tkkn2025.Core.Behaviors;
using tkkn2025.Core.Entities.Ship;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;
using tkkn2025.Settings;

namespace tkkn2025.Core.GameModes.MazeMode
{
    /// <summary>
    /// Manages game levels and triggers level-specific mechanics
    /// </summary>
    public class Maze : GameMode_Base
    {

        public GameSettings_Maze Settings { get; set; } = new GameSettings_Maze();

        #region Static Canvas Variables (initialized once per game)

        protected static Canvas? gameCanvas;

        protected static double canvasWidth;
        protected static double canvasHeight;
        protected static Point centerScreen;

        #endregion

        #region Static Speed Management for Shared Reference

        /// <summary>
        /// Current level speed that all mechanics should use for new particles
        /// This is updated each level and shared across all mechanics
        /// </summary>
        public static double CurrentLevelSpeed { get; private set; } = GameSettings.MazeMode_InitialParticleSpeed.Value;
        
        /// <summary>
        /// Target speed we're ramping toward
        /// </summary>
        private static double targetLevelSpeed = GameSettings.MazeMode_InitialParticleSpeed.Value;

        /// <summary>
        /// Speed increase rate per second for smooth ramping
        /// /// </summary>
        private static double speedRampRate = 5.0; // Speed units per second

        /// <summary>
        /// Update the current level speed for all mechanics to use
        /// Now implements smooth speed ramping instead of instant changes
        /// </summary>
        /// <param name="level">Current level</param>
        public static void UpdateCurrentLevelSpeed(int level)
        {
            double newTargetSpeed = GameSettings.MazeMode_InitialParticleSpeed.Value + level * GameSettings.MazeMode_SpeedIncreasePerLevel.Value;
            targetLevelSpeed = newTargetSpeed;
        }

        /// <summary>
        /// Update the speed ramping system - call this each frame
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private static void UpdateSpeedRamping(double deltaTime)
        {
            if (Math.Abs(CurrentLevelSpeed - targetLevelSpeed) > 0.1)
            {
                double speedDifference = targetLevelSpeed - CurrentLevelSpeed;
                double speedChange = Math.Sign(speedDifference) * GameSettings.MazeMode_SpeedRamprate * deltaTime;
                
                // Don't overshoot the target
                if (Math.Abs(speedChange) > Math.Abs(speedDifference))
                {
                    CurrentLevelSpeed = targetLevelSpeed;
                }
                else
                {
                    CurrentLevelSpeed += speedChange;
                }
            }
        }

        /// <summary>
        /// Frame counter for debug logging
        /// </summary>
        private static int frameCounter = 0;

        #endregion

        private int currentLevel;
        private DateTime gameStartTime;
        private DateTime lastLevelUpTime;
        private double levelDuration;


        private readonly List<IParticleMechanic> allMechanics = new List<IParticleMechanic>();
        private readonly List<IParticleMechanic> sequenceMechanics = new List<IParticleMechanic>();
        private readonly Ship ship;



        private readonly List<IParticleMechanic> levelMechanics = new List<IParticleMechanic>();

        public int CurrentLevel => currentLevel;

        public event EventHandler<int>? LevelChanged;
        public event EventHandler<string>? LevelMechanicTriggered;
 
        public Maze()
        {
            Name = "Maze Mode";
            Description = "Navigate through a maze with increasing speed and obstacles.";

            sequenceMechanics = new List<IParticleMechanic>();
        }

        public Maze(List<IParticleMechanic> sequenceMechanics, Canvas gameCanvas, Ship ship)
        {
            CurrentLevelSpeed = GameSettings.MazeMode_InitialParticleSpeed.Value;

            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);

            this.sequenceMechanics = sequenceMechanics ?? throw new ArgumentNullException(nameof(sequenceMechanics));
            this.ship = ship;
            allMechanics.AddRange(this.sequenceMechanics);
            allMechanics.AddRange(levelMechanics);

            Reset(gameCanvas);
        }

        public void Start()
        {
            if (GameSettings.AreMazeWallsEnabled)
            {
                sequenceMechanics[0].Activate();
                sequenceMechanics[1].Activate(); 
            }
        }
      
        


       

        



      
        /// <summary>
        /// Reset level manager for a new game
        /// </summary>
        public void Reset(Canvas gameCanvas)
        {
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);


            ParticleMechanicsBase.UpdateCanvasDimensions(gameCanvas);
            
            currentLevel = 1;
            gameStartTime = DateTime.Now;
            lastLevelUpTime = gameStartTime;
            levelDuration = GameSettings.LevelDuration.Value;

            // Initialize the global level speed for level 1
            UpdateCurrentLevelSpeed(currentLevel);

            // Reset current speed to initial value for smooth ramping
            CurrentLevelSpeed = GameSettings.MazeMode_InitialParticleSpeed.Value + currentLevel * GameSettings.MazeMode_SpeedIncreasePerLevel.Value;
            targetLevelSpeed = CurrentLevelSpeed;
            frameCounter = 0;

            // Stop all active mechanics
            foreach (var mechanic in allMechanics)
            {
                mechanic.Stop();
            }
            
        }

        /// <summary>
        /// Update level progression based on elapsed time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(double deltaTime)
        {
            frameCounter++;
            
            // Update speed ramping system for smooth speed transitions
            UpdateSpeedRamping(deltaTime);
            
            var now = DateTime.Now;
            var timeSinceLastLevel = (now - lastLevelUpTime).TotalSeconds;

            // Check if it's time to level up
            if (timeSinceLastLevel >= levelDuration)
            {
                LevelUp();
                lastLevelUpTime = now;
            }

            // Update all active mechanics
            UpdateActiveMechanics(deltaTime);
        }

        /// <summary>
        /// Force level up
        /// </summary>
        public void LevelUp()
        {
            currentLevel++;
            
            System.Diagnostics.Debug.WriteLine($"?? Level Up! Now at level {currentLevel}");
            
            // Update the global level speed for all mechanics to use
            UpdateCurrentLevelSpeed(currentLevel);
            
            // Update all active particles to match current level speed
            UpdateAllParticleSpeeds();
            
            // Trigger level changed event
            LevelChanged?.Invoke(this, currentLevel);
            
            // Check for level-specific mechanics
            ActivateMechanicsForLevel(currentLevel);
        }

        /// <summary>
        /// Update all active particles from all mechanics to match current level speed
        /// This prevents faster particles from higher levels catching up to slower particles from previous levels
        /// </summary>
        private void UpdateAllParticleSpeeds()
        {
            // Use the global CurrentLevelSpeed
            double currentLevelSpeed = CurrentLevelSpeed;
            
            // Update all active mechanics' particle speeds
            foreach (var mechanic in allMechanics.Where(m => m.IsActive))
            {
                // Call update speed method if the mechanic supports it
                if (mechanic is ParticleMechanicsBase baseMechanic)
                {
                    baseMechanic.UpdateParticleSpeed(currentLevelSpeed);
                }
            }
        }

        /// <summary>
        /// Activate all mechanics that should trigger at the specified level
        /// </summary>
        /// <param name="level">Current level</param>
        private void ActivateMechanicsForLevel(int level)
        {
           if  (GameSettings.AreMazeWallsEnabled == false) return;

            var mechanicsToActivate = allMechanics.Where(m => m.ActivationLevel == level).ToList();
            
            foreach (var mechanic in mechanicsToActivate)
            {
                try
                {
                    mechanic.Activate();
                    
                    string mechanicName = mechanic.GetType().Name.Replace("Mechanic", "");
                    string message = $"Level {level}: {mechanicName} activated! {mechanic.ParticleCount} particles incoming!";
                    
                    System.Diagnostics.Debug.WriteLine($"?? {message}");
                    LevelMechanicTriggered?.Invoke(this, message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error activating mechanic {mechanic.GetType().Name}: {ex.Message}");
                }
            }
            
            if (mechanicsToActivate.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"?? No mechanics to activate at level {level}");
            }
        }

        /// <summary>
        /// Update all active mechanics
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private void UpdateActiveMechanics(double deltaTime)
        {
            foreach (var mechanic in allMechanics.Where(m => m.IsActive))
            {
                mechanic.Update(deltaTime);
            }
        }

        /// <summary>
        /// Check for collisions with any active level mechanic particles
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public bool CheckParticleMechanicsCollisions(Point shipPosition)
        {
            return allMechanics.Where(m => m.IsActive).Any(m => m.CheckCollisions(shipPosition));
        }

        /// <summary>
        /// Get status information about current level and mechanics
        /// </summary>
        /// <returns>Level status string</returns>
        public string GetLevelStatus()
        {
            var timeSinceLastLevel = (DateTime.Now - lastLevelUpTime).TotalSeconds;
            var timeToNextLevel = Math.Max(0, levelDuration - timeSinceLastLevel);
            
            string status = $"Level {currentLevel}";
            
            if (timeToNextLevel > 0)
            {
                status += $" (Next in {timeToNextLevel:F1}s)";
            }
            
            // Add active mechanics status
            var activeMechanics = allMechanics.Where(m => m.IsActive).ToList();
            if (activeMechanics.Any())
            {
                var mechanicStatuses = activeMechanics.Select(m => 
                {
                    string name = m.GetType().Name.Replace("Mechanic", "");
                    return $"{name}: {m.GetActiveParticleCount()}";
                });
                status += $" | {string.Join(", ", mechanicStatuses)}";
            }

            return status;
        }

        /// <summary>
        /// Get the total number of level mechanic particles currently active
        /// </summary>
        /// <returns>Number of active level mechanic particles</returns>
        public int GetActiveLevelMechanicParticleCount()
        {
            return sequenceMechanics.Sum(m => m.GetActiveParticleCount());
        }

        /// <summary>
        /// Stop all level mechanics and clear particles
        /// </summary>
        public void StopAllMechanics()
        {
            foreach (var mechanic in allMechanics)
            {
                mechanic.Stop();
            }
            System.Diagnostics.Debug.WriteLine("?? All level mechanics stopped");
        }


        /// <summary>
        /// Manually trigger mechanics for a specific level (for testing)
        /// </summary>
        /// <param name="level">Level to trigger mechanics for</param>
        public void TriggerMechanicsForLevel(int level)
        {
            System.Diagnostics.Debug.WriteLine($"?? Manually triggering mechanics for level {level}");
            ActivateMechanicsForLevel(level);
        }

        /// <summary>
        /// Log the activation levels of all mechanics for debugging
        /// </summary>
        private void LogMechanicActivationLevels()
        {
            System.Diagnostics.Debug.WriteLine("?? Registered mechanics:");
            foreach (var mechanic in allMechanics.OrderBy(m => m.ActivationLevel))
            {
                string name = mechanic.GetType().Name.Replace("Mechanic", "");
                System.Diagnostics.Debug.WriteLine($"  - {name}: Level {mechanic.ActivationLevel} ({mechanic.ParticleCount} particles)");
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                StopAllMechanics();
                
                // Dispose individual mechanics if they implement IDisposable
                foreach (var mechanic in allMechanics)
                {
                    if (mechanic is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                allMechanics.Clear();
                sequenceMechanics.Clear();
                levelMechanics.Clear();
                
                System.Diagnostics.Debug.WriteLine("?? Level Manager disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Level Manager: {ex.Message}");
            }
        }
    }
}