using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using tkkn2025.GameObjects; // Add this for LevelManager access

namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Level mechanic that creates a star formation of particles that moves from top to bottom of screen
    /// The star formation maintains its shape while moving downward at a random X position
    /// </summary>
    public class StarMechanic : ParticleMechanicsBase
    {
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        private readonly double particleSpeed; // speed of the star formation moving downward
        
        // Star specific configuration
        private readonly double minStarSize; // minimum star radius
        private readonly double maxStarSize; // maximum star radius
        private readonly int minPoints; // minimum number of star points
        private readonly int maxPoints; // maximum number of star points
        private readonly bool randomizeSize; // whether to randomize star size per activation
        
        // Star formation state
        private Vector2 starCenterPosition; // current center of the star formation
        private List<Vector2> relativeStarPositions; // relative positions from center for each particle
        private readonly Random random;
        private readonly Vector2 movementDirection = new Vector2(0, 1); // Moving straight down
        
        // Particle properties
        private readonly Brush particleColor = Brushes.Gold; // Distinct color for star particles

        public StarMechanic(int activationLevel = 5, int particleCount = 24, double launchTiming = 0.1, 
                           double particleSpeed = 200.0, Vector2? centerPosition = null,
                           double minStarSize = 40.0, double maxStarSize = 80.0,
                           int minPoints = 5, int maxPoints = 8, bool randomizeSize = true)
        {
            ActivationLevel = activationLevel;
            ParticleCount = Math.Max(1, particleCount);
            this.particleSpeed = Math.Max(50.0, particleSpeed);
            
            this.minStarSize = Math.Max(25.0, minStarSize);
            this.maxStarSize = Math.Max(this.minStarSize, maxStarSize);
            this.minPoints = Math.Max(3, minPoints);
            this.maxPoints = Math.Max(this.minPoints, maxPoints);
            this.randomizeSize = randomizeSize;
            
            relativeStarPositions = new List<Vector2>();
            random = new Random();

            System.Diagnostics.Debug.WriteLine($"?? Star mechanic created: Level {ActivationLevel}, {ParticleCount} particles, " +
                                             $"{particleSpeed} speed, " +
                                             $"size: {minStarSize}-{maxStarSize}, points: {minPoints}-{maxPoints}");
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Calculate relative star positions (offsets from center)
            CalculateStarFormation();
            
            // Set initial star center position at top of screen with random X
            double randomX = random.NextDouble() * (canvasWidth - maxStarSize * 2) + maxStarSize;
            starCenterPosition = new Vector2((float)randomX, -(float)maxStarSize); // Start above screen
            
            // Create all particles at once in the star formation
            CreateStarFormation();
            
            System.Diagnostics.Debug.WriteLine($"?? Star formation activated at X:{randomX:F0}, moving downward with {ParticleCount} particles");
        }

        /// <summary>
        /// Called when the mechanic is stopped
        /// </summary>
        protected override void OnStop()
        {
            System.Diagnostics.Debug.WriteLine("?? Star formation stopped and particles cleared");
        }

        /// <summary>
        /// Called every frame when the mechanic is active
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        protected override void OnUpdate(double deltaTime)
        {
            if (mechanicParticles.Count == 0) return;

            // Move the star formation center downward
            starCenterPosition += movementDirection * (float)(LevelManager.CurrentLevelSpeed * deltaTime);

            var particlesToRemove = new List<Particle>();
            int particleIndex = 0;

            foreach (var particle in mechanicParticles)
            {
                // Update particle position to maintain formation relative to center
                if (particleIndex < relativeStarPositions.Count)
                {
                    particle.Position = starCenterPosition + relativeStarPositions[particleIndex];
                }
                
                // Update visual position
                if (particle.Visual != null)
                {
                    Canvas.SetLeft(particle.Visual, particle.Position.X);
                    Canvas.SetTop(particle.Visual, particle.Position.Y);
                }

                // Check if the entire star formation has moved below the screen
                if (starCenterPosition.Y > canvasHeight + maxStarSize)
                {
                    particlesToRemove.Add(particle);
                }
                
                particleIndex++;
            }

            // Remove particles when the formation has left the screen
            foreach (var particle in particlesToRemove)
            {
                RemoveParticle(particle);
            }
            
            // If the star has moved off screen, stop the mechanic
            if (starCenterPosition.Y > canvasHeight + maxStarSize)
            {
                System.Diagnostics.Debug.WriteLine("?? Star formation moved off screen");
            }
        }

        /// <summary>
        /// Calculate relative star positions for the formation
        /// </summary>
        private void CalculateStarFormation()
        {
            relativeStarPositions.Clear();
            
            // Determine star properties
            int pointCount = random.Next(minPoints, maxPoints + 1);
            double starSize = randomizeSize ? 
                random.NextDouble() * (maxStarSize - minStarSize) + minStarSize : 
                (minStarSize + maxStarSize) / 2;
            
            // Apply a scaling factor to make the overall formation smaller
            double formationScale = 0.6; // Scale down to 60% of original size
            starSize *= formationScale;
            
            // Generate star outline points
            var starOutlinePoints = new List<Vector2>();
            
            // Create star points (alternating outer and inner points)
            for (int point = 0; point < pointCount; point++)
            {
                // Outer point angle
                double outerAngle = (2 * Math.PI * point) / pointCount - Math.PI / 2; // Start from top
                
                // Outer point position (relative to center)
                Vector2 outerPoint = new Vector2(
                    (float)(Math.Cos(outerAngle) * starSize),
                    (float)(Math.Sin(outerAngle) * starSize)
                );
                starOutlinePoints.Add(outerPoint);
                
                // Inner point angle (between outer points)
                double innerAngle = outerAngle + (Math.PI / pointCount);
                
                // Inner point position (smaller radius, relative to center)
                Vector2 innerPoint = new Vector2(
                    (float)(Math.Cos(innerAngle) * starSize * 0.4),
                    (float)(Math.Sin(innerAngle) * starSize * 0.4)
                );
                starOutlinePoints.Add(innerPoint);
            }
            
            // Distribute particles more densely along the star outline with smaller spacing
            // Calculate total perimeter of the star to distribute particles more evenly
            float totalPerimeter = 0;
            for (int i = 0; i < starOutlinePoints.Count; i++)
            {
                Vector2 startPoint = starOutlinePoints[i];
                Vector2 endPoint = starOutlinePoints[(i + 1) % starOutlinePoints.Count];
                totalPerimeter += Vector2.Distance(startPoint, endPoint);
            }
            
            // Create a more compact distribution - use fewer segments but with tighter particle spacing
            double particleSpacing = 0.7; // Reduce spacing between particles (smaller = closer together)
            
            // Distribute particles based on segment length for even density
            for (int i = 0; i < starOutlinePoints.Count; i++)
            {
                Vector2 startPoint = starOutlinePoints[i];
                Vector2 endPoint = starOutlinePoints[(i + 1) % starOutlinePoints.Count];
                
                // Calculate how many particles for this segment based on its length
                float segmentLength = Vector2.Distance(startPoint, endPoint);
                
                // Use tighter particle spacing for more compact formation
                int particlesForSegment = Math.Max(1, (int)Math.Ceiling(segmentLength / (float)(starSize * particleSpacing * 0.1)));
                
                // Limit particles per segment to prevent too many particles in one area
                particlesForSegment = Math.Min(particlesForSegment, ParticleCount / starOutlinePoints.Count + 2);
                
                // Add particles along this segment with closer spacing
                for (int j = 0; j < particlesForSegment && relativeStarPositions.Count < ParticleCount; j++)
                {
                    float t = particlesForSegment > 1 ? (float)j / (particlesForSegment - 1) : 0.5f;
                    Vector2 position = Vector2.Lerp(startPoint, endPoint, t);
                    
                    // Apply additional compaction - pull particles slightly toward center
                    Vector2 compactedPosition = position * 0.85f; // Pull 15% closer to center
                    
                    relativeStarPositions.Add(compactedPosition);
                }
            }
            
            // Fill remaining particles if needed by adding them closer to the center
            while (relativeStarPositions.Count < ParticleCount)
            {
                // Add particles in inner areas for denser coverage
                for (int i = 0; i < starOutlinePoints.Count && relativeStarPositions.Count < ParticleCount; i++)
                {
                    Vector2 outerPoint = starOutlinePoints[i];
                    
                    // Create particles between center and outline points for density
                    Vector2 innerPosition = outerPoint * 0.6f; // 60% of the way to the point
                    relativeStarPositions.Add(innerPosition);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Generated compact star formation with {pointCount} points, scaled size {starSize:F1}, {relativeStarPositions.Count} particles");
        }

        /// <summary>
        /// Create all particles in the star formation at once
        /// </summary>
        private void CreateStarFormation()
        {
            try
            {
                for (int i = 0; i < relativeStarPositions.Count; i++)
                {
                    // Calculate absolute position for this particle
                    Vector2 absolutePosition = starCenterPosition + relativeStarPositions[i];
                    
                    // Create particle with downward movement direction
                    var particle = CreateParticleWithLevelSpeed(absolutePosition, movementDirection, particleColor, 6.0);

                    // Set additional properties
                    particle.ShouldChaseShip = false; // Star particles move in formation
                    particle.IsSpawnVectorTowardsShip = false;
                    particle.IsFreshlySpawned = true;
                }

                System.Diagnostics.Debug.WriteLine($"?? Star formation created with {mechanicParticles.Count} particles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating star formation: {ex.Message}");
            }
        }

        public override void Dispose()
        {
            try
            {
                base.Dispose();
                System.Diagnostics.Debug.WriteLine("?? Star mechanic disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Star mechanic: {ex.Message}");
            }
        }
    }
}