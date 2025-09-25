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
    /// Level mechanic that creates horizontal lines of particles across the screen with an opening (gap)
    /// Multiple lines can be created with delays between them
    /// </summary>
    public class LineMechanic : ParticleMechanicsBase
    {
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        
        // Line properties
        private readonly double particleSpeed; // Speed at which the lines move down
        private readonly double openingStart; // Starting position of the opening as percentage of canvas width (0.0 to 1.0)
        private readonly double openingWidth; // Width of the opening as percentage of canvas width (0.0 to 1.0)
        private readonly int repeatCount; // Number of lines to create
        private readonly double delayMilliseconds; // Delay between lines in milliseconds
        
        // Visual properties
        private readonly Brush particleColor = Brushes.Cyan; // Distinct color for line particles
        private readonly double particleSize = 8.0;
        private readonly double particleSpacing = 12.0; // Spacing between particles in the line
        
        // Repeat state
        private int linesCreated;
        private DispatcherTimer repeatTimer;
        
        /// <summary>
        /// Creates a new line mechanic
        /// </summary>
        /// <param name="activationLevel">Level at which this mechanic activates</param>
        /// <param name="particleSpeed">Speed at which the lines move down (pixels per second)</param>
        /// <param name="openingStart">Starting position of the opening as percentage of canvas width (0.0 to 1.0)</param>
        /// <param name="openingWidth">Width of the opening as percentage of canvas width (0.0 to 1.0)</param>
        /// <param name="repeatCount">Number of lines to create (default: 1)</param>
        /// <param name="delayMilliseconds">Delay between lines in milliseconds (default: 500)</param>
        public LineMechanic(int activationLevel = 2, double particleSpeed = 150.0, double openingStart = 0.4, double openingWidth = 0.2, int repeatCount = 1, double delayMilliseconds = 500.0)
        {
            ActivationLevel = activationLevel;
            this.particleSpeed = Math.Max(50.0, particleSpeed); // Ensure minimum speed
            this.openingStart = Math.Clamp(openingStart, 0.0, 1.0); // Clamp to valid range
            this.openingWidth = Math.Clamp(openingWidth, 0.1, 0.8); // Ensure reasonable opening size
            this.repeatCount = Math.Max(1, repeatCount); // Ensure at least 1 line
            this.delayMilliseconds = Math.Max(100.0, delayMilliseconds); // Ensure minimum delay
            
            // Calculate particle count based on canvas width and spacing (for all lines)
            // This will be updated when the mechanic is activated
            ParticleCount = CalculateParticleCount() * this.repeatCount;
            
            // Initialize timer for repeats
            repeatTimer = new DispatcherTimer();
            repeatTimer.Tick += RepeatTimer_Tick;
            
            System.Diagnostics.Debug.WriteLine($"?? Line mechanic created: Level {ActivationLevel}, Speed: {this.particleSpeed}, Opening: {this.openingStart:P1}-{(this.openingStart + this.openingWidth):P1}, Repeats: {this.repeatCount}, Delay: {this.delayMilliseconds}ms");
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Reset repeat state
            linesCreated = 0;
            
            // Create the first line immediately
            CreateLine();
            linesCreated++;
            
            // If we need more lines, start the timer for repeats
            if (linesCreated < repeatCount)
            {
                repeatTimer.Interval = TimeSpan.FromMilliseconds(delayMilliseconds);
                repeatTimer.Start();
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Line activated: {ParticleCount} total particles, Opening from {openingStart:P1} to {(openingStart + openingWidth):P1}, {repeatCount} repeats with {delayMilliseconds}ms delay");
        }

        /// <summary>
        /// Called when the mechanic is stopped
        /// </summary>
        protected override void OnStop()
        {
            if (repeatTimer.IsEnabled)
            {
                repeatTimer.Stop();
            }
            
            linesCreated = 0;
            
            System.Diagnostics.Debug.WriteLine("?? Line stopped and particles cleared");
        }

        /// <summary>
        /// Timer tick handler for creating repeated lines
        /// </summary>
        private void RepeatTimer_Tick(object? sender, EventArgs e)
        {
            if (linesCreated >= repeatCount)
            {
                // All lines have been created, stop the timer
                repeatTimer.Stop();
                System.Diagnostics.Debug.WriteLine($"?? Line mechanic completed: All {repeatCount} lines created");
                return;
            }

            // Create the next line
            CreateLine();
            linesCreated++;
            
            System.Diagnostics.Debug.WriteLine($"?? Line repeat {linesCreated}/{repeatCount} created");
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
                // Update particle position (move straight down)
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

            // If all particles are gone and no more lines to create, the mechanic is effectively complete
            if (mechanicParticles.Count == 0 && linesCreated >= repeatCount && isActive)
            {
                System.Diagnostics.Debug.WriteLine("?? Line mechanic completed: All particles have left the screen");
            }
        }

        /// <summary>
        /// Calculate the number of particles needed to fill one line (excluding the opening)
        /// </summary>
        /// <returns>Number of particles for one line</returns>
        private int CalculateParticleCount()
        {
            if (canvasWidth <= 0) return 50; // Default fallback
            
            // Calculate total line width excluding the opening
            double openingPixelStart = canvasWidth * openingStart;
            double openingPixelWidth = canvasWidth * openingWidth;
            double openingPixelEnd = openingPixelStart + openingPixelWidth;
            
            // Calculate particles needed for left side of opening
            int leftSideParticles = (int)(openingPixelStart / particleSpacing);
            
            // Calculate particles needed for right side of opening
            double rightSideWidth = canvasWidth - openingPixelEnd;
            int rightSideParticles = (int)(rightSideWidth / particleSpacing);
            
            int totalParticles = leftSideParticles + rightSideParticles;
            
            return Math.Max(1, totalParticles); // Ensure at least 1 particle
        }

        /// <summary>
        /// Create a single horizontal line of particles with an opening
        /// </summary>
        private void CreateLine()
        {
            try
            {
                // Calculate opening boundaries in pixels
                double openingPixelStart = canvasWidth * openingStart;
                double openingPixelWidth = canvasWidth * openingWidth;
                double openingPixelEnd = openingPixelStart + openingPixelWidth;
                
                // Starting Y position (above the screen)
                float startY = -20;
                
                // Create particles for the left side of the opening
                CreateParticlesForSection(0, openingPixelStart, startY);
                
                // Create particles for the right side of the opening
                CreateParticlesForSection(openingPixelEnd, canvasWidth, startY);
                
                System.Diagnostics.Debug.WriteLine($"?? Line {linesCreated + 1}/{repeatCount} created with particles, opening from {openingPixelStart:F0} to {openingPixelEnd:F0} pixels");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating line: {ex.Message}");
            }
        }

        /// <summary>
        /// Create particles for a specific section of the line
        /// </summary>
        /// <param name="startX">Starting X position</param>
        /// <param name="endX">Ending X position</param>
        /// <param name="y">Y position for all particles</param>
        private void CreateParticlesForSection(double startX, double endX, float y)
        {
            double currentX = startX;
            
            while (currentX <= endX - particleSpacing)
            {
                // Create particle position
                var position = new Vector2((float)currentX, y);
                
                // Set direction to move straight down
                var direction = new Vector2(0, 1);
                
                // Create particle using the new level speed method
                var particle = CreateParticleWithLevelSpeed(position, direction, particleColor, particleSize);
                
                // Set additional properties
                particle.ShouldChaseShip = false; // Straight line movement
                particle.IsSpawnVectorTowardsShip = false;
                particle.IsFreshlySpawned = true;
                
                // Move to next position
                currentX += particleSpacing;
            }
        }

        /// <summary>
        /// Get string representation of this mechanic for debugging
        /// </summary>
        /// <returns>Debug string</returns>
        public override string ToString()
        {
            return $"LineMechanic(Level:{ActivationLevel}, Speed:{particleSpeed:F0}, Opening:{openingStart:P1}-{(openingStart + openingWidth):P1}, Repeats:{repeatCount}, Delay:{delayMilliseconds}ms)";
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public override void Dispose()
        {
            try
            {
                base.Dispose();
                
                if (repeatTimer != null)
                {
                    repeatTimer.Tick -= RepeatTimer_Tick;
                    repeatTimer.Stop();
                    repeatTimer = null!;
                }
                
                System.Diagnostics.Debug.WriteLine("?? Line mechanic disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Line mechanic: {ex.Message}");
            }
        }
    }
}