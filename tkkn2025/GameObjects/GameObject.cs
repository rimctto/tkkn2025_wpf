using System;
using System.Numerics;
using System.Windows.Media;

namespace tkkn2025
{
    /// <summary>
    /// Base class for all game objects with position, velocity, and basic properties
    /// </summary>
    public abstract class GameObject
    {
        // Position management
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        
        // Legacy properties for backward compatibility
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
        public Brush Color { get; set; } = Brushes.White; // default color

        /// <summary>
        /// Initialize game object with starting position
        /// </summary>
        /// <param name="startPosition">Starting position as Vector2</param>
        protected GameObject(Vector2 startPosition)
        {
            Position = startPosition;
            Velocity = new Vector2(1, 0); // initial velocity pointing right
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected GameObject()
        {
            Position = Vector2.Zero;
            Velocity = new Vector2(1, 0);
        }
    }
}