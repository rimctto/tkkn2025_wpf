using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Represents a repulsor that applies repelling force to particles and follows the ship
    /// </summary>
    public class Repulsor : GameObject
    {
        public float Mass { get; set; }
        public double RemainingTime { get; set; }
        public Canvas? Visual { get; set; }
        private readonly Storyboard? pulseAnimation;

        public Repulsor(Vector2 position, float mass = -35f, double duration = 5) : base(position)
        {
            Mass = -50f;
            RemainingTime = duration;
            IsActive = true;
            CreateVisual();
            StartPulseAnimation();
        }


        /// <summary>
        /// Apply repelling force to a particle
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

            // Force magnitude: F = G * m1 * m2 / r² (Mass is negative for repelling)
            float force = G * particleMass * Mass / (distSq);

            // Acceleration: a = F / m
            Vector2 acceleration = dirNormalized * force / particleMass;

            // Update velocity
            particleVelocity += acceleration * deltaTime;
        }

        
        private void CreateVisual()
        {
            Visual = new Canvas
            {
                Width = 100,
                Height = 100
            };

            // Main green sphere
            var mainSphere = new Ellipse
            {
                Width = 40,
                Height = 40,
                Fill = Brushes.Lime,
                Opacity = 0.25
            };
            Canvas.SetLeft(mainSphere, 30); // Center in 100x100 canvas
            Canvas.SetTop(mainSphere, 30);
            Visual.Children.Add(mainSphere);

            // Create multiple animated circles for pulsing effect
            for (int i = 0; i < 3; i++)
            {
                var pulseCircle = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Stroke = Brushes.LimeGreen,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent,
                    Opacity = 0.8
                };
                Canvas.SetLeft(pulseCircle, 40); // Center in 100x100 canvas
                Canvas.SetTop(pulseCircle, 40);
                Visual.Children.Add(pulseCircle);

                // Create pulsing animation for this circle
                CreatePulseAnimation(pulseCircle, i * 0.5); // Stagger the animations
            }
        }

        private void CreatePulseAnimation(Ellipse circle, double delay)
        {
            // Scale animation
            var scaleTransform = new ScaleTransform(1, 1, 10, 10); // Center of scaling
            circle.RenderTransform = scaleTransform;

            var scaleAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 4.0,
                Duration = TimeSpan.FromSeconds(2.0),
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(delay)
            };

            // Opacity animation
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(2.0),
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(delay)
            };

            // Apply animations
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            circle.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private void StartPulseAnimation()
        {
            if (Visual == null) return;

            // The animations are already started in CreatePulseAnimation method
            // This method can be used for additional visual effects if needed
        }

        /// <summary>
        /// Update the repulsor position to follow the ship and handle timer countdown
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <returns>True if repulsor is still active</returns>
        public bool Update(Vector2 shipPosition, double deltaTime)
        {
            RemainingTime -= deltaTime;
            
            // Update position to follow the ship
            Position = shipPosition;
            
            // Update visual opacity based on remaining time
            if (Visual != null && RemainingTime > 0)
            {
                double normalizedTime = Math.Max(0, RemainingTime / 5.0); // Assuming 5 second duration
                Visual.Opacity = 0.6 + 0.4 * normalizedTime; // Fade from 1.0 to 0.6
            }
            
            return RemainingTime > 0;
        }

        /// <summary>
        /// Stop all animations when the repulsor is destroyed
        /// </summary>
        public void StopAnimations()
        {
            if (Visual != null)
            {
                foreach (UIElement child in Visual.Children)
                {
                    if (child is Ellipse ellipse)
                    {
                        ellipse.BeginAnimation(UIElement.OpacityProperty, null);
                        if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                        {
                            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                        }
                    }
                }
            }
        }
    }
}