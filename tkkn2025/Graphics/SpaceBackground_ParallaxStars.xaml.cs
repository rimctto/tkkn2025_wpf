using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace tkkn2025.Graphics
{
    /// <summary>
    /// Parallax stars background with scrolling effect
    /// </summary>
    public partial class SpaceBackground_ParallaxStars : UserControl
    {
        private Random _rand = new Random();
        private DispatcherTimer _starAnimationTimer;
        private List<Star> _stars;
        private readonly int _maxStars = 100; // Maximum number of stars on screen
        private readonly double _spawnRate = 1; // Stars spawned per second
        private double _timeSinceLastSpawn = 0;

        public SpaceBackground_ParallaxStars()
        {
            InitializeComponent();
            Loaded += SpaceBackground_ParallaxStars_Loaded;
            Unloaded += SpaceBackground_ParallaxStars_Unloaded;
        }

        private void SpaceBackground_ParallaxStars_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeStars();
            StartAnimation();
        }

        private void SpaceBackground_ParallaxStars_Unloaded(object sender, RoutedEventArgs e)
        {
            _starAnimationTimer?.Stop();
        }

        private void InitializeStars()
        {
            _stars = new List<Star>();
            
            // Create initial stars scattered across the screen
            int initialStarCount = Math.Min(_maxStars / 2, 50);
            for (int i = 0; i < initialStarCount; i++)
            {
                CreateStar(randomY: true);
            }

            // Initialize and start the animation timer
            _starAnimationTimer = new DispatcherTimer();
            _starAnimationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _starAnimationTimer.Tick += StarAnimationTimer_Tick;
        }

        private void StartAnimation()
        {
            _starAnimationTimer?.Start();
        }

        private void StarAnimationTimer_Tick(object sender, EventArgs e)
        {
            double deltaTime = 0.016; // 16ms in seconds
            
            // Update spawn timing
            _timeSinceLastSpawn += deltaTime;
            
            // Spawn new stars
            if (_timeSinceLastSpawn >= (1.0 / _spawnRate) && _stars.Count < _maxStars)
            {
                CreateStar();
                _timeSinceLastSpawn = 0;
            }
            
            // Update existing stars
            UpdateStars(deltaTime);
            
            // Remove stars that have moved off screen
            RemoveOffScreenStars();
        }

        private void CreateStar(bool randomY = false)
        {
            if (this.ActualWidth <= 0 || this.ActualHeight <= 0) return;
            
            // Random position
            double x = _rand.NextDouble() * this.ActualWidth;
            double y = randomY ? _rand.NextDouble() * this.ActualHeight : -10; // Start above screen or random for initial
            
            // Random size
            double radius = 1 + 2 * _rand.NextDouble(); // 1 to 3
            
            // Random speed based on size for parallax effect (smaller = farther = slower)
            double baseSpeed = 50; // pixels per second
            double speedMultiplier = radius / 3.0; // Normalize to 0.25 - 1.0
            speedMultiplier = 0.3 + (speedMultiplier * 0.7); // Range: 0.3 - 1.0
            double speed = baseSpeed * speedMultiplier;
            
            // Random color in light gray to white range
            byte grayValue = (byte)_rand.Next(150, 190); // 180-255 for light gray to white
            Color starColor = Color.FromRgb(grayValue, grayValue, grayValue);
            
            // Create visual element
            Ellipse starVisual = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(starColor),
                Stroke = null // No border
            };
            
            // Position the star
            Canvas.SetLeft(starVisual, x - radius);
            Canvas.SetTop(starVisual, y - radius);
            
            // Add to canvas
            StarCanvas.Children.Add(starVisual);
            
            // Create star data object
            var star = new Star
            {
                Visual = starVisual,
                X = x,
                Y = y,
                Radius = radius,
                Speed = speed
            };
            
            _stars.Add(star);
        }

        private void UpdateStars(double deltaTime)
        {
            foreach (var star in _stars)
            {
                // Move star down
                star.Y += star.Speed * deltaTime;
                
                // Update visual position
                Canvas.SetTop(star.Visual, star.Y - star.Radius);
            }
        }

        private void RemoveOffScreenStars()
        {
            for (int i = _stars.Count - 1; i >= 0; i--)
            {
                var star = _stars[i];
                
                // Remove if star is below the screen
                if (star.Y - star.Radius > this.ActualHeight + 50)
                {
                    StarCanvas.Children.Remove(star.Visual);
                    _stars.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Data class for star properties
        /// </summary>
        private class Star
        {
            public Ellipse Visual { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Radius { get; set; }
            public double Speed { get; set; }
        }

        /// <summary>
        /// Public property to control star spawn rate
        /// </summary>
        public double StarSpawnRate
        {
            get { return _spawnRate; }
        }

        /// <summary>
        /// Public property to control maximum number of stars
        /// </summary>
        public int MaxStars
        {
            get { return _maxStars; }
        }

        /// <summary>
        /// Clear all stars from the canvas
        /// </summary>
        public void ClearStars()
        {
            foreach (var star in _stars)
            {
                StarCanvas.Children.Remove(star.Visual);
            }
            _stars.Clear();
        }

        /// <summary>
        /// Get current number of active stars
        /// </summary>
        public int ActiveStarCount
        {
            get { return _stars.Count; }
        }
    }
}