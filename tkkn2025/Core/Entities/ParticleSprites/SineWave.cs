using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.Core.GameModes.MazeMode;
using tkkn2025.GameObjects.LevelMechanics;

namespace tkkn2025.GameObjects.LevelMechanics.ParticleSprites
{
    /// <summary>
    /// Sine wave particle mechanic that creates rotating sine wave patterns
    /// Inherits from ParticleMechanicsBase to support positioning and particle mechanics
    /// </summary>
    public class SineWave : ParticleMechanicsBase
    {
        #region Private Fields

        private readonly SineWaveConfig config;
        private readonly List<Particle> sineWaveParticles = new List<Particle>();
        private double currentFrame;
        private double lastUpdateTime;

        // Activation properties
        private readonly int activationLevel;
        private readonly int totalParticleCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new SineWave mechanic at the specified position
        /// </summary>
        /// <param name="position">Center position of the sine wave</param>
        /// <param name="activationLevel">Level at which this mechanic activates</param>
        /// <param name="particleCount">Number of particles in the sine wave</param>
        /// <param name="config">Optional sine wave configuration, uses default if null</param>
        public SineWave(Vector2 position, int activationLevel = 1, int particleCount = 25, SineWaveConfig? config = null)
            : base()
        {
            Position = position;
            this.config = config ?? SineWaveConfig.LoadConfig();
            this.activationLevel = activationLevel;
            this.totalParticleCount = particleCount;
            this.currentFrame = 0;
            this.lastUpdateTime = 0;

            System.Diagnostics.Debug.WriteLine($"SineWaveMechanic created at {position} with {totalParticleCount} particles");
        }

        #endregion


        #region Draw Methods

        /// <summary>
        /// Create initial particles for the sine wave pattern
        /// </summary>
        private void CreateInitialParticles()
        {
            // Check if canvas is available
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: gameCanvas is null in SineWaveMechanic - ParticleMechanicsBase.Reset() may not have been called");
                return;
            }

            // Create sine wave particles
            for (int i = 0; i < config.ParticleCount; i++)
            {
                var sineParticle = CreateSineWaveParticle(i, Brushes.MediumPurple);
                sineWaveParticles.Add(sineParticle);
            }
        }

        /// <summary>
        /// Create a single sine wave particle
        /// </summary>
        private Particle CreateSineWaveParticle(int index, Brush color)
        {
            var particle = new Particle(Position)
            {
                Speed = Maze.CurrentLevelSpeed,
                IsActive = true,
                Color = color,
                ShouldChaseShip = false,
                IsSpawnVectorTowardsShip = false,
                IsFreshlySpawned = true
            };

            //Last Particle
            if (index == config.ParticleCount - 1)
            {
                particle.Size = 15;
                particle.Color = Brushes.Blue;
            }

            // Create visual element
            var visual = new Ellipse
            {
                Width = particle.Size,
                Height = particle.Size,
                Stroke = particle.Color,
                Fill = particle.Color,
                StrokeThickness = 1
            };

            particle.Visual = visual;
            gameCanvas.Children.Add(visual);

            // Add to base class mechanic particles list for proper tracking
            mechanicParticles.Add(particle);

            return particle;
        }

        /// <summary>
        /// Update positions of sine wave particles
        /// </summary>
        private void UpdateSineWaveParticles()
        {
            if (gameCanvas == null) return;

            var centerX = Position.X;
            var centerY = Position.Y;
            
            for (int i = 0; i < sineWaveParticles.Count; i++)
            {
                var particle = sineWaveParticles[i];
                if (!particle.IsActive || particle.Visual == null) continue;

                var damping = 1 / (i * 0.075 + 1);

                var amplitude = Math.Min(config.Amplitude - (config.Amplitude * damping), config.Amplitude);

                var x = i * config.ParticleSpacing;
                var y = amplitude * -Math.Sin(i * config.Frequency  + currentFrame * 0.05);

                // Apply rotation using base Entity class method
                var rotatedPos = ApplyRotation(x, y);

                // Update particle position relative to sine wave center
                var newX = centerX + rotatedPos.X;
                var newY = centerY + rotatedPos.Y;

                particle.Position = new Vector2((float)newX, (float)newY);

                // Update visual position
                Canvas.SetLeft(particle.Visual, newX - particle.Size / 2);
                Canvas.SetTop(particle.Visual, newY - particle.Size / 2);
            }
        }

