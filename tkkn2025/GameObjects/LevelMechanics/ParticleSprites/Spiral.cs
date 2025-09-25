using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;

namespace tkkn2025.GameObjects.LevelMechanics.ParicleSprites
{
    /// <summary>
    /// Spiral particle mechanic that creates rotating spiral patterns
    /// Inherits from ParticleMechanicsBase and GameObject to support positioning and particle mechanics
    /// </summary>
    public class SpiralMechanic : ParticleMechanicsBase
    {
        #region Private Fields

        private readonly SpiralConfig config;
        private readonly List<Particle> spiralParticles = new List<Particle>();
        private readonly List<Particle> sineWaveParticles = new List<Particle>();
        private double currentFrame;
        private double lastUpdateTime;

        // Activation properties
        private readonly int activationLevel;
        private readonly int totalParticleCount;

        // Animation variables
        private double ix, iy, jx, jy;

        // GameObject properties for positioning
        public Vector2 Position { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new Spiral mechanic at the specified position
        /// </summary>
        /// <param name="position">Center position of the spiral</param>
        /// <param name="activationLevel">Level at which this mechanic activates</param>
        /// <param name="particleCount">Number of particles in the spiral</param>
        /// <param name="config">Optional spiral configuration, uses default if null</param>
        public SpiralMechanic(Vector2 position, int activationLevel = 1, int particleCount = 25, SpiralConfig? config = null) 
            : base()
        {
            Position = position;
            this.config = config ?? SpiralConfig.LoadConfig();
            this.activationLevel = activationLevel;
            this.totalParticleCount = particleCount * 2; // Spiral + Sine wave particles
            this.currentFrame = 0;
            this.lastUpdateTime = 0;
            
            System.Diagnostics.Debug.WriteLine($"SpiralMechanic created at {position} with {totalParticleCount} total particles");
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
            System.Diagnostics.Debug.WriteLine($"SpiralMechanic activated at level {activationLevel}");
        }

        protected override void OnStop()
        {
            ClearCustomParticles();
            System.Diagnostics.Debug.WriteLine("SpiralMechanic stopped");
        }

        protected override void OnUpdate(double deltaTime)
        {
            lastUpdateTime += deltaTime;
            currentFrame = lastUpdateTime * 60.0; // Convert to frame-based animation (assuming 60 FPS equivalent)

            UpdateRotationMatrix();
            UpdateSpiralParticles();
            UpdateSineWaveParticles();
            RemoveOutOfBoundsParticles();
        }

        public override bool CheckCollisions(Point shipPosition)
        {
            if (!isActive) return false;

            const double collisionDistance = 12.0;
            const double collisionDistanceSquared = collisionDistance * collisionDistance;

            // Check collisions with spiral particles
            foreach (var particle in spiralParticles)
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
            return spiralParticles.Count + sineWaveParticles.Count;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create initial particles for both spiral and sine wave patterns
        /// </summary>
        private void CreateInitialParticles()
        {
            // Check if canvas is available
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: gameCanvas is null in SpiralMechanic - ParticleMechanicsBase.Reset() may not have been called");
                return;
            }

            // Create spiral particles
            for (int i = 0; i < config.ParticleCount; i++)
            {
                var spiralParticle = CreateSpiralParticle(i, Brushes.LightBlue);
                spiralParticles.Add(spiralParticle);
            }

            // Create sine wave particles
            for (int i = 0; i < config.ParticleCount; i++)
            {
                var sineParticle = CreateSineWaveParticle(i, Brushes.MediumPurple);
                sineWaveParticles.Add(sineParticle);
            }
        }

        /// <summary>
        /// Create a single spiral particle
        /// </summary>
        private Particle CreateSpiralParticle(int index, Brush color)
        {
            var particle = new Particle(Position)
            {
                Speed = LevelManager.CurrentLevelSpeed,
                IsActive = true,
                Color = color,
                ShouldChaseShip = false,
                IsSpawnVectorTowardsShip = false,
                IsFreshlySpawned = true
            };

            // Create visual element
            var visual = new Ellipse
            {
                Width = config.Width,
                Height = config.Width,
                Stroke = color,
                Fill = color,
                StrokeThickness = 1
            };

            particle.Visual = visual;
            gameCanvas.Children.Add(visual);

            return particle;
        }

        /// <summary>
        /// Create a single sine wave particle
        /// </summary>
        private Particle CreateSineWaveParticle(int index, Brush color)
        {
            var particle = new Particle(Position)
            {
                Speed = LevelManager.CurrentLevelSpeed,
                IsActive = true,
                Color = color,
                ShouldChaseShip = false,
                IsSpawnVectorTowardsShip = false,
                IsFreshlySpawned = true
            };

            // Create visual element
            var visual = new Ellipse
            {
                Width = config.Width,
                Height = config.Height,
                Stroke = color,
                Fill = Brushes.MediumPurple,
                StrokeThickness = 1
            };

            particle.Visual = visual;
            gameCanvas.Children.Add(visual);

            return particle;
        }

        /// <summary>
        /// Update rotation matrix for animation
        /// </summary>
        private void UpdateRotationMatrix()
        {
            double angle = currentFrame * config.Speed;
            ix = Math.Cos(angle);
            iy = Math.Sin(angle);
            jx = -Math.Sin(angle);
            jy = Math.Cos(angle);
        }

        /// <summary>
        /// Update positions of spiral particles
        /// </summary>
        private void UpdateSpiralParticles()
        {
            if (gameCanvas == null) return;

            var centerX = Position.X;
            var centerY = Position.Y;

            for (int i = 0; i < spiralParticles.Count; i++)
            {
                var particle = spiralParticles[i];
                if (!particle.IsActive || particle.Visual == null) continue;

                var calculatedRadius = i * config.Radius * 0.01; // Scale down the radius
                
                var x = Math.Sin(Math.PI * config.ParticleSpacing * 10 * i) * calculatedRadius;
                var y = Math.Cos(Math.PI * config.ParticleSpacing * 10 * i) * calculatedRadius;

                // Apply rotation matrix
                var rotatedX = x * ix + y * iy;
                var rotatedY = x * jx + y * jy;

                // Update particle position relative to spiral center
                var newX = centerX + rotatedX;
                var newY = centerY + rotatedY;

                particle.Position = new Vector2((float)newX, (float)newY);

                // Update visual position
                Canvas.SetLeft(particle.Visual, newX - config.Width / 2);
                Canvas.SetTop(particle.Visual, newY - config.Height / 2);
            }
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

                var x = i * config.ParticleSpacing * 1000; // Scale up spacing for visibility
                var y = config.Amplitude * -Math.Sin(i * config.Frequency * 100 + currentFrame * 0.05);

                // Apply rotation matrix
                var rotatedX = x * ix + y * iy;
                var rotatedY = x * jx + y * jy;

                // Update particle position relative to spiral center
                var newX = centerX + rotatedX;
                var newY = centerY + rotatedY;

                particle.Position = new Vector2((float)newX, (float)newY);

                // Update visual position
                Canvas.SetLeft(particle.Visual, newX - config.Width / 2);
                Canvas.SetTop(particle.Visual, newY - config.Height / 2);
            }
        }

