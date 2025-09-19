using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Example level mechanic that demonstrates how to create new mechanics using the base class
    /// This mechanic spawns particles in a circular pattern around the center of the screen
    /// </summary>
    public class CircularSpawnMechanic : ParticleMechanicsBase
    {
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        
        // Circular spawn specific properties
        private readonly double particleSpeed;
        private readonly double spawnRadius;
        private readonly Brush particleColor;

        public CircularSpawnMechanic(System.Windows.Controls.Canvas canvas, Random randomGenerator, 
            int activationLevel = 5, int particleCount = 20, double speed = 150.0, double radius = 200.0)
            : base(canvas, randomGenerator)
        {
            ActivationLevel = activationLevel;
            ParticleCount = Math.Max(1, particleCount);
            particleSpeed = Math.Max(50.0, speed);
            spawnRadius = Math.Max(100.0, radius);
            particleColor = Brushes.Orange; // Distinct color for this mechanic
            
            System.Diagnostics.Debug.WriteLine($"?? CircularSpawn mechanic created: Level {ActivationLevel}, {ParticleCount} particles, {particleSpeed} speed");
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Spawn all particles at once in a circular pattern
            SpawnCircularPattern();
            
            System.Diagnostics.Debug.WriteLine($"?? CircularSpawn activated: {ParticleCount} particles spawned in circle");
        }

        /// <summary>
        /// Called when the mechanic is stopped
        /// </summary>
        protected override void OnStop()
        {
            System.Diagnostics.Debug.WriteLine("?? CircularSpawn stopped and particles cleared");
        }

        /// <summary>
        /// Called every frame when the mechanic is active
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        protected override void OnUpdate(double deltaTime)
        {
            // Use the base implementation which handles particle movement and cleanup
            base.OnUpdate(deltaTime);
            
            // Could add custom behavior here, like:
            // - Particle trail effects
            // - Special collision behavior
            // - Dynamic velocity changes
        }

        /// <summary>
        /// Spawn particles in a circular pattern around the center of the screen
        /// </summary>
        private void SpawnCircularPattern()
        {
            if (ParticleCount <= 0) return;

            // Calculate angle step for even distribution
            double angleStep = (2 * Math.PI) / ParticleCount;
            
            for (int i = 0; i < ParticleCount; i++)
            {
                // Calculate spawn position on circle
                double angle = i * angleStep;
                float spawnX = (float)(centerScreen.X + Math.Cos(angle) * spawnRadius);
                float spawnY = (float)(centerScreen.Y + Math.Sin(angle) * spawnRadius);
                var spawnPosition = new Vector2(spawnX, spawnY);
                
                // Calculate velocity toward center
                Vector2 direction = new Vector2((float)centerScreen.X, (float)centerScreen.Y) - spawnPosition;
                direction = Vector2.Normalize(direction);
                var velocity = direction * (float)particleSpeed;
                
                // Create particle using base class method
                CreateParticle(spawnPosition, velocity, particleColor, 10.0);
            }
        }
        
        /// <summary>
        /// Override collision detection to add special behavior (optional)
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public override bool CheckCollisions(Point shipPosition)
        {
            // Use base implementation, but could add custom collision behavior here
            bool collision = base.CheckCollisions(shipPosition);
            
            if (collision)
            {
                System.Diagnostics.Debug.WriteLine("?? CircularSpawn particle collision detected!");
            }
            
            return collision;
        }
    }
}