using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using tkkn2025.GameObjects;
using tkkn2025.Settings;

namespace tkkn2025.GameObjects.PowerUps
{
    /// <summary>
    /// Manages power-ups in the game including spawning, collision detection, and effects
    /// </summary>
    public class PowerUpManager
    {
        public static Brush TimeWarpBrush { get; set; } = Brushes.DarkBlue;
        public static Brush SingularityBrush { get; set; } = Brushes.DarkRed;
        public static Brush RepulsorBrush { get; set; } = Brushes.DarkGreen;

               
        private readonly Canvas gameCanvas;
        private readonly Random random;
        private readonly List<PowerUp> activePowerUps = new List<PowerUp>();
        private readonly List<ActivePowerUpEffect> activePowerUpEffects = new List<ActivePowerUpEffect>();
        private readonly List<Singularity> activeSingularities = new List<Singularity>();
        private readonly List<Repulsor> activeRepulsors = new List<Repulsor>();
        
        // Stored power-ups (not yet activated)
        private readonly Dictionary<string, int> storedPowerUps = new Dictionary<string, int>();
        
        // Power-up spawning
        private DateTime lastPowerUpSpawn = DateTime.Now;
        
        // Canvas dimensions
        private double canvasWidth;
        private double canvasHeight;
        
        // Physics constants
        private const float GravitationalConstant = 50000f; // Adjusted for game scale

        public PowerUpManager(Canvas canvas, Random randomGenerator)
        {
            gameCanvas = canvas;
            random = randomGenerator;
            UpdateCanvasDimensions();
        }

        /// <summary>
        /// Update canvas dimensions when window is resized
        /// </summary>
        public void UpdateCanvasDimensions()
        {
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
        }

        /// <summary>
        /// Start a new game by clearing all power-ups and effects
        /// </summary>
        public void StartNewGame()
        {
            ClearAllPowerUps();
            ClearAllEffects();
            ClearAllSingularities();
            ClearAllRepulsors();
            storedPowerUps.Clear();
            lastPowerUpSpawn = DateTime.Now;
        }

        /// <summary>
        /// Update power-ups and their effects
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <param name="shipPosition">Current ship position for repulsor following</param>
        public void Update(double deltaTime, Vector2 shipPosition)
        {
            // Check if it's time to spawn a new power-up
            if ((DateTime.Now - lastPowerUpSpawn).TotalSeconds >= GameSettings.PowerUpSpawnRate)
            {
                SpawnRandomPowerUp();
                lastPowerUpSpawn = DateTime.Now;
            }

            // Update active power-up effects
            UpdateActivePowerUpEffects(deltaTime);
            
            // Update active singularities
            UpdateActiveSingularities(deltaTime);
            
            // Update active repulsors
            UpdateActiveRepulsors(shipPosition, deltaTime);
        }

        /// <summary>
        /// Check for collisions between power-ups and ship
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if any power-up was collected</returns>
        public bool CheckCollisions(Point shipPosition)
        {
            bool collected = false;
            var powerUpsToRemove = new List<PowerUp>();

            foreach (var powerUp in activePowerUps)
            {
                double distance = Math.Sqrt(
                    Math.Pow(shipPosition.X - powerUp.Position.X, 2) +
                    Math.Pow(shipPosition.Y - powerUp.Position.Y, 2));

                if (distance < 25) // Collision threshold
                {
                    CollectPowerUp(powerUp);
                    powerUpsToRemove.Add(powerUp);
                    collected = true;
                }
            }

            // Remove collected power-ups
            foreach (var powerUp in powerUpsToRemove)
            {
                RemovePowerUp(powerUp);
            }

            return collected;
        }