        /// <summary>
        /// Remove particles that have moved out of bounds
        /// </summary>
        private void RemoveOutOfBoundsParticles()
        {
            if (gameCanvas == null) return;

            const double margin = 100;

            // Check spiral particles
            var spiralToRemove = new List<Particle>();
            foreach (var particle in spiralParticles)
            {
                if (IsParticleOutOfBounds(particle, margin))
                {
                    spiralToRemove.Add(particle);
                }
            }

            foreach (var particle in spiralToRemove)
            {
                RemoveCustomParticle(particle, spiralParticles);
            }

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
                
                if (particle != null)
                {
                    particle.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing spiral particle: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all particles from both patterns
        /// </summary>
        private void ClearCustomParticles()
        {
            if (gameCanvas != null)
            {
                // Clear spiral particles
                foreach (var particle in spiralParticles)
                {
                    if (particle.Visual != null)
                    {
                        gameCanvas.Children.Remove(particle.Visual);
                    }
                    particle.IsActive = false;
                }

                // Clear sine wave particles
                foreach (var particle in sineWaveParticles)
                {
                    if (particle.Visual != null)
                    {
                        gameCanvas.Children.Remove(particle.Visual);
                    }
                    particle.IsActive = false;
                }
            }

            spiralParticles.Clear();
            sineWaveParticles.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update the spiral's center position
        /// </summary>
        /// <param name="newPosition">New center position</param>
        public void UpdatePosition(Vector2 newPosition)
        {
            Position = newPosition;
        }

        /// <summary>
        /// Get the current configuration
        /// </summary>
        public SpiralConfig GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Update particle speeds when level changes
        /// </summary>
        /// <param name="newSpeed">New speed for particles</param>
        public override void UpdateParticleSpeed(double newSpeed)
        {
            // Update spiral particles
            foreach (var particle in spiralParticles)
            {
                if (particle.IsActive)
                {
                    particle.Speed = newSpeed;
                }
            }

            // Update sine wave particles
            foreach (var particle in sineWaveParticles)
            {
                if (particle.IsActive)
                {
                    particle.Speed = newSpeed;
                }
            }

            System.Diagnostics.Debug.WriteLine($"SpiralMechanic: Updated {spiralParticles.Count + sineWaveParticles.Count} particles to speed {newSpeed}");
        }

        #endregion
    }
}
