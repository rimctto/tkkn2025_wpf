using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Base class for all particle-based level mechanics
    /// Provides common functionality and static canvas variables to avoid repetitive initialization
    /// </summary>
    public abstract class ParticleMechanicsBase : IParticleMechanics, IDisposable
    {
        #region Static Canvas Variables (initialized once per game)
  
        protected static Canvas? gameCanvas;
        protected static double canvasWidth;
        protected static double canvasHeight;
        protected static Point centerScreen;
        
        #endregion

        #region Instance Variables
        
        /// <summary>
        /// Random number generator for this mechanic instance
        /// </summary>
        protected readonly Random random;
        
        /// <summary>
        /// List of particles created by this mechanic
        /// </summary>
        protected readonly List<Particle> mechanicParticles = new List<Particle>();
        
        /// <summary>
        /// Whether this mechanic is currently active
        /// </summary>
        protected bool isActive;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Constructor that initializes the random number generator
        /// </summary>
        protected ParticleMechanicsBase()
        {
            random = new Random();
        }
        
        #endregion

        #region IParticleMechanics Properties
        
        /// <summary>
        /// The level at which this mechanic should be activated
        /// </summary>
        public abstract int ActivationLevel { get; }
        
        /// <summary>
        /// The number of particles this mechanic will create
        /// </summary>
        public abstract int ParticleCount { get; }
        
        /// <summary>
        /// Whether this mechanic is currently active
        /// </summary>
        public bool IsActive => isActive;
        
        #endregion


        #region IParticleMechanics Implementation
        
        public static void Reset(Canvas gameCanvas)
        {
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Attempting to reset ParticleMechanicsBase with null canvas");
                throw new ArgumentNullException(nameof(gameCanvas), "Game canvas cannot be null");
            }
            
            ParticleMechanicsBase.gameCanvas = gameCanvas;
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);
            
            System.Diagnostics.Debug.WriteLine($"ParticleMechanicsBase reset with canvas: {canvasWidth}x{canvasHeight}");
        }

        /// <summary>
        /// Activate the particle mechanic
        /// </summary>
        public virtual void Activate()
        {
           
            if (isActive)
            {
                Stop(); // Stop any current activity
            }
            
            isActive = true;
            OnActivate();
            
            System.Diagnostics.Debug.WriteLine($"?? {GetType().Name} activated");
        }
        
        /// <summary>
        /// Stop the mechanic and remove all particles from the canvas
        /// </summary>
        public virtual void Stop()
        {
            isActive = false;
            ClearAllParticles();
            OnStop();
            
            System.Diagnostics.Debug.WriteLine($"?? {GetType().Name} stopped");
        }
        
        /// <summary>
        /// Update the mechanic (called every frame)
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public virtual void Update(double deltaTime)
        {
            if (!isActive || mechanicParticles.Count == 0) return;
            
            OnUpdate(deltaTime);
        }
        
        /// <summary>
        /// Check for collisions with the ship
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public virtual bool CheckCollisions(Point shipPosition)
        {
            const double collisionDistance = 12.0;
            
            foreach (var particle in mechanicParticles)
            {
                double deltaX = shipPosition.X - particle.Position.X;
                double deltaY = shipPosition.Y - particle.Position.Y;
                double distanceSquared = deltaX * deltaX + deltaY * deltaY;
                
                if (distanceSquared < collisionDistance * collisionDistance)
                {
                    return true; // Collision detected
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Get the number of active particles for this mechanic
        /// </summary>
        /// <returns>Number of active particles</returns>
        public virtual int GetActiveParticleCount()
        {
            return mechanicParticles.Count;
        }
        
        #endregion

        #region Protected Virtual Methods (for derived classes to override)
        
        /// <summary>
        /// Called when the mechanic is activated
        /// Override this in derived classes for specific activation logic
        /// </summary>
        protected virtual void OnActivate()
        {
            // Base implementation does nothing
        }
        
        /// <summary>
        /// Called when the mechanic is stopped
        /// Override this in derived classes for specific stop logic
        /// </summary>
        protected virtual void OnStop()
        {
            // Base implementation does nothing
        }
        
        /// <summary>
        /// Called every frame when the mechanic is active
        /// Override this in derived classes for specific update logic
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        protected virtual void OnUpdate(double deltaTime)
        {
            // Default implementation: update all particles and remove out-of-bounds ones
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
                if (IsParticleOutOfBounds(particle))
                {
                    particlesToRemove.Add(particle);
                }
            }
            
            // Remove out-of-bounds particles
            foreach (var particle in particlesToRemove)
            {
                RemoveParticle(particle);
            }
        }
        
        #endregion

        #region Protected Helper Methods
        
        /// <summary>
        /// Create a particle with the specified properties
        /// </summary>
        /// <param name="position">Starting position</param>
        /// <param name="velocity">Initial velocity</param>
        /// <param name="color">Particle color</param>
        /// <param name="size">Particle size (diameter)</param>
        /// <returns>Created particle</returns>
        protected virtual Particle CreateParticle(Vector2 position, Vector2 velocity, Brush? color = null, double size = 8.0)
        {
            // Check if canvas is available
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: gameCanvas is null in {GetType().Name}.CreateParticle() - Reset() may not have been called");
                throw new InvalidOperationException($"Game canvas not initialized. Call ParticleMechanicsBase.Reset() before creating particles.");
            }
          
            var particle = new Particle(position)
            {
                Velocity = velocity,
                Speed = velocity.Length(),
                ShouldChaseShip = false,
                IsSpawnVectorTowardsShip = false,
                IsFreshlySpawned = true,
                IsActive = true
            };
            
            // Create visual element
            var visual = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = color ?? Brushes.White
            };
            
            particle.Visual = visual;
            particle.Color = color ?? Brushes.White;
            
            // Position the visual element
            Canvas.SetLeft(visual, position.X);
            Canvas.SetTop(visual, position.Y);
            
            // Add to canvas and tracking list
            gameCanvas.Children.Add(visual);
            mechanicParticles.Add(particle);
            
            return particle;
        }

        /// <summary>
        /// Create a particle with current level speed - convenience method for mechanics
        /// </summary>
        /// <param name="position">Starting position</param>
        /// <param name="direction">Direction vector (will be normalized)</param>
        /// <param name="color">Particle color</param>
        /// <param name="size">Particle size (diameter)</param>
        /// <returns>Created particle</returns>
        protected virtual Particle CreateParticleWithLevelSpeed(Vector2 position, Vector2 direction, Brush? color = null, double size = 8.0)
        {
            // Check if canvas is available
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: gameCanvas is null in {GetType().Name}.CreateParticleWithLevelSpeed() - Reset() may not have been called");
                throw new InvalidOperationException($"Game canvas not initialized. Call ParticleMechanicsBase.Reset() before creating particles.");
            }
            
            // Use the current level speed from LevelManager
            double currentSpeed = LevelManager.CurrentLevelSpeed;
            
            // Normalize direction and apply current speed
            if (direction.Length() > 0.01f)
            {
                direction = Vector2.Normalize(direction);
            }
            var velocity = direction * (float)currentSpeed;
            
            var particle = new Particle(position)
            {
                Velocity = velocity,
                Speed = currentSpeed,
                ShouldChaseShip = false,
                IsSpawnVectorTowardsShip = false,
                IsFreshlySpawned = true,
                IsActive = true
            };
            
            // Create visual element
            var visual = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = color ?? Brushes.White
            };
            
            particle.Visual = visual;
            particle.Color = color ?? Brushes.White;
            
            // Position the visual element
            Canvas.SetLeft(visual, position.X);
            Canvas.SetTop(visual, position.Y);
            
            // Add to canvas and tracking list
            gameCanvas.Children.Add(visual);
            mechanicParticles.Add(particle);
            
            return particle;
        }
        
        /// <summary>
        /// Remove a particle from the game
        /// </summary>
        /// <param name="particle">Particle to remove</param>
        protected virtual void RemoveParticle(Particle particle)
        {
            try
            {
                if (particle == null) return;
                
                mechanicParticles.Remove(particle);
                
                if (particle.Visual != null && gameCanvas != null)
                {
                    gameCanvas.Children.Remove(particle.Visual);
                }
                else if (particle.Visual != null && gameCanvas == null)
                {
                    System.Diagnostics.Debug.WriteLine($"WARNING: Cannot remove particle visual - gameCanvas is null in {GetType().Name}");
                }
                
                particle.IsActive = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing particle in {GetType().Name}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clear all particles created by this mechanic
        /// </summary>
        protected virtual void ClearAllParticles()
        {
            foreach (var particle in mechanicParticles)
            {
                if (particle.Visual != null && gameCanvas != null)
                {
                    gameCanvas.Children.Remove(particle.Visual);
                }
                particle.IsActive = false;
            }
            mechanicParticles.Clear();
        }
        
        /// <summary>
        /// Check if a particle is out of bounds and should be removed
        /// </summary>
        /// <param name="particle">Particle to check</param>
        /// <returns>True if particle is out of bounds</returns>
        protected virtual bool IsParticleOutOfBounds(Particle particle)
        {
            const double margin = 50; // Allow particles to go slightly off-screen before removal
            return particle.Position.X < -margin || 
                   particle.Position.X > canvasWidth + margin ||
                   particle.Position.Y < -margin || 
                   particle.Position.Y > canvasHeight + margin;
        }
        
        /// <summary>
        /// Get a random position along the top edge of the screen
        /// </summary>
        /// <param name="margin">Margin from edges</param>
        /// <returns>Random position along top edge</returns>
        protected Vector2 GetRandomTopPosition(double margin = 40)
        {
            double x = margin + random.NextDouble() * (canvasWidth - 2 * margin);
            return new Vector2((float)x, -20);
        }
        
        /// <summary>
        /// Get evenly spaced positions along the top edge of the screen
        /// </summary>
        /// <param name="count">Number of positions</param>
        /// <param name="margin">Margin from edges</param>
        /// <returns>List of evenly spaced positions</returns>
        protected List<Vector2> GetEvenlySpacedTopPositions(int count, double margin = 40)
        {
            var positions = new List<Vector2>();
            
            if (count <= 0) return positions;
            
            double availableWidth = canvasWidth - (2 * margin);
            
            if (count == 1)
            {
                // Single position at center
                positions.Add(new Vector2((float)(canvasWidth / 2), -20));
            }
            else
            {
                // Multiple positions evenly spaced
                double spacing = availableWidth / (count - 1);
                
                for (int i = 0; i < count; i++)
                {
                    float x = (float)(margin + (i * spacing));
                    positions.Add(new Vector2(x, -20));
                }
            }
            
            return positions;
        }
        
        #endregion

        #region Disposal
        
        /// <summary>
        /// Dispose of resources
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                Stop();
                System.Diagnostics.Debug.WriteLine($"?? {GetType().Name} disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing {GetType().Name}: {ex.Message}");
            }
        }
        
        #endregion

        /// <summary>
        /// Update the speed of all active particles from this mechanic
        /// This is called when the game level increases to ensure all particles move at the current level speed
        /// </summary>
        /// <param name="newSpeed">New speed for all particles</param>
        public virtual void UpdateParticleSpeed(double newSpeed)
        {
            foreach (var particle in mechanicParticles)
            {
                if (particle.IsActive)
                {
                    // Update the particle's speed property
                    particle.Speed = newSpeed;
                    
                    // Update the velocity magnitude while preserving direction
                    if (particle.Velocity.Length() > 0)
                    {
                        var direction = Vector2.Normalize(particle.Velocity);
                        particle.Velocity = direction * (float)newSpeed;
                    }
                    else
                    {
                        // Default to downward movement if no velocity
                        particle.Velocity = new Vector2(0, (float)newSpeed);
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"?? {GetType().Name}: Updated {mechanicParticles.Count} particles to speed {newSpeed}");
        }
    }
}