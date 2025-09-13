using System.Windows.Media;
using System.Windows.Shapes;

namespace tkkn2025
{
    /// <summary>
    /// Represents a particle in the game with position, velocity, and visual properties
    /// </summary>
    internal class Patricle
    {
        public Ellipse Visual { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; } // pixels per second
        public double VelocityY { get; set; } // pixels per second
        public bool IsActive { get; set; }
        public double Speed { get; set; } // pixels per second
        public Brush Color { get; set; } = Brushes.White; // default color
    }
}