        /// <summary>
        /// Try to activate a stored Singularity power-up at the specified location
        /// </summary>
        /// <param name="position">Position to activate the singularity</param>
        /// <returns>True if a singularity was activated</returns>
        public bool TryActivateSingularity(Vector2 position)
        {
            if (storedPowerUps.ContainsKey("Singularity") && storedPowerUps["Singularity"] > 0)
            {
                // Use one stored singularity
                storedPowerUps["Singularity"]--;
                if (storedPowerUps["Singularity"] == 0)
                {
                    storedPowerUps.Remove("Singularity");
                }

                // Create and activate the singularity
                var singularity = new Singularity(position);
                if (singularity.Visual != null)
                {
                    Canvas.SetLeft(singularity.Visual, position.X - 25); // Center the 50px visual
                    Canvas.SetTop(singularity.Visual, position.Y - 25);
                    gameCanvas.Children.Add(singularity.Visual);
                }
                
                activeSingularities.Add(singularity);
                
                // Use GameEvents instead of direct event
                GameEvents.RaiseSingularityActivated(position);
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Try to activate a stored Repulsor power-up at the specified location
        /// </summary>
        /// <param name="position">Position to activate the repulsor</param>
        /// <returns>True if a repulsor was activated</returns>
        public bool TryActivateRepulsor(Vector2 position)
        {
            if (storedPowerUps.ContainsKey("Repulsor") && storedPowerUps["Repulsor"] > 0)
            {
                // Use one stored repulsor
                storedPowerUps["Repulsor"]--;
                if (storedPowerUps["Repulsor"] == 0)
                {
                    storedPowerUps.Remove("Repulsor");
                }

                // Create and activate the repulsor (negative mass for repelling)
                var repulsor = new Repulsor(position);
                if (repulsor.Visual != null)
                {
                    Canvas.SetLeft(repulsor.Visual, position.X - 50); // Center the 100px visual
                    Canvas.SetTop(repulsor.Visual, position.Y - 50);
                    gameCanvas.Children.Add(repulsor.Visual);
                }
                
                activeRepulsors.Add(repulsor);
                
                // Use GameEvents instead of direct event
                GameEvents.RaiseRepulsorActivated(position);
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// Apply gravitational forces from all active singularities to particles
        /// </summary>
        /// <param name="particlePosition">Particle position (will be modified)</param>
        /// <param name="particleVelocity">Particle velocity (will be modified)</param>
        /// <param name="deltaTime">Time step</param>
        public void ApplySingularityForces(ref Vector2 particlePosition, ref Vector2 particleVelocity, float deltaTime)
        {
            foreach (var singularity in activeSingularities)
            {
                singularity.ApplyForceToParticle(ref particlePosition, ref particleVelocity, 1f, GravitationalConstant, deltaTime);
            }
        }

        /// <summary>
        /// Apply repelling forces from all active repulsors to particles
        /// </summary>
        /// <param name="particlePosition">Particle position (will be modified)</param>
        /// <param name="particleVelocity">Particle velocity (will be modified)</param>
        /// <param name="deltaTime">Time step</param>
        public void ApplyRepulsorForces(ref Vector2 particlePosition, ref Vector2 particleVelocity, float deltaTime)
        {
            foreach (var repulsor in activeRepulsors)
            {
                repulsor.ApplyForceToParticle(ref particlePosition, ref particleVelocity, 1f, GravitationalConstant, deltaTime);
            }
        }

        /// <summary>
        /// Apply both singularity and repulsor forces to particles
        /// </summary>
        /// <param name="particlePosition">Particle position (will be modified)</param>
        /// <param name="particleVelocity">Particle velocity (will be modified)</param>
        /// <param name="deltaTime">Time step</param>
        public void ApplyAllForces(ref Vector2 particlePosition, ref Vector2 particleVelocity, float deltaTime)
        {
            ApplySingularityForces(ref particlePosition, ref particleVelocity, deltaTime);
            ApplyRepulsorForces(ref particlePosition, ref particleVelocity, deltaTime);
        }

        /// <summary>
        /// Get the current speed multiplier for TimeWarp effect
        /// </summary>
        /// <returns>Speed multiplier (1.0 = normal speed)</returns>
        public double GetSpeedMultiplier()
        {
            var timeWarpEffect = activePowerUpEffects.FirstOrDefault(e => e.Type == "TimeWarp");
            return timeWarpEffect?.EffectFactor ?? 1.0;
        }

        /// <summary>
        /// Check if a specific power-up effect is active
        /// </summary>
        /// <param name="effectType">Type of effect to check</param>
        /// <returns>True if the effect is active</returns>
        public bool IsEffectActive(string effectType)
        {
            return activePowerUpEffects.Any(e => e.Type == effectType);
        }

        /// <summary>
        /// Get remaining time for a specific effect
        /// </summary>
        /// <param name="effectType">Type of effect</param>
        /// <returns>Remaining time in seconds, or 0 if not active</returns>
        public double GetEffectRemainingTime(string effectType)
        {
            var effect = activePowerUpEffects.FirstOrDefault(e => e.Type == effectType);
            return effect?.RemainingTime ?? 0.0;
        }

        /// <summary>
        /// Get the number of stored power-ups of a specific type
        /// </summary>
        /// <param name="powerUpType">Type of power-up</param>
        /// <returns>Number of stored power-ups</returns>
        public int GetStoredPowerUpCount(string powerUpType)
        {
            return storedPowerUps.ContainsKey(powerUpType) ? storedPowerUps[powerUpType] : 0;
        }

        /// <summary>
        /// Get all stored power-up counts
        /// </summary>
        /// <returns>Dictionary of power-up types and their counts</returns>
        public Dictionary<string, int> GetAllStoredPowerUps()
        {
            return new Dictionary<string, int>(storedPowerUps);
        }

        private void SpawnRandomPowerUp()
        {
            var position = GetRandomSpawnPosition();
            PowerUp powerUp;
            PowerUpControl visual;

            // Randomly choose between TimeWarp, Singularity, and Repulsor
            double randomValue = random.NextDouble();
            if (randomValue < 0.33) // 33% chance for TimeWarp
            {
                // TimeWarp power-up
                powerUp = new PowerUp(position, "TimeWarp", "Slows down time for 3 seconds", 0.5, 3.0)
                {
                    IsActive = true
                };
                visual = new PowerUpControl();
                visual.SphereColor = TimeWarpBrush; // Gold for TimeWarp
            }
            else if (randomValue < 0.66) // 33% chance for Singularity
            {
                // Singularity power-up
                powerUp = new PowerUp(position, "Singularity", "Creates a gravity well when activated", 1000f, 5.0)
                {
                    IsActive = true
                };
                visual = new PowerUpControl();
                visual.SphereColor = SingularityBrush; // Red for Singularity
            }
            else // 33% chance for Repulsor
            {
                // Repulsor power-up
                powerUp = new PowerUp(position, "Repulsor", "Creates a repelling field that follows you when activated", -1000f, 5.0)
                {
                    IsActive = true
                };
                visual = new PowerUpControl();
                visual.SphereColor = RepulsorBrush; // Green for Repulsor
            }

            //Spawn Default Power Up for testing
            if (false)
            {
                powerUp = new PowerUp(position, "Repulsor", "Creates a repelling field that follows you when activated", -1000f, 500.0)
                {
                    IsActive = true
                };
                visual = new PowerUpControl();
                visual.SphereColor = RepulsorBrush; // Green for Repulsor

            }
            // Position the visual
            Canvas.SetLeft(visual, position.X - 15); // Center the 30px wide control
            Canvas.SetTop(visual, position.Y - 15);   // Center the 30px high control

            powerUp.Visual = visual;
            gameCanvas.Children.Add(visual);
            activePowerUps.Add(powerUp);
        }

        private Vector2 GetRandomSpawnPosition()
        {
            // Spawn power-ups away from edges to ensure they're fully visible
            double margin = 50;
            double x = random.NextDouble() * (canvasWidth - 2 * margin) + margin;
            double y = random.NextDouble() * (canvasHeight - 2 * margin) + margin;
            return new Vector2((float)x, (float)y);
        }

        private void CollectPowerUp(PowerUp powerUp)
        {
            // Use GameEvents instead of direct event
            GameEvents.RaisePowerUpCollected(powerUp.Type);

            // Apply the power-up effect
            switch (powerUp.Type)
            {
                case "TimeWarp":
                    ApplyTimeWarpEffect(powerUp);
                    break;
                case "Singularity":
                    StoreSingularityPowerUp(powerUp);
                    break;
                case "Repulsor":
                    StoreRepulsorPowerUp(powerUp);
                    break;
                // Add more power-up types here in the future
            }
        }

        private void ApplyTimeWarpEffect(PowerUp powerUp)
        {
            // Remove any existing TimeWarp effect
            var existingEffect = activePowerUpEffects.FirstOrDefault(e => e.Type == "TimeWarp");
            if (existingEffect != null)
            {
                activePowerUpEffects.Remove(existingEffect);
            }

            // Add new TimeWarp effect
            var effect = new ActivePowerUpEffect
            {
                Type = powerUp.Type,
                EffectFactor = powerUp.EffectFactor,
                RemainingTime = powerUp.Duration,
                StartTime = DateTime.Now
            };

            activePowerUpEffects.Add(effect);
            
            // Use GameEvents instead of direct event
            GameEvents.RaisePowerUpEffectStarted(powerUp.Type, powerUp.Duration);
        }

        private void StoreSingularityPowerUp(PowerUp powerUp)
        {
            // Store the singularity for later use
            if (!storedPowerUps.ContainsKey("Singularity"))
            {
                storedPowerUps["Singularity"] = 0;
            }
            storedPowerUps["Singularity"]++;
            
            // Use GameEvents instead of direct event
            GameEvents.RaisePowerUpStored("Singularity");
        }

        private void StoreRepulsorPowerUp(PowerUp powerUp)
        {
            // Store the repulsor for later use
            if (!storedPowerUps.ContainsKey("Repulsor"))
            {
                storedPowerUps["Repulsor"] = 0;
            }
            storedPowerUps["Repulsor"]++;
            
            // Use GameEvents instead of direct event
            GameEvents.RaisePowerUpStored("Repulsor");
        }

        private void UpdateActivePowerUpEffects(double deltaTime)
        {
            var effectsToRemove = new List<ActivePowerUpEffect>();

            foreach (var effect in activePowerUpEffects)
            {
                effect.RemainingTime -= deltaTime;
                
                if (effect.RemainingTime <= 0)
                {
                    effectsToRemove.Add(effect);
                    
                    // Use GameEvents instead of direct event
                    GameEvents.RaisePowerUpEffectEnded(effect.Type);
                }
            }

            foreach (var effect in effectsToRemove)
            {
                activePowerUpEffects.Remove(effect);
            }
        }

        private void UpdateActiveSingularities(double deltaTime)
        {
            var singularitiesToRemove = new List<Singularity>();

            foreach (var singularity in activeSingularities)
            {
                if (!singularity.Update(deltaTime))
                {
                    singularitiesToRemove.Add(singularity);
                }
            }

            foreach (var singularity in singularitiesToRemove)
            {
                RemoveSingularity(singularity);
            }
        }

        private void UpdateActiveRepulsors(Vector2 shipPosition, double deltaTime)
        {
            var repulsorsToRemove = new List<Repulsor>();

            foreach (var repulsor in activeRepulsors)
            {
                if (!repulsor.Update(shipPosition, deltaTime))
                {
                    repulsorsToRemove.Add(repulsor);
                }
                else
                {
                    // Update visual position to follow ship
                    if (repulsor.Visual != null)
                    {
                        Canvas.SetLeft(repulsor.Visual, shipPosition.X - 50);
                        Canvas.SetTop(repulsor.Visual, shipPosition.Y - 50);
                    }
                }
            }

            foreach (var repulsor in repulsorsToRemove)
            {
                RemoveRepulsor(repulsor);
            }
        }

        private void RemovePowerUp(PowerUp powerUp)
        {
            activePowerUps.Remove(powerUp);
            if (powerUp.Visual != null)
            {
                gameCanvas.Children.Remove(powerUp.Visual);
            }
        }

        private void RemoveSingularity(Singularity singularity)
        {
            activeSingularities.Remove(singularity);
            if (singularity.Visual != null)
            {
                gameCanvas.Children.Remove(singularity.Visual);
            }
        }

        private void RemoveRepulsor(Repulsor repulsor)
        {
            activeRepulsors.Remove(repulsor);
            repulsor.StopAnimations(); // Stop animations before removing
            if (repulsor.Visual != null)
            {
                gameCanvas.Children.Remove(repulsor.Visual);
            }
        }

        private void ClearAllPowerUps()
        {
            foreach (var powerUp in activePowerUps)
            {
                if (powerUp.Visual != null)
                {
                    gameCanvas.Children.Remove(powerUp.Visual);
                }
            }
            activePowerUps.Clear();
        }

        private void ClearAllEffects()
        {
            activePowerUpEffects.Clear();
        }

        private void ClearAllSingularities()
        {
            foreach (var singularity in activeSingularities)
            {
                if (singularity.Visual != null)
                {
                    gameCanvas.Children.Remove(singularity.Visual);
                }
            }
            activeSingularities.Clear();
        }

        private void ClearAllRepulsors()
        {
            foreach (var repulsor in activeRepulsors)
            {
                repulsor.StopAnimations();
                if (repulsor.Visual != null)
                {
                    gameCanvas.Children.Remove(repulsor.Visual);
                }
            }
            activeRepulsors.Clear();
        }

        /// <summary>
        /// Represents an active power-up effect
        /// </summary>
        private class ActivePowerUpEffect
        {
            public string Type { get; set; } = string.Empty;
            public double EffectFactor { get; set; }
            public double RemainingTime { get; set; }
            public DateTime StartTime { get; set; }
        }
    }
}