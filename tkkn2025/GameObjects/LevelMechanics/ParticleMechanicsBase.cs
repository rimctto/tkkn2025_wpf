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
        
        /// <summary>
        /// Static canvas reference - shared across all mechanics
        /// </summary>
        protected static Canvas? gameCanvas;
        
        /// <summary>
        /// Static canvas dimensions - updated once when game starts
        /// </summary>
        protected static double canvasWidth;
        protected static double canvasHeight;
        protected static Point centerScreen;
        
        /// <summary>
        /// Flag to track if static variables have been initialized
        /// </summary>
        private static bool staticVariablesInitialized = false;
        
        #endregion

        #region Instance Variables
        
        /// <summary>
        /// Random number generator for this mechanic instance
        /// </summary>
        protected readonly Random random;
        
        /// <summary>
        /// List of particles created by this mechanic
        /// </summary>
        protected readonly List<Patricle> mechanicParticles = new List<Patricle>();
        
        /// <summary>
        /// Whether this mechanic is currently active
        /// </summary>
        protected bool isActive;
        
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

        #region Constructor
        
        /// <summary>
        /// Initialize the base particle mechanic
        /// </summary>
        /// <param name="canvas">Game canvas reference</param>
        /// <param name="randomGenerator">Random number generator</param>
        protected ParticleMechanicsBase(Canvas canvas, Random randomGenerator)
        {
            random = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
            
            // Initialize static variables if not already done
            InitializeStaticVariables(canvas);
            
            System.Diagnostics.Debug.WriteLine($"?? ParticleMechanicsBase initialized for {GetType().Name}");
        }
        
        #endregion

        #region Static Initialization
        
        /// <summary>
        /// Initialize static canvas variables once per game session
        /// This avoids repeated initialization across multiple mechanics
        /// </summary>
        /// <param name="canvas">Game canvas reference</param>
        public static void InitializeStaticVariables(Canvas canvas)
        {
            if (staticVariablesInitialized && gameCanvas == canvas)
            {
                return; // Already initialized with the same canvas
            }
            
            gameCanvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            UpdateCanvasDimensions();
            staticVariablesInitialized = true;
            
            System.Diagnostics.Debug.WriteLine($"?? Static canvas variables initialized: {canvasWidth}x{canvasHeight}");
        }
        
        /// <summary>
        /// Update canvas dimensions - called when window is resized or game starts
        /// </summary>
        public static void UpdateCanvasDimensions()
        {
            if (gameCanvas == null) return;
            
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);
            
            System.Diagnostics.Debug.WriteLine($"?? Canvas dimensions updated: {canvasWidth}x{canvasHeight}");
        }
        
        /// <summary>
        /// Reset static variables for a new game session
        /// </summary>
        public static void ResetStaticVariables()
        {
            staticVariablesInitialized = false;
            gameCanvas = null;
            canvasWidth = 0;
            canvasHeight = 0;
            centerScreen = new Point(0, 0);
            
            System.Diagnostics.Debug.WriteLine("?? Static variables reset for new game session");
        }
        
        #endregion

        #region IParticleMechanics Implementation
        
        /// <summary>
        /// Activate the particle mechanic
        /// </summary>
        public virtual void Activate()
        {
            if (gameCanvas == null)
            {
                System.Diagnostics.Debug.WriteLine($"?? Cannot activate {GetType().Name}: gameCanvas is null");
                return;
            }
            
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
            const double collisionDistance = 15.0;
            
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
            var particlesToRemove = new List<Patricle>();
            
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
        protected virtual Patricle CreateParticle(Vector2 position, Vector2 velocity, Brush? color = null, double size = 8.0)
        {
            if (gameCanvas == null)
            {
                throw new InvalidOperationException("Cannot create particle: gameCanvas is null");
            }
            
            var particle = new Patricle(position)
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
        /// Remove a particle from the game
        /// </summary>
        /// <param name="particle">Particle to remove</param>
        protected virtual void RemoveParticle(Patricle particle)
        {
            try
            {
                mechanicParticles.Remove(particle);
                
                if (particle.Visual != null && gameCanvas != null)
                {
                    gameCanvas.Children.Remove(particle.Visual);
                }
                
                particle.IsActive = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing particle: {ex.Message}");
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
        protected virtual bool IsParticleOutOfBounds(Patricle particle)
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
    }
}