using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace tkkn2025
{
    /// <summary>
    /// Represents a particle in the game with position, velocity, and visual properties
    /// </summary>
    public class Patricle : GameObject
    {
        public Ellipse Visual { get; set; } = null!;
        
        /// <summary>
        /// Indicates whether this particle's spawn vector was directed towards the ship
        /// This value is set when the particle is created based on the game setting
        /// </summary>
        public bool IsSpawnVectorTowardsShip { get; set; }
        
        /// <summary>
        /// Turn speed for steering behavior (radians per second)
        /// </summary>
        public float TurnSpeed { get; set; }
        
        /// <summary>
        /// Whether this particle should chase the ship or move in straight line
        /// </summary>
        public bool ShouldChaseShip { get; set; } = false;

        /// <summary>
        /// Initialize particle with starting position
        /// </summary>
        /// <param name="startPosition">Starting position as Vector2</param>
        public Patricle(Vector2 startPosition) : base(startPosition)
        {
        }

        /// <summary>
        /// Default constructor for backward compatibility
        /// </summary>
        public Patricle() : base()
        {
        }

        /// <summary>
        /// Update particle position and behavior
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update (in seconds)</param>
        /// <param name="shipPosition">Current ship position for chasing behavior</param>
        public void Update(float deltaTime, Vector2 shipPosition)
        {
            if (ShouldChaseShip)
            {
                UpdateWithSteering(deltaTime, shipPosition);
            }
            else
            {
                UpdateStraightLine(deltaTime);
            }
            
            // Update visual position
            UpdateVisualPosition();
        }

        /// <summary>
        /// Update particle with steering behavior to chase the ship
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        /// <param name="target">Target position (ship position)</param>
        private void UpdateWithSteering(float deltaTime, Vector2 target)
        {
            float speed = (float)Speed;
            
            // 1. Compute the desired direction
            Vector2 toTarget = target - Position;
            if (toTarget.Length() > 0.01f) // Avoid division by zero
            {
                float speed2D = Math.Min((float)Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y), 300);
                Vector2 desired = Vector2.Normalize(toTarget) * speed;

                // 2. Compute the steering (desired velocity - current velocity)
                Vector2 steer = desired - Velocity;

                // 3. Limit the rotation: gradually change direction
                float angle = MathF.Atan2(Velocity.Y, Velocity.X);
                float desiredAngle = MathF.Atan2(desired.Y, desired.X);

                // Compute smallest angle difference
                float deltaAngle = WrapAngle(desiredAngle - angle);

                // Rotate the current velocity by a fraction of deltaAngle
                float maxTurn = TurnSpeed * deltaTime;
                float turn = Clamp(deltaAngle, -maxTurn, maxTurn);

                float newAngle = angle + turn;

                // Keep the speed magnitude consistent
                Velocity = new Vector2(MathF.Cos(newAngle), MathF.Sin(newAngle)) * speed2D;
            }

            // 4. Move particle
            Position += Velocity * deltaTime;
        }

        /// <summary>
        /// Update particle with straight-line movement
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private void UpdateStraightLine(float deltaTime)
        {
            Position += Velocity * deltaTime;
        }

        /// <summary>
        /// Clamp a value between min and max
        /// </summary>
        /// <param name="value">Value to clamp</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Clamped value</returns>
        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Wrap angle to [-PI, PI]
        /// </summary>
        /// <param name="angle">Angle in radians</param>
        /// <returns>Wrapped angle</returns>
        private float WrapAngle(float angle)
        {
            while (angle < -MathF.PI) angle += 2 * MathF.PI;
            while (angle > MathF.PI) angle -= 2 * MathF.PI;
            return angle;
        }

        /// <summary>
        /// Update the visual element position based on current particle position
        /// </summary>
        private void UpdateVisualPosition()
        {
            if (Visual != null)
            {
                Canvas.SetLeft(Visual, Position.X);
                Canvas.SetTop(Visual, Position.Y);
            }
        }

        /// <summary>
        /// Set initial velocity based on target position and speed
        /// </summary>
        /// <param name="target">Target position to aim towards</param>
        /// <param name="speed">Movement speed</param>
        public void SetInitialVelocity(Vector2 target, float speed)
        {
            Vector2 direction = target - Position;
            if (direction.Length() > 0.01f)
            {
                direction = Vector2.Normalize(direction);
                Velocity = direction * speed;
                Speed = speed;
            }
        }

        /// <summary>
        /// Sets the particle color based on its speed relative to the default speed
        /// /// <param name="defaultSpeed">The default/base particle speed to compare against</param>
        /// </summary>
        public void SetColorBasedOnSpeed(double defaultSpeed)
        {
            Brush selectedColor = Brushes.White;

            //faster 
            if (Speed > defaultSpeed)
            {
                // Faster than default - Red
                selectedColor = Brushes.Red;
                if(TurnSpeed > 0.7)
                {
                    // Very fast - Orange
                    selectedColor = Brushes.DarkOrange;
                }
                else if (TurnSpeed > 0.3)
                {
                    // High turn speed - Green
                    selectedColor = Brushes.Red;
                }
            }

            //slower
            else if (Speed < defaultSpeed)
            {
                // Slower than default - Blue
                selectedColor = Brushes.Blue; 
                 
                if (TurnSpeed > 0.7)
                {
                    // Very fast - Orange
                    selectedColor = Brushes.LightBlue;
                }
                else if (TurnSpeed > 0.3)
                {
                    // High turn speed - Green
                   selectedColor = Brushes.DarkBlue;
                }
            }

            
            //turn speed only
            else if (TurnSpeed > 0.7)
            {
                // High turn speed - Green
                selectedColor = Brushes.MediumPurple;
            }
            else if(TurnSpeed > 0.3)
            {
                // High turn speed - Green
                selectedColor = Brushes.White;  
            }
           
            
            // Update both the internal Color property and the Visual element
            Color = selectedColor;
            if (Visual != null)
            {
                Visual.Fill = selectedColor;
            }

        }

        /// <summary>
        /// Applies the current color to the visual element
        /// Useful when the visual is created after the color is set
        /// </summary>
        public void ApplyColorToVisual()
        {
            if (Visual != null)
            {
                Visual.Fill = Color;
            }
        }
    }
}