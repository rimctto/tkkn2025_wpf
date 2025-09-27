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
        }

        protected Entity()
        {
            Position = Vector2.Zero;
            Velocity = new Vector2(1, 0);
        }

        public void Update(float deltaTime)
        {
            foreach (var behavior in Behaviors)
            {
                behavior.ApplyUpdate(deltaTime);
            }
        }

        
    }
}