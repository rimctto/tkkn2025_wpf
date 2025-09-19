using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects.PowerUps;
using tkkn2025.Helpers;
using tkkn2025.Settings;

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Manages all particle-related operations including creation, updates, collisions, and pooling
    /// </summary>
    public class ParticleManager
    {
        // Particle management
        private static List<Patricle> particles = new List<Patricle>();
        private readonly Queue<Patricle> particlePool = new Queue<Patricle>();
        private static Random random = new Random();
        private static Canvas gameCanvas;

        // Game state
        private static double canvasWidth;
        private static double canvasHeight;
        private static Point centerScreen;

        // Active game settings - snapshot taken when game starts
        private static double activeParticleSpeed;
        private static double activeParticleTurnSpeed;
        private static double activeParticleSpeedVariance;
        private static double activeParticleRandomizerPercentage;
        private static bool activeIsParticleSpawnVectorTowardsShip;
        private static bool activeShouldParticlesChaseShip;

        // Current game values
        private static int currentParticleCount = 0;

        public int ParticleCount => particles.Count;

        public int CurrentParticleCount => currentParticleCount;

        public ParticleManager(Canvas canvas)
        {
            gameCanvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            UpdateCanvasDimensions();
        }

        public static void UpdateCanvasDimensions()
        {
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);
        }


        public static void InitializeGameSettings()
        {
            // Snapshot current settings as active settings for this game
            activeParticleSpeed = GameSettings.ParticleSpeed.Value;
            activeParticleTurnSpeed = GameSettings.ParticleTurnSpeed.Value;
            activeParticleSpeedVariance = GameSettings.ParticleSpeedVariance.Value;
            activeParticleRandomizerPercentage = GameSettings.ParticleRandomizerPercentage.Value;
            activeIsParticleSpawnVectorTowardsShip = GameSettings.IsParticleSpawnVectorTowardsShip.Value;
            activeShouldParticlesChaseShip = GameSettings.IsParticleChaseShip.Value;
            currentParticleCount = GameSettings.StartingParticles.Value;
        }


        public void StartNewGame()
        {
            ClearAllParticles();
            CreateParticles(currentParticleCount);
        }


        public void UpdateParticles(double deltaTime, Point shipPosition, PowerUpManager? powerUpManager = null)
        {
            var shipPos = new Vector2((float)shipPosition.X, (float)shipPosition.Y);

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];

                if (particle.X > 0 && particle.Y > 0 && particle.X < canvasWidth && particle.Y < canvasHeight)
                    particle.IsFreshlySpawned = false;

                // Update particle using its own Update method
                particle.Update((float)deltaTime, shipPos);

                // Apply powerup forces if any
                if (powerUpManager != null)
                {
                    var particlePos = particle.Position;
                    var particleVel = particle.Velocity;
                    powerUpManager.ApplyAllForces(ref particlePos, ref particleVel, (float)deltaTime);
                    particle.Position = particlePos;
                    particle.Velocity = particleVel;
                }

                // Check bounds
                if (particle.IsFreshlySpawned == false && 
                    (particle.X < -20 || particle.X > canvasWidth + 20 ||
                    particle.Y < -20 || particle.Y > canvasHeight + 20))
                {
                    // Remove and pool particle
                    gameCanvas.Children.Remove(particle.Visual);
                    particle.IsActive = false;
                    particles.RemoveAt(i);

                    // Spawn a new particle to replace it
                    CreateParticle();
                }
            }

            // Raise particle count changed event
            GameEvents.RaiseParticleCountChanged(particles.Count);
        }

        /// <summary>
        /// Check for collisions between particles and ship
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public bool CheckCollisions(Point shipPosition)
        {
            double shipX = shipPosition.X;
            double shipY = shipPosition.Y;
            const double maxCheckDistance = 50; // Only check nearby particles

            foreach (var particle in particles)
            {
                // Quick distance check first (cheaper than full calculation)
                double roughDeltaX = Math.Abs(shipX - particle.X);
                double roughDeltaY = Math.Abs(shipY - particle.Y);

                // Skip if obviously too far away
                if (roughDeltaX > maxCheckDistance || roughDeltaY > maxCheckDistance)
                    continue;

                // Now do precise collision detection
                double particleX = particle.X + 4; // Center of particle (8x8 ellipse)
                double particleY = particle.Y + 4;
                double deltaX = shipX - particleX;
                double deltaY = shipY - particleY;
                double distanceSquared = deltaX * deltaX + deltaY * deltaY;

                if (distanceSquared < 225) // 15^2
                {
                    // Use GameEvents instead of direct event
                    GameEvents.RaiseCollisionDetected();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generate more particles for level progression
        /// </summary>
        /// <param name="newParticlesPerLevel">Percentage increase in particles</param>
        public void GenerateMoreParticles(double newParticlesPerLevel)
        {
            // Increase particle count by the specified percentage
            currentParticleCount = (int)(currentParticleCount * (1 + newParticlesPerLevel / 100.0));
            CreateParticles(Math.Max(1, currentParticleCount / 10)); // Add some particles
        }

        /// <summary>
        /// Clear all particles from the game
        /// </summary>
        public void ClearAllParticles()
        {
            foreach (var particle in particles)
            {
                if (particle.IsActive)
                {
                    gameCanvas.Children.Remove(particle.Visual);
                    particle.IsActive = false;
                    particlePool.Enqueue(particle);
                }
            }
            particles.Clear();
        }

        /// <summary>
        /// Create multiple particles
        /// </summary>
        /// <param name="count">Number of particles to create</param>
        private void CreateParticles(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateParticle();
            }
        }

        /// <summary>
        /// Get a particle from the pool or create a new one
        /// </summary>
        /// <returns>Pooled or new particle</returns>
        private Patricle GetPooledParticle()
        {
            if (particlePool.Count > 0)
            {
                var pooled = particlePool.Dequeue();
                pooled.IsActive = true;
                pooled.IsFreshlySpawned = true;
                return pooled;
            }

            return new Patricle
            {
                Visual = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White // Default color
                },
                IsActive = true
            };
        }

        /// <summary>
        /// Create a single particle with randomized properties
        /// </summary>
        private void CreateParticle()
        {
            var particle =  GetPooledParticle();

            // Spawn particle outside game area
            Point spawnPosition = GetRandomSpawnPosition();
            particle.Position = new Vector2((float)spawnPosition.X, (float)spawnPosition.Y);

            // Calculate base speed for this particle
            double actualParticleSpeed = activeParticleSpeed;
            double actualParticleTurnSpeed = activeParticleTurnSpeed;

            // Speed randomization
            int randomValue = random.Next(1, 101); // 1 to 100
            if (randomValue <= activeParticleRandomizerPercentage)
            {
                // Apply speed variance (randomly faster or slower)
                double varianceMultiplier = (random.NextDouble() * 2 - 1) * (activeParticleSpeedVariance / 100.0);
                actualParticleSpeed = activeParticleSpeed * (1 + varianceMultiplier);

                // Ensure speed doesn't go negative or too slow
                actualParticleSpeed = Math.Max(actualParticleSpeed, activeParticleSpeed * 0.1);
            }

            // Turn speed randomization
            randomValue = random.Next(1, 101); // 1 to 100
            if (randomValue <= activeParticleRandomizerPercentage)
            {
                // Ensure turn speed doesn't go negative or too slow
                var multiplier = random.NextDouble();
                actualParticleTurnSpeed = multiplier * activeParticleTurnSpeed;
            }

            // Store the actual speed and turn speed in the particle
            particle.Speed = actualParticleSpeed;
            particle.TurnSpeed = (float)actualParticleTurnSpeed;

            // Set particle color based on speed
            particle.SetColorBasedOnSpeed(activeParticleSpeed);

            // Set the IsSpawnVectorTowardsShip property
            particle.IsSpawnVectorTowardsShip = activeIsParticleSpawnVectorTowardsShip;

            // Set whether particle should chase ship
            particle.ShouldChaseShip = activeShouldParticlesChaseShip;

            // Calculate initial velocity based on spawn vector setting
            Vector2 target;
            if (activeIsParticleSpawnVectorTowardsShip)
            {
                // This would need ship position - we'll use center for now and update in MainWindow
                target = new Vector2((float)centerScreen.X, (float)centerScreen.Y);
            }
            else
            {
                // Aim towards the center screen
                target = new Vector2((float)centerScreen.X, (float)centerScreen.Y);
            }

            // Set initial velocity
            particle.SetInitialVelocity(target, (float)actualParticleSpeed);

            // Position the visual element
            Canvas.SetLeft(particle.Visual, particle.X);
            Canvas.SetTop(particle.Visual, particle.Y);

            particles.Add(particle);
            gameCanvas.Children.Add(particle.Visual);
        }

        /// <summary>
        /// Update initial velocity for a particle to target ship position
        /// </summary>
        /// <param name="particle">Particle to update</param>
        /// <param name="shipPosition">Current ship position</param>
        public void SetParticleTargetToShip(Patricle particle, Point shipPosition)
        {
            if (particle.IsSpawnVectorTowardsShip)
            {
                var target = new Vector2((float)shipPosition.X, (float)shipPosition.Y);
                particle.SetInitialVelocity(target, (float)particle.Speed);
            }
        }

        /// <summary>
        /// Get a random spawn position outside the game area
        /// </summary>
        /// <returns>Random spawn position</returns>
        private Point GetRandomSpawnPosition()
        {

            int angle = random.Next(360);

            double theta = angle * (Math.PI / 180); // Convert to radians

            Point point = new Point();
            double radius = Math.Max(canvasWidth, canvasHeight) / 2 + 250; // Spawn just outside view

            radius = radius += Math.Abs(random.NextDouble() * 2 - 1) * .5;

            point.X = radius * Math.Cos(theta) + centerScreen.X;
            point.Y = radius * Math.Sin(theta) + centerScreen.Y;

            return point;

            int side = random.Next(4); // 0: top, 1: right, 2: bottom, 3: left

            return side switch
            {
                0 => new Point(random.NextDouble() * canvasWidth, -20), // Top
                1 => new Point(canvasWidth + 20, random.NextDouble() * canvasHeight), // Right
                2 => new Point(random.NextDouble() * canvasWidth, canvasHeight + 20), // Bottom
                3 => new Point(-20, random.NextDouble() * canvasHeight), // Left
                _ => new Point(0, 0)
            };



        }
    }
}