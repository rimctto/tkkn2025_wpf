using System;
using System.Collections.Generic;
using System.Linq;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.Settings;

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Manages game levels and triggers level-specific mechanics
    /// </summary>
    public class LevelManager
    {
        private int currentLevel;
        private DateTime gameStartTime;
        private DateTime lastLevelUpTime;
        private double levelDuration;
        private readonly List<IParticleMechanics> particleMechanics;

        public int CurrentLevel => currentLevel;
        public int TotalMechanicsCount => particleMechanics.Count;

        public event EventHandler<int>? LevelChanged;
        public event EventHandler<string>? LevelMechanicTriggered;

        public LevelManager(List<IParticleMechanics> mechanics)
        {
            particleMechanics = mechanics ?? throw new ArgumentNullException(nameof(mechanics));
            
            // Calculate total particle pool needed
            int totalParticles = particleMechanics.Sum(m => m.ParticleCount);
            System.Diagnostics.Debug.WriteLine($"?? Level Manager created with {particleMechanics.Count} mechanics, total particle pool: {totalParticles}");
            
            Reset();
        }

        /// <summary>
        /// Convenience constructor for single mechanic
        /// </summary>
        /// <param name="mechanic">Single particle mechanic</param>
        public LevelManager(IParticleMechanics mechanic) : this(new List<IParticleMechanics> { mechanic })
        {
        }

        /// <summary>
        /// Initialize static canvas variables for all particle mechanics
        /// This should be called once when the game starts to set up canvas dimensions
        /// </summary>
        /// <param name="canvas">Game canvas reference</param>
        public static void InitializeCanvasForMechanics(System.Windows.Controls.Canvas canvas)
        {
            ParticleMechanicsBase.InitializeStaticVariables(canvas);
            System.Diagnostics.Debug.WriteLine("?? Canvas initialized for all particle mechanics");
        }

        /// <summary>
        /// Update canvas dimensions for all particle mechanics
        /// This should be called when the window is resized
        /// </summary>
        public static void UpdateCanvasDimensionsForAllMechanics()
        {
            ParticleMechanicsBase.UpdateCanvasDimensions();
            System.Diagnostics.Debug.WriteLine("?? Canvas dimensions updated for all particle mechanics");
        }

        /// <summary>
        /// Reset static variables for a new game session
        /// </summary>
        public static void ResetStaticVariables()
        {
            ParticleMechanicsBase.ResetStaticVariables();
            System.Diagnostics.Debug.WriteLine("?? Static variables reset for new game session");
        }

        /// <summary>
        /// Reset level manager for a new game
        /// </summary>
        public void Reset()
        {
            currentLevel = 1;
            gameStartTime = DateTime.Now;
            lastLevelUpTime = gameStartTime;
            levelDuration = GameSettings.LevelDuration.Value;
            
            // Stop all active mechanics
            foreach (var mechanic in particleMechanics)
            {
                mechanic.Stop();
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Level Manager reset - Starting at level {currentLevel}");
            LogMechanicActivationLevels();
        }

        /// <summary>
        /// Update level progression based on elapsed time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(double deltaTime)
        {
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
            
            // Trigger level changed event
            LevelChanged?.Invoke(this, currentLevel);
            
            // Check for level-specific mechanics
            ActivateMechanicsForLevel(currentLevel);
        }

        /// <summary>
        /// Activate all mechanics that should trigger at the specified level
        /// </summary>
        /// <param name="level">Current level</param>
        private void ActivateMechanicsForLevel(int level)
        {
            var mechanicsToActivate = particleMechanics.Where(m => m.ActivationLevel == level).ToList();
            
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
            foreach (var mechanic in particleMechanics.Where(m => m.IsActive))
            {
                mechanic.Update(deltaTime);
            }
        }

        /// <summary>
        /// Check for collisions with any active level mechanic particles
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public bool CheckLevelMechanicCollisions(System.Windows.Point shipPosition)
        {
            return particleMechanics.Where(m => m.IsActive).Any(m => m.CheckCollisions(shipPosition));
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
            var activeMechanics = particleMechanics.Where(m => m.IsActive).ToList();
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
            return particleMechanics.Sum(m => m.GetActiveParticleCount());
        }

        /// <summary>
        /// Stop all level mechanics and clear particles
        /// </summary>
        public void StopAllMechanics()
        {
            foreach (var mechanic in particleMechanics)
            {
                mechanic.Stop();
            }
            System.Diagnostics.Debug.WriteLine("?? All level mechanics stopped");
        }

        /// <summary>
        /// Get time elapsed since game start
        /// </summary>
        /// <returns>Total game time in seconds</returns>
        public double GetTotalGameTime()
        {
            return (DateTime.Now - gameStartTime).TotalSeconds;
        }

        /// <summary>
        /// Get time elapsed since last level up
        /// </summary>
        /// <returns>Time since last level in seconds</returns>
        public double GetTimeSinceLastLevel()
        {
            return (DateTime.Now - lastLevelUpTime).TotalSeconds;
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
        /// Manually trigger a specific mechanic by name (for testing)
        /// </summary>
        /// <param name="mechanicName">Name of the mechanic to trigger</param>
        public void TriggerMechanic(string mechanicName)
        {
            var mechanic = particleMechanics.FirstOrDefault(m => 
                m.GetType().Name.Contains(mechanicName, StringComparison.OrdinalIgnoreCase));
            
            if (mechanic != null)
            {
                try
                {
                    mechanic.Activate();
                    System.Diagnostics.Debug.WriteLine($"?? Manually triggered mechanic: {mechanic.GetType().Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error manually triggering mechanic {mechanic.GetType().Name}: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unknown level mechanic: {mechanicName}");
            }
        }

        /// <summary>
        /// Get information about all registered mechanics
        /// </summary>
        /// <returns>List of mechanic information strings</returns>
        public List<string> GetMechanicInfo()
        {
            return particleMechanics.Select(m => 
                $"{m.GetType().Name}: Level {m.ActivationLevel}, {m.ParticleCount} particles, Active: {m.IsActive}"
            ).ToList();
        }

        /// <summary>
        /// Get list of mechanics for external access (needed for UpdateCanvasDimensions)
        /// </summary>
        /// <returns>List of mechanics</returns>
        public List<IParticleMechanics> GetMechanics()
        {
            return particleMechanics;
        }

        /// <summary>
        /// Log the activation levels of all mechanics for debugging
        /// </summary>
        private void LogMechanicActivationLevels()
        {
            System.Diagnostics.Debug.WriteLine("?? Registered mechanics:");
            foreach (var mechanic in particleMechanics.OrderBy(m => m.ActivationLevel))
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
                foreach (var mechanic in particleMechanics)
                {
                    if (mechanic is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                particleMechanics.Clear();
                
                System.Diagnostics.Debug.WriteLine("?? Level Manager disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Level Manager: {ex.Message}");
            }
        }
    }
}