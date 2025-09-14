using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Represents a singularity that applies gravitational force to particles
    /// </summary>
    public class Singularity : GameObject
    {
        public float Mass { get; set; }
        public double RemainingTime { get; set; }
        public Ellipse? Visual { get; set; }

        public Singularity(Vector2 position, float mass = 350f, double duration = 5.0) : base(position)
        {
            Mass = mass;
            RemainingTime = duration;
            IsActive = true;
            CreateVisual();
        }

        private void CreateVisual()
        {
            Visual = new Ellipse
            {
                Width = 50,
                Height = 50
            };

            // Create radial gradient from white center to black edge
            var gradient = new RadialGradientBrush();
            gradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            gradient.GradientStops.Add(new GradientStop(Colors.LightGray, 0.3));
            gradient.GradientStops.Add(new GradientStop(Colors.Gray, 0.6));
            gradient.GradientStops.Add(new GradientStop(Colors.DarkGray, 0.8));
            gradient.GradientStops.Add(new GradientStop(Colors.Black, 1.0));

            Visual.Fill = gradient;
            Visual.Stroke = Brushes.White;
            Visual.StrokeThickness = 1;
            Visual.Opacity = 0.8;
        }

        /// <summary>
        /// Update the singularity (mainly for timer countdown)
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <returns>True if singularity is still active</returns>
        public bool Update(double deltaTime)
        {
            RemainingTime -= deltaTime;
            
            // Update visual opacity based on remaining time
            if (Visual != null && RemainingTime > 0)
            {
                double normalizedTime = Math.Max(0, RemainingTime / 5.0); // Assuming 5 second duration
                Visual.Opacity = 0.4 + 0.4 * normalizedTime; // Fade from 0.8 to 0.4
            }
            
            return RemainingTime > 0;
        }

        /// <summary>
        /// Apply gravitational force to a particle
        /// </summary>
        /// <param name="particlePosition">Particle position</param>
        /// <param name="particleVelocity">Particle velocity (will be modified)</param>
        /// <param name="particleMass">Particle mass</param>
        /// <param name="G">Gravitational constant</param>
        /// <param name="deltaTime">Time step</param>
        public void ApplyForceToParticle(ref Vector2 particlePosition, ref Vector2 particleVelocity, float particleMass, float G, float deltaTime)
        {
            if (!IsActive) return;

            Vector2 dir = Position - particlePosition;
            float distSq = dir.LengthSquared();

            if (distSq < 0.0001f) return; // avoid divide by zero

            float dist = MathF.Sqrt(distSq);
            Vector2 dirNormalized = dir / dist;

            // Force magnitude: F = G * m1 * m2 / r²
            float force = G * particleMass * Mass / (distSq * 1.5f);

            // Acceleration: a = F / m
            Vector2 acceleration = dirNormalized * force / particleMass;

            // Update velocity
            particleVelocity += acceleration * deltaTime;
        }
    }
}