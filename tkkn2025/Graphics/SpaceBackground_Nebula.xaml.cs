using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace tkkn2025.Graphics
{
    public partial class SpaceBackground_Nebula : UserControl
    {
        private Random _rand = new Random();
        
        // Configurable maximum opacity for clouds
        private double _maxCloudOpacity = 0.7;

        // Timer for cloud position animation
        private DispatcherTimer _cloudAnimationTimer;
        
        // Cloud movement data
        private List<CloudMovementData> _cloudMovements;
        
        // Movement boundaries (50% beyond canvas)
        private double _minX, _maxX, _minY, _maxY;

        public SpaceBackground_Nebula()
        {
            InitializeComponent();
            Loaded += SpaceBackground_Loaded;
            Unloaded += SpaceBackground_Unloaded;
        }

        private void SpaceBackground_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCloudMovement();
            StartAnimations();
        }

        private void SpaceBackground_Unloaded(object sender, RoutedEventArgs e)
        {
            _cloudAnimationTimer?.Stop();
        }

        private void InitializeCloudMovement()
        {
            // Calculate movement boundaries (50% beyond visible canvas)
            _minX = -this.ActualWidth * 0.5;
            _maxX = this.ActualWidth * 1.5;
            _minY = -this.ActualHeight * 0.5;
            _maxY = this.ActualHeight * 1.5;

            // Initialize cloud movement data
            _cloudMovements = new List<CloudMovementData>
            {
                new CloudMovementData(Cloud1, _rand),
                new CloudMovementData(Cloud2, _rand),
                new CloudMovementData(Cloud3, _rand),
                new CloudMovementData(Cloud4, _rand),
                new CloudMovementData(Cloud5, _rand),
                new CloudMovementData(Cloud6, _rand)
            };

            // Set initial random positions
            foreach (var cloudData in _cloudMovements)
            {
                Canvas.SetLeft(cloudData.CloudElement, _rand.NextDouble() * (_maxX - _minX) + _minX);
                Canvas.SetTop(cloudData.CloudElement, _rand.NextDouble() * (_maxY - _minY) + _minY);
            }

            // Initialize and start the timer
            _cloudAnimationTimer = new DispatcherTimer();
            _cloudAnimationTimer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
            _cloudAnimationTimer.Tick += CloudAnimationTimer_Tick;
            _cloudAnimationTimer.Start();
        }

        private void CloudAnimationTimer_Tick(object sender, EventArgs e)
        {
            foreach (var cloudData in _cloudMovements)
            {
                cloudData.Update(_minX, _maxX, _minY, _maxY);
            }
        }

        private void StartAnimations()
        {
            // Linear gradient
            AnimateLinearGradient(LinearBrush, 3, 6);
            AnimateOpacity(LinearLayer, 0.3, 0.6);

            // Radial gradients
            AnimateRadialGradient(RadialBrush1, 4, 7);
            AnimateOpacity(RadialLayer1, 0.2, 0.5);

            AnimateRadialGradient(RadialBrush2, 5, 8);
            AnimateOpacity(RadialLayer2, 0.2, 0.5);

            // Animate cloud gradients (keeping these as XAML animations)
            AnimateRadialGradient(CloudBrush1, 8, 15);
            AnimateRadialGradient(CloudBrush2, 10, 18);
            AnimateRadialGradient(CloudBrush3, 9, 16);
            AnimateRadialGradient(CloudBrush4, 11, 19);
            AnimateRadialGradient(CloudBrush5, 7, 14);
            AnimateRadialGradient(CloudBrush6, 8, 15);

            // Animate cloud opacity with fade in/out using the configurable max opacity
            AnimateCloudOpacity(Cloud1, 0.1, _maxCloudOpacity * 0.8, 8, 15);
            AnimateCloudOpacity(Cloud2, 0.05, _maxCloudOpacity * 0.7, 10, 18);
            AnimateCloudOpacity(Cloud3, 0.15, _maxCloudOpacity * 0.9, 12, 20);
            AnimateCloudOpacity(Cloud4, 0.08, _maxCloudOpacity * 0.6, 9, 16);
            AnimateCloudOpacity(Cloud5, 0.12, _maxCloudOpacity * 0.8, 11, 19);
            AnimateCloudOpacity(Cloud6, 0.1, _maxCloudOpacity * 0.7, 7, 14);

            // Add rotation animations for clouds
            AddCloudRotationAnimations();
        }

        private void AddCloudRotationAnimations()
        {
            var clouds = new[] { Cloud1, Cloud2, Cloud3, Cloud4, Cloud5, Cloud6 };
            
            foreach (var cloud in clouds)
            {
                var rotateTransform = new RotateTransform();
                cloud.RenderTransform = rotateTransform;
                cloud.RenderTransformOrigin = new Point(0.5, 0.5);

                var rotateAnim = new DoubleAnimation
                {
                    From = -2,
                    To = 2,
                    Duration = TimeSpan.FromSeconds(_rand.NextDouble() * 12 + 15),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }
        }

        private void AnimateCloudOpacity(UIElement cloud, double minOpacity, double maxOpacity, double minDuration, double maxDuration)
        {
            var anim = new DoubleAnimation
            {
                From = minOpacity,
                To = maxOpacity,
                Duration = TimeSpan.FromSeconds(_rand.NextDouble() * (maxDuration - minDuration) + minDuration),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            cloud.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void AnimateLinearGradient(LinearGradientBrush brush, double minDuration, double maxDuration)
        {
            foreach (var stop in brush.GradientStops)
            {
                AnimateGradientStop(stop, minDuration, maxDuration);
            }

            AnimatePoint(brush as Animatable, LinearGradientBrush.StartPointProperty, minDuration, maxDuration);
            AnimatePoint(brush as Animatable, LinearGradientBrush.EndPointProperty, minDuration, maxDuration);
        }

        private void AnimateRadialGradient(RadialGradientBrush brush, double minDuration, double maxDuration)
        {
            foreach (var stop in brush.GradientStops)
            {
                AnimateGradientStop(stop, minDuration, maxDuration);
            }

            AnimatePoint(brush as Animatable, RadialGradientBrush.GradientOriginProperty, minDuration, maxDuration);
            AnimatePoint(brush as Animatable, RadialGradientBrush.CenterProperty, minDuration, maxDuration);
        }

        private void AnimateGradientStop(GradientStop stop, double minDuration, double maxDuration)
        {
            double target = _rand.NextDouble() * 0.6 + 0.2;
            var anim = new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromSeconds(_rand.NextDouble() * (maxDuration - minDuration) + minDuration),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            stop.BeginAnimation(GradientStop.OffsetProperty, anim);
        }

        private void AnimatePoint(Animatable target, DependencyProperty prop, double minDuration, double maxDuration)
        {
            var anim = new PointAnimation
            {
                To = new Point(_rand.NextDouble(), _rand.NextDouble()),
                Duration = TimeSpan.FromSeconds(_rand.NextDouble() * (maxDuration - minDuration) + minDuration),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            target.BeginAnimation(prop, anim);
        }

        private void AnimateOpacity(UIElement layer, double min, double max)
        {
            var anim = new DoubleAnimation
            {
                From = min,
                To = max,
                Duration = TimeSpan.FromSeconds(_rand.NextDouble() * 3 + 2),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            layer.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        // Public property to easily modify the maximum cloud opacity
        public double MaxCloudOpacity
        {
            get { return _maxCloudOpacity; }
            set { _maxCloudOpacity = Math.Max(0, Math.Min(1, value)); } // Clamp between 0 and 1
        }
    }

    // Helper class for managing individual cloud movement
    internal class CloudMovementData
    {
        public UIElement CloudElement { get; }
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        public double TargetX { get; private set; }
        public double TargetY { get; private set; }
        public DateTime NextDirectionChange { get; private set; }
        
        private Random _random;
        private double _speed;

        public CloudMovementData(UIElement cloudElement, Random random)
        {
            CloudElement = cloudElement;
            _random = random;
            _speed = _random.NextDouble() * 0.3 + 0.1; // Speed between 0.1 and 0.4 pixels per frame
            
            SetNewTarget();
        }

        public void Update(double minX, double maxX, double minY, double maxY)
        {
            var currentX = Canvas.GetLeft(CloudElement);
            var currentY = Canvas.GetTop(CloudElement);

            // Check if we need to change direction
            if (DateTime.Now >= NextDirectionChange || 
                Math.Abs(currentX - TargetX) < 5 && Math.Abs(currentY - TargetY) < 5)
            {
                SetNewTarget(minX, maxX, minY, maxY);
            }

            // Calculate direction to target
            var deltaX = TargetX - currentX;
            var deltaY = TargetY - currentY;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distance > 0)
            {
                // Normalize and apply speed
                VelocityX = (deltaX / distance) * _speed;
                VelocityY = (deltaY / distance) * _speed;
            }

            // Update position
            var newX = currentX + VelocityX;
            var newY = currentY + VelocityY;

            // Apply boundaries (wrap around)
            if (newX < minX) newX = maxX;
            if (newX > maxX) newX = minX;
            if (newY < minY) newY = maxY;
            if (newY > maxY) newY = minY;

            Canvas.SetLeft(CloudElement, newX);
            Canvas.SetTop(CloudElement, newY);
        }

        private void SetNewTarget(double? minX = null, double? maxX = null, double? minY = null, double? maxY = null)
        {
            // Set boundaries if provided
            var boundsMinX = minX ?? -400;
            var boundsMaxX = maxX ?? 1200;
            var boundsMinY = minY ?? -300;
            var boundsMaxY = maxY ?? 750;

            // Pick a new random target
            TargetX = _random.NextDouble() * (boundsMaxX - boundsMinX) + boundsMinX;
            TargetY = _random.NextDouble() * (boundsMaxY - boundsMinY) + boundsMinY;

            // Set next direction change time (between 5-15 seconds)
            NextDirectionChange = DateTime.Now.AddSeconds(_random.NextDouble() * 10 + 5);

            // Randomize speed slightly
            _speed = _random.NextDouble() * 0.3 + 0.1;
        }
    }
}