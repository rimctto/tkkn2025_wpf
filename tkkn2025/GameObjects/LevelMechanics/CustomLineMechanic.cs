using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects;

namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Level mechanic that creates line formations based on coordinates in percentage of canvas dimensions
    /// Lines are drawn connected from previous line endpoints and move down the screen
    /// </summary>
    public class CustomLineMechanic : ParticleMechanicsBase
    {
        // Mechanic configuration
        public override int ActivationLevel { get; }
        public override int ParticleCount { get; }
        private readonly double particleSpeed;
        
        // Line specific configuration
        private readonly double x1Percent, y1Percent, x2Percent, y2Percent;
        private readonly Brush lineColor;
        private readonly bool isFirstLine;
        private readonly CustomLineMechanic? previousLine;
        
        // Line state
        private List<Vector2> linePositions;
        private readonly double activationThreshold; // Y coordinate threshold for next line activation
        private bool hasActivatedNextLine = false;
        
        // Event for triggering next line
        public event EventHandler<CustomLineMechanic>? NextLineActivated;
        
        public CustomLineMechanic(int activationLevel = 0, int particleCount = 20, double particleSpeed = 200.0,
                                 double x1Percent = 0.2, double y1Percent = 0.1, 
                                 double x2Percent = 0.5, double y2Percent = 0.6,
                                 Brush? color = null, bool firstLine = false, 
                                 CustomLineMechanic? previousLine = null)
        {
            ActivationLevel = activationLevel;
            ParticleCount = Math.Max(1, particleCount);
            this.particleSpeed = Math.Max(50.0, particleSpeed);
            
            // Store line coordinates as percentages
            this.x1Percent = Math.Clamp(x1Percent, 0.0, 1.0);
            this.y1Percent = Math.Clamp(y1Percent, 0.0, 1.0);
            this.x2Percent = Math.Clamp(x2Percent, 0.0, 1.0);
            this.y2Percent = Math.Clamp(y2Percent, 0.0, 1.0);
            
            // Set default color if not provided
            this.lineColor = color ?? Brushes.MediumPurple;
            this.isFirstLine = firstLine;
            this.previousLine = previousLine;

            // Set activation threshold to -1 normalized (just before visible area)
            this.activationThreshold = -1.0;
            
            linePositions = new List<Vector2>();
        }

        /// <summary>
        /// Called when the mechanic is activated
        /// </summary>
        protected override void OnActivate()
        {
            // Calculate line positions based on canvas dimensions
            CalculateLinePositions();
            
            // Create all particles at once in the line formation
            CreateLineFormation();
        }

        /// <summary>
        /// Called when the mechanic is stopped
        /// </summary>
        protected override void OnStop()
        {
            hasActivatedNextLine = false;
        }

        /// <summary>
        /// Called every frame when the mechanic is active
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private Particle? lastParticle = null;
        protected override void OnUpdate(double deltaTime)
        {
            if (mechanicParticles.Count == 0) return;
            
            // Get the last particle (the one at the end of the line)
            lastParticle = mechanicParticles[mechanicParticles.Count - 1];

            var particlesToRemove = new List<Particle>();
            var movementVector = new Vector2(0, (float)(LevelManager.CurrentLevelSpeed * deltaTime));

            foreach (var particle in mechanicParticles)
            {
                // Update particle position
                particle.Position += movementVector;
                
                // Update visual position
                if (particle.Visual != null)
                {
                    Canvas.SetLeft(particle.Visual, particle.Position.X);
                    Canvas.SetTop(particle.Visual, particle.Position.Y);
                }

                // Check if particle has left the screen
                if (particle.Position.Y > canvasHeight)
                {
                    particlesToRemove.Add(particle);
                }
            }

            // Check if we should activate the next line (when last particle reaches y = -1 threshold)
            if (!hasActivatedNextLine && lastParticle != null && lastParticle.Position.Y >= activationThreshold)
            {
                hasActivatedNextLine = true;
                NextLineActivated?.Invoke(this, this);
                System.Diagnostics.Debug.WriteLine($"?? Line activated next line at Y position: {lastParticle.Position.Y}");
            }

            // Remove particles that have left the screen
            foreach (var particle in particlesToRemove)
            {
                RemoveParticle(particle);
            }
        }

        /// <summary>
        /// Calculate line positions based on canvas dimensions and previous line if applicable
        /// </summary>
        private void CalculateLinePositions()
        {
            linePositions.Clear();
            
            Vector2 startPoint, endPoint;
            
            if (isFirstLine || previousLine == null)
            {
                // Use provided percentages for first line or standalone line
                startPoint = new Vector2(
                    (float)(x1Percent * canvasWidth),
                    (float)(y1Percent * -canvasHeight) // Start above screen
                );
                endPoint = new Vector2(
                    (float)(x2Percent * canvasWidth),
                    (float)(y2Percent * -canvasHeight) // Start above screen
                );
            }
            else
            {
                // Connect from previous line's endpoint to new endpoint
                startPoint = new Vector2(
                    (float)(x1Percent * canvasWidth),
                    (float)(y1Percent * -canvasHeight) 
                );
                
                // Calculate new endpoint based on provided percentages
                endPoint = new Vector2(
                    (float)(x2Percent * canvasWidth),
                    (float)(y2Percent * -canvasHeight) 
                );
            }
            
            // Distribute particles along the line
            for (int i = 0; i < ParticleCount; i++)
            {
                float t = ParticleCount > 1 ? (float)i / (ParticleCount - 1) : 0.5f;
                Vector2 position = Vector2.Lerp(startPoint, endPoint, t);
                linePositions.Add(position);
            }
        }

        /// <summary>
        /// Create all particles in the line formation at once
        /// </summary>
        private void CreateLineFormation()
        {
            try
            {
                var movementDirection = new Vector2(0, 1); // Moving straight down
                
                for (int i = 0; i < linePositions.Count; i++)
                {
                    Vector2 position = linePositions[i];
                    
                    // Create particle with downward movement direction
                    var particle = CreateParticleWithLevelSpeed(position, movementDirection, lineColor, 6.0);

                    // Set additional properties
                    particle.ShouldChaseShip = false; // Line particles move in straight lines
                    particle.IsSpawnVectorTowardsShip = false;
                    particle.IsFreshlySpawned = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating custom line formation: {ex.Message}");
            }
        }

      
        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing CustomLine mechanic: {ex.Message}");
            }
        }
    }
}