        /// <summary>
        /// Remove particles that have moved out of bounds
        /// </summary>
        private void RemoveOutOfBoundsParticles()
        {
            if (gameCanvas == null) return;

            const double margin = 100;

            // Check sine wave particles
            var sineToRemove = new List<Particle>();
            foreach (var particle in sineWaveParticles)
            {
                if (IsParticleOutOfBounds(particle, margin))
                {
                    sineToRemove.Add(particle);
                }
            }

            foreach (var particle in sineToRemove)
            {
                RemoveCustomParticle(particle, sineWaveParticles);
            }
        }

        /// <summary>
        /// Check if particle is out of bounds
        /// </summary>
        private bool IsParticleOutOfBounds(Particle particle, double margin)
        {
            if (gameCanvas == null) return true;

            return particle.Position.X < -margin ||
                   particle.Position.X > canvasWidth + margin ||
                   particle.Position.Y < -margin ||
                   particle.Position.Y > canvasHeight + margin;
        }

        /// <summary>
        /// Remove a particle from the specified list and canvas
        /// </summary>
        private void RemoveCustomParticle(Particle particle, List<Particle> particleList)
        {
            try
            {
                if (particle?.Visual != null && gameCanvas != null)
                {
                    gameCanvas.Children.Remove(particle.Visual);
                }

                particleList.Remove(particle);

                // Also remove from base class mechanicParticles list for proper tracking
                mechanicParticles.Remove(particle);

                if (particle != null)
                {
                    particle.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing sine wave particle: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all particles from the sine wave
        /// </summary>
        private void ClearCustomParticles()
        {
            // Clear local lists
            sineWaveParticles.Clear();

            // Use base class method to properly clear all particles from canvas and mechanicParticles list
            ClearAllParticles();
        }

        #endregion


        #region ParticleMechanicsBase Implementation

        public override int ActivationLevel => activationLevel;
        public override int ParticleCount => totalParticleCount;



        protected override void OnActivate()
        {
            currentFrame = 0;
            lastUpdateTime = 0;
            CreateInitialParticles();
            System.Diagnostics.Debug.WriteLine($"SineWaveMechanic activated at level {activationLevel}");
        }

        protected override void OnStop()
        {
            ClearCustomParticles();
            System.Diagnostics.Debug.WriteLine("SineWaveMechanic stopped");
        }

        protected override void OnUpdate(double deltaTime)
        {
            lastUpdateTime += deltaTime;
            currentFrame = lastUpdateTime * 120; // Convert to frame-based animation (assuming 60 FPS equivalent)

            // Update rotation matrix with current frame and rotation speed
            double angle = currentFrame * config.RotationSpeed;

            foreach (var behavior in Behaviors)
            {
                behavior.ApplyUpdate(deltaTime);
            }

            UpdateRotationMatrix(Rotation);
            
            UpdateSineWaveParticles();
            RemoveOutOfBoundsParticles();
            
        }


        public override bool CheckCollisions(Point shipPosition)
        {
            if (!isActive) return false;

            const double collisionDistance = 12.0;
            const double collisionDistanceSquared = collisionDistance * collisionDistance;

            // Check collisions with sine wave particles
            foreach (var particle in sineWaveParticles)
            {
                if (!particle.IsActive) continue;

                double deltaX = shipPosition.X - particle.Position.X;
                double deltaY = shipPosition.Y - particle.Position.Y;
                double distanceSquared = deltaX * deltaX + deltaY * deltaY;

                if (distanceSquared < collisionDistanceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetActiveParticleCount()
        {
            return sineWaveParticles.Count;
        }

        #endregion

       
        #region Public Methods

        /// <summary>
        /// Update the sine wave's center position
        /// </summary>
        /// <param name="newPosition">New center position</param>
        public void UpdatePosition(Vector2 newPosition)
        {
            Position = newPosition;
        }

        /// <summary>
        /// Get the current configuration
        /// </summary>
        public SineWaveConfig GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Update particle speeds when level changes
        /// </summary>
        /// <param name="newSpeed">New speed for particles</param>
        public override void UpdateParticleSpeed(double newSpeed)
        {
            // Update sine wave particles
            foreach (var particle in sineWaveParticles)
            {
                if (particle.IsActive)
                {
                    particle.Speed = newSpeed;
                }
            }

            System.Diagnostics.Debug.WriteLine($"SineWaveMechanic: Updated {sineWaveParticles.Count} particles to speed {newSpeed}");
        }

        #endregion
    }
}