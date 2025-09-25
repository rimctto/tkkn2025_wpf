using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Level mechanic that launches particles from the top of the screen straight down
    /// Particles are evenly spaced across the width and launched at timed intervals
    /// </summary>
    public class SweepMechanic : ParticleMechanicsBase
    {
        private DispatcherTimer launchTimer;
        
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        private readonly double launchInterval; // seconds between particle launches
        private readonly bool reverse; // whether to launch particles from right to left
        
        // Sweep state
        private int particlesLaunched;
        private List<Vector2> launchPositions;
        
        // Particle properties
        private readonly double particleSpeed; // Configurable speed for sweep particles
        private readonly Brush particleColor = Brushes.Yellow; // Distinct color for sweep particles

        public SweepMechanic(int activationLevel = 3, int particleCount = 30, double launchTiming = 0.5, bool reverse = false, double particleSpeed = 200.0)
           
        {
            ActivationLevel = activationLevel;
            ParticleCount = Math.Max(1, particleCount);
            launchInterval = Math.Max(0.1, launchTiming);
            this.reverse = reverse;
            this.particleSpeed = Math.Max(50.0, particleSpeed); // Ensure minimum speed
            
            launchTimer = new DispatcherTimer();
            launchTimer.Tick += LaunchTimer_Tick;
            
            launchPositions = new List<Vector2>();

            System.Diagnostics.Debug.WriteLine($"?? StraightSweep mechanic created: Level {ActivationLevel}, {ParticleCount} particles, {launchInterval}s intervals, {this.particleSpeed} speed, reverse: {reverse}");
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Calculate launch positions evenly spaced across the top of the screen
            CalculateLaunchPositions();
            
            // Reset state
            particlesLaunched = 0;
            
            // Configure and start the timer
            launchTimer.Interval = TimeSpan.FromSeconds(launchInterval);
            launchTimer.Start();
            
            System.Diagnostics.Debug.WriteLine($"?? StraightSweep activated: {ParticleCount} particles, {launchInterval}s intervals, {particleSpeed} speed");
        }

        /// <summary>
        /// Called when the mechanic is stopped
        /// </summary>
        protected override void OnStop()
        {
            if (launchTimer.IsEnabled)
            {
                launchTimer.Stop();
            }
            
            particlesLaunched = 0;
            
            System.Diagnostics.Debug.WriteLine("?? StraightSweep stopped and particles cleared");
        }

        /// <summary>
        /// Called every frame when the mechanic is active
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        protected override void OnUpdate(double deltaTime)
        {
            if (mechanicParticles.Count == 0) return;

            var particlesToRemove = new List<Particle>();

            foreach (var particle in mechanicParticles)
            {
                // Update particle position
                particle.Position += particle.Velocity * (float)deltaTime;
                
                // Update visual position
                if (particle.Visual != null)
                {
                    Canvas.SetLeft(particle.Visual, particle.Position.X);
                    Canvas.SetTop(particle.Visual, particle.Position.Y);
                }

                // Check if particle has left the screen (bottom edge)
                if (particle.Position.Y > canvasHeight + 20)
                {
                    particlesToRemove.Add(particle);
                }
            }

            // Remove particles that have left the screen
            foreach (var particle in particlesToRemove)
            {
                RemoveParticle(particle);
            }
        }

        /// <summary>
        /// Calculate evenly spaced launch positions across the top of the screen
        /// </summary>
        private void CalculateLaunchPositions()
        {
            var positions = GetEvenlySpacedTopPositions(ParticleCount);
            
            // If reverse is true, reverse the order of positions
            if (reverse)
            {
                positions.Reverse();
            }
            
            launchPositions = positions;
        }

        /// <summary>
        /// Timer tick handler for launching particles
        /// </summary>
        private void LaunchTimer_Tick(object? sender, EventArgs e)
        {
            if (particlesLaunched >= ParticleCount)
            {
                // All particles have been launched, stop the timer
                launchTimer.Stop();
                System.Diagnostics.Debug.WriteLine($"?? StraightSweep completed: All {ParticleCount} particles launched");
                return;
            }

            // Launch the next particle
            LaunchParticle(launchPositions[particlesLaunched]);
            particlesLaunched++;
        }

        /// <summary>
        /// Launch a single particle from the specified position
        /// </summary>
        /// <param name="position">Launch position</param>
        private void LaunchParticle(Vector2 position)
        {
            try
            {
                // Use current level speed instead of stored particleSpeed
                // Set direction to move straight down
                var direction = new Vector2(0, 1);

                // Create particle using the new level speed method
                var particle = CreateParticleWithLevelSpeed(position, direction, particleColor, 8.0);

                // Set additional properties
                particle.ShouldChaseShip = false; // Straight line movement
                particle.IsSpawnVectorTowardsShip = false;
                particle.IsFreshlySpawned = true;

                System.Diagnostics.Debug.WriteLine($"?? Sweep particle launched at ({position.X:F0}, {position.Y:F0}) with speed {particle.Speed} - {particlesLaunched + 1}/{ParticleCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error launching sweep particle: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public override void Dispose()
        {
            try
            {
                base.Dispose();
                
                if (launchTimer != null)
                {
                    launchTimer.Tick -= LaunchTimer_Tick;
                    launchTimer = null!;
                }
                
                System.Diagnostics.Debug.WriteLine("?? StraightSweep mechanic disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing StraightSweep mechanic: {ex.Message}");
            }
        }
    }
}