using System;
using System.Numerics;
using System.Windows.Media;
using tkkn2025.Core.Behaviors;

namespace tkkn2025
{
    /// <summary>
    /// Base class for all game objects with position, velocity, and basic properties
    /// </summary>
    public abstract class Entity
    {
        private bool _disposedValue;

        public List<IBehavior> Behaviors { get; } = new List<IBehavior>();

        // Position management
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Rotation { get; set; }


        // Rotation matrix variables for transformations
        private double ix, iy, jx, jy;

        public double X 
        { 
            get => Position.X; 
            set => Position = new Vector2((float)value, Position.Y); 
        }
        public double Y 
        { 
            get => Position.Y; 
            set => Position = new Vector2(Position.X, (float)value); 
        }
 

        public bool IsActive { get; set; }
        public double Speed { get; set; } // pixels per second
        


        protected Entity(Vector2 startPosition)
        {
            Position = startPosition;
            Velocity = new Vector2(1, 0); // initial velocity pointing right
            UpdateRotationMatrix();
        }

        protected Entity()
        {
            Position = Vector2.Zero;
            Velocity = new Vector2(1, 0);
            UpdateRotationMatrix();
        }

        public void Update(float deltaTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.ApplyUpdate(deltaTime);
            }
        }

        /// <summary>
        /// Update rotation matrix based on current frame or rotation angle
        /// </summary>
        /// <param name="angle">Rotation angle in radians. If null, uses entity's Rotation property</param>
        public void UpdateRotationMatrix(double? angle = null)
        {
            double rotationAngle = angle ?? Rotation;
            ix = Math.Cos(rotationAngle);
            iy = Math.Sin(rotationAngle) * -1;
            jx = Math.Sin(rotationAngle);
            jy = Math.Cos(rotationAngle);
        }

        /// <summary>
        /// Apply rotation matrix transformation to a 2D point
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Rotated coordinates as a Vector2</returns>
        public Vector2 ApplyRotation(double x, double y)
        {
            var rotatedX = x * ix + y * iy;
            var rotatedY = x * jx + y * jy;
            return new Vector2((float)rotatedX, (float)rotatedY);
        }

    }
}