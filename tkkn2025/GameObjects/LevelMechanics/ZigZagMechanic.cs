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
    /// Level mechanic that launches particles in a zig-zag pattern from the top of the screen
    /// Particles follow a diagonal path that changes direction at specified intervals
    /// </summary>
    public class ZigZagMechanic : ParticleMechanicsBase
    {
        private DispatcherTimer launchTimer;
        
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        private readonly double launchInterval; // seconds between particle launches
        private readonly bool reverse; // whether to launch particles from right to left
        private readonly double particleSpeed; // speed of particles
        
        // ZigZag specific configuration
        private readonly double startPosition; // starting position as percentage of canvas width (0.0 to 1.0)
        private readonly double width; // length of each zigzag segment as percentage of canvas width
        private readonly int repeat; // number of times the zigzag pattern doubles back
        
        // Sweep state
        private int particlesLaunched;
        private List<Vector2> launchPositions;
        
        // Particle properties
        private readonly Brush particleColor = Brushes.Orange; // Distinct color for zigzag particles

        public ZigZagMechanic(int activationLevel = 3, int particleCount = 30, double launchTiming = 0.5, 
                             bool reverse = false, double particleSpeed = 200.0, 
                             double startPosition = 0.2, double width = 0.6, int repeat = 1)
        {
            ActivationLevel = activationLevel;
            ParticleCount = Math.Max(1, particleCount);
            launchInterval = Math.Max(0.001, launchTiming);
            this.reverse = reverse;
            this.particleSpeed = Math.Max(50.0, particleSpeed);
            
            // Clamp percentages to valid ranges
            this.startPosition = Math.Clamp(startPosition, 0.0, 1.0);
            this.width = Math.Clamp(width, 0.1, 1.0);
            this.repeat = Math.Max(1, repeat);
            
            launchTimer = new DispatcherTimer();
            launchTimer.Tick += LaunchTimer_Tick;
            
            launchPositions = new List<Vector2>();

            System.Diagnostics.Debug.WriteLine($"?? ZigZag mechanic created: Level {ActivationLevel}, {ParticleCount} particles, " +
                                             $"{launchInterval}s intervals, {particleSpeed} speed, start: {this.startPosition:P0}, " +
                                             $"length: {this.width:P0}, repeat: {repeat}, reverse: {reverse}");
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Calculate launch positions and zigzag paths
            CalculateLaunchPositions();
            
            // Reset state
            particlesLaunched = 0;
            
            // Configure and start the timer
            launchTimer.Interval = TimeSpan.FromSeconds(launchInterval);
            launchTimer.Start();
            
            System.Diagnostics.Debug.WriteLine($"?? ZigZag activated: {ParticleCount} particles, {launchInterval}s intervals, {particleSpeed} speed");
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
            
            System.Diagnostics.Debug.WriteLine("?? ZigZag stopped and particles cleared");
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

                // Check if particle has left the screen
                if (particle.Position.Y > canvasHeight + 20 || 
                    particle.Position.X < -20 || 
                    particle.Position.X > canvasWidth + 20)
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
        /// Calculate launch positions and zigzag paths for each particle
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


        protected List<Vector2> GetEvenlySpacedTopPositions(int particleCount, double margin = 40)
        {
            var positions = new List<Vector2>();

            if (particleCount <= 0) return positions;


            // Multiple positions evenly spaced
            double spacing = (width*canvasWidth) / (particleCount - 1);

            for (int r = 0; r < repeat; r++)
            {
                for (int i = 0; i < particleCount; i++)
                {
                    float x = (float)((i * spacing) + startPosition * canvasWidth);
                    positions.Add(new Vector2(x, -20));
                }

                for (int i = 0; i < particleCount; i++)
                {
                    if (i ==0) continue; // Skip the first to avoid duplicate position
                    float x = (float)((startPosition + width) * canvasWidth - (i * spacing));
                    positions.Add(new Vector2(x, -20));
                } 
            }


            return positions;
        }

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

                System.Diagnostics.Debug.WriteLine($"?? ZigZag particle launched at ({position.X:F0}, {position.Y:F0}) with speed {particle.Speed} - {particlesLaunched + 1}/{ParticleCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error launching zigzag particle: {ex.Message}");
            }
        }

        /// <summary>
        /// Timer tick handler for launching particles
        /// </summary>
        private void LaunchTimer_Tick(object? sender, EventArgs e)
        {
            int particleTotal = ParticleCount * 2 * repeat-repeat-1;
            if (particlesLaunched >= particleTotal)
            {
                // All particles have been launched, stop the timer
                launchTimer.Stop();
                System.Diagnostics.Debug.WriteLine($"?? ZigZag completed: All {ParticleCount} particles launched");
                return;
            }

            // Launch the next particle
            LaunchParticle(launchPositions[particlesLaunched]);
            particlesLaunched++;
        }
        
       
        

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
                
                
                System.Diagnostics.Debug.WriteLine("?? ZigZag mechanic disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing ZigZag mechanic: {ex.Message}");
            }
        }
    }
}