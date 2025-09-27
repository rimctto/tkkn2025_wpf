using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;

namespace tkkn2025.UI
{


    /// <summary>
    /// Interaction logic for SandboxWindow.xaml
    /// Provides a sandbox environment for testing and experimenting with visual elements
    /// </summary>
    public partial class SandboxWindow : Window
    {
        #region Private Fields

        private readonly List<UIElement> drawnObjects = new List<UIElement>();
        private readonly List<SpiralAndSine> activeSpirals = new List<SpiralAndSine>();
        private string currentTool = "None";
        private bool isDrawing = false;
        private Point startPoint;
        private SpiralAndSineConfig spiralConfig;
        private readonly Random random = new Random();

        // Animation timer
        private System.Windows.Threading.DispatcherTimer animationTimer;
        private bool isAnimating = false;

        #endregion

        #region Constructor

        public SandboxWindow()
        {
            InitializeComponent();
            InitializeSandbox();
        }

        #endregion

        #region Initialization

        private void InitializeSandbox()
        {
            // Initialize spiral configuration with fallback
            try
            {
                spiralConfig = SpiralAndSineConfig.LoadConfig();
            }
            catch (Exception ex)
            {
                // If loading fails completely, create a default instance
                spiralConfig = new SpiralAndSineConfig();
                UpdateStatus($"Failed to load config, using defaults: {ex.Message}");
            }

            // Ensure spiralConfig is never null
            spiralConfig ??= new SpiralAndSineConfig();

            DataContext = new { SpiralConfig = spiralConfig };

            // Initialize animation timer
            animationTimer = new System.Windows.Threading.DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;

            // Set initial canvas size display
            UpdateCanvasInfo();
            UpdateStatus("Sandbox initialized. Select a drawing tool to begin.");

            // Handle window size changes
            this.SizeChanged += SandboxWindow_SizeChanged;
        }

        private void SandboxWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCanvasInfo();
        }

        #endregion

        #region Canvas Event Handlers

        private void SandboxCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(SandboxCanvas);
                isDrawing = true;
                SandboxCanvas.CaptureMouse();

                switch (currentTool)
                {
                    case "Spiral":
                        DrawSpiralAt(startPoint);
                        break;
                    case "Text":
                        DrawParticleAdventures(startPoint);
                        break;
                    case "Circle":
                        // Circle drawing will be handled in MouseMove and MouseUp
                        break;
                    case "Line":
                        // Line drawing will be handled in MouseMove and MouseUp
                        break;
                }
            }
        }

        private void SandboxCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(SandboxCanvas);
            MousePositionText.Text = $"({currentPoint.X:F0}, {currentPoint.Y:F0})";

            if (isDrawing && e.LeftButton == MouseButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case "Line":
                        DrawPreviewLine(startPoint, currentPoint);
                        break;
                    case "Circle":
                        DrawPreviewCircle(startPoint, currentPoint);
                        break;
                }
            }
        }

        private void SandboxCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDrawing)
            {
                var endPoint = e.GetPosition(SandboxCanvas);

                switch (currentTool)
                {
                    case "Line":
                        DrawLineFromTo(startPoint, endPoint);
                        break;
                    case "Circle":
                        DrawCircleFromTo(startPoint, endPoint);
                        break;
                }

                isDrawing = false;
                SandboxCanvas.ReleaseMouseCapture();
                RemovePreviewElements();
            }
        }

        #endregion

        #region Drawing Tools

        private void DrawSpiralAt(Point position)
        {
            try
            {
                // Create spiral mechanic at the clicked position
                var spiralPosition = new Vector2((float)position.X, (float)position.Y);
                var spiral = new SpiralAndSine(spiralPosition, 1, spiralConfig.ParticleCount, spiralConfig);

                // Since we don't have the full game infrastructure, we'll create a visual representation
                DrawSpiralVisualization(position);

                UpdateStatus($"Spiral drawn at ({position.X:F0}, {position.Y:F0})");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error drawing spiral: {ex.Message}");
            }
        }

        private void DrawParticleAdventures(Point startPosition)
        {
            try
            {
                var textGroup = new Canvas();
                double letterSpacing = 60;
                double wordSpacing = 120;
                double currentX = startPosition.X;
                double currentY = startPosition.Y;

                // Draw "PARTICLE"
                string word1 = "PARTICLE";
                foreach (char c in word1)
                {
                    var letter = CreateCartoonLetter(c, new Point(currentX, currentY));
                    if (letter != null)
                    {
                        textGroup.Children.Add(letter);
                    }
                    currentX += letterSpacing;
                }

                // Move to next word position
                currentX += wordSpacing;

                // Draw "ADVENTURES"
                string word2 = "ADVENTURES";
                foreach (char c in word2)
                {
                    var letter = CreateCartoonLetter(c, new Point(currentX, currentY));
                    if (letter != null)
                    {
                        textGroup.Children.Add(letter);
                    }
                    currentX += letterSpacing;
                }

                SandboxCanvas.Children.Add(textGroup);
                drawnObjects.Add(textGroup);
                UpdateObjectCount();
                UpdateStatus("Particle Adventures text drawn!");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error drawing text: {ex.Message}");
            }
        }

        private Polygon? CreateCartoonLetter(char letter, Point position)
        {
            var points = new PointCollection();
            var polygon = new Polygon
            {
                Fill = Brushes.Orange,
                Stroke = Brushes.DarkOrange,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };

            double x = position.X;
            double y = position.Y;
            double size = 50; // Base size for letters

            switch (char.ToUpper(letter))
            {
                case 'P':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x, y + size));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x + size * 0.7, y + size * 0.6));
                    points.Add(new Point(x + size * 0.3, y + size * 0.6));
                    points.Add(new Point(x + size * 0.3, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y));
                    break;

                case 'A':
                    points.Add(new Point(x + size * 0.5, y));
                    points.Add(new Point(x + size * 0.8, y + size));
                    points.Add(new Point(x + size * 0.6, y + size));
                    points.Add(new Point(x + size * 0.55, y + size * 0.7));
                    points.Add(new Point(x + size * 0.45, y + size * 0.7));
                    points.Add(new Point(x + size * 0.4, y + size));
                    points.Add(new Point(x + size * 0.2, y + size));
                    break;

                case 'R':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x, y + size));
                    points.Add(new Point(x + size * 0.3, y + size));
                    points.Add(new Point(x + size * 0.3, y + size * 0.6));
                    points.Add(new Point(x + size * 0.5, y + size));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x + size * 0.5, y + size * 0.5));
                    points.Add(new Point(x + size * 0.6, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y));
                    break;

                case 'T':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.8, y));
                    points.Add(new Point(x + size * 0.8, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y + size));
                    points.Add(new Point(x + size * 0.4, y + size));
                    points.Add(new Point(x + size * 0.4, y + size * 0.3));
                    points.Add(new Point(x + size * 0.2, y + size * 0.3));
                    break;

                case 'I':
                    points.Add(new Point(x + size * 0.2, y));
                    points.Add(new Point(x + size * 0.8, y));
                    points.Add(new Point(x + size * 0.8, y + size * 0.2));
                    points.Add(new Point(x + size * 0.6, y + size * 0.2));
                    points.Add(new Point(x + size * 0.6, y + size * 0.8));
                    points.Add(new Point(x + size * 0.8, y + size * 0.8));
                    points.Add(new Point(x + size * 0.8, y + size));
                    points.Add(new Point(x + size * 0.2, y + size));
                    points.Add(new Point(x + size * 0.2, y + size * 0.8));
                    points.Add(new Point(x + size * 0.4, y + size * 0.8));
                    points.Add(new Point(x + size * 0.4, y + size * 0.2));
                    points.Add(new Point(x + size * 0.2, y + size * 0.2));
                    break;

                case 'C':
                    points.Add(new Point(x + size * 0.7, y + size * 0.1));
                    points.Add(new Point(x + size * 0.3, y + size * 0.1));
                    points.Add(new Point(x + size * 0.1, y + size * 0.3));
                    points.Add(new Point(x + size * 0.1, y + size * 0.7));
                    points.Add(new Point(x + size * 0.3, y + size * 0.9));
                    points.Add(new Point(x + size * 0.7, y + size * 0.9));
                    points.Add(new Point(x + size * 0.7, y + size * 0.7));
                    points.Add(new Point(x + size * 0.4, y + size * 0.7));
                    points.Add(new Point(x + size * 0.3, y + size * 0.6));
                    points.Add(new Point(x + size * 0.3, y + size * 0.4));
                    points.Add(new Point(x + size * 0.4, y + size * 0.3));
                    points.Add(new Point(x + size * 0.7, y + size * 0.3));
                    break;

                case 'L':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.3, y));
                    points.Add(new Point(x + size * 0.3, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x, y + size));
                    break;

                case 'E':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.7, y));
                    points.Add(new Point(x + size * 0.7, y + size * 0.2));
                    points.Add(new Point(x + size * 0.3, y + size * 0.2));
                    points.Add(new Point(x + size * 0.3, y + size * 0.4));
                    points.Add(new Point(x + size * 0.6, y + size * 0.4));
                    points.Add(new Point(x + size * 0.6, y + size * 0.6));
                    points.Add(new Point(x + size * 0.3, y + size * 0.6));
                    points.Add(new Point(x + size * 0.3, y + size * 0.8));
                    points.Add(new Point(x + size * 0.7, y + size * 0.8));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x, y + size));
                    break;

                case 'D':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.5, y));
                    points.Add(new Point(x + size * 0.7, y + size * 0.2));
                    points.Add(new Point(x + size * 0.7, y + size * 0.8));
                    points.Add(new Point(x + size * 0.5, y + size));
                    points.Add(new Point(x, y + size));
                    points.Add(new Point(x, y + size * 0.7));
                    points.Add(new Point(x + size * 0.3, y + size * 0.7));
                    points.Add(new Point(x + size * 0.4, y + size * 0.6));
                    points.Add(new Point(x + size * 0.4, y + size * 0.4));
                    points.Add(new Point(x + size * 0.3, y + size * 0.3));
                    points.Add(new Point(x, y + size * 0.3));
                    break;

                case 'V':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.2, y));
                    points.Add(new Point(x + size * 0.5, y + size * 0.7));
                    points.Add(new Point(x + size * 0.8, y));
                    points.Add(new Point(x + size, y));
                    points.Add(new Point(x + size * 0.6, y + size));
                    points.Add(new Point(x + size * 0.4, y + size));
                    break;

                case 'N':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.3, y));
                    points.Add(new Point(x + size * 0.3, y + size * 0.4));
                    points.Add(new Point(x + size * 0.5, y + size * 0.2));
                    points.Add(new Point(x + size * 0.7, y));
                    points.Add(new Point(x + size, y));
                    points.Add(new Point(x + size, y + size));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x + size * 0.7, y + size * 0.6));
                    points.Add(new Point(x + size * 0.5, y + size * 0.8));
                    points.Add(new Point(x + size * 0.3, y + size));
                    points.Add(new Point(x, y + size));
                    break;

                case 'U':
                    points.Add(new Point(x, y));
                    points.Add(new Point(x + size * 0.3, y));
                    points.Add(new Point(x + size * 0.3, y + size * 0.7));
                    points.Add(new Point(x + size * 0.4, y + size * 0.8));
                    points.Add(new Point(x + size * 0.6, y + size * 0.8));
                    points.Add(new Point(x + size * 0.7, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y));
                    points.Add(new Point(x + size, y));
                    points.Add(new Point(x + size, y + size * 0.8));
                    points.Add(new Point(x + size * 0.7, y + size));
                    points.Add(new Point(x + size * 0.3, y + size));
                    points.Add(new Point(x, y + size * 0.8));
                    break;

                case 'S':
                    points.Add(new Point(x + size * 0.8, y + size * 0.2));
                    points.Add(new Point(x + size * 0.3, y + size * 0.2));
                    points.Add(new Point(x + size * 0.2, y + size * 0.3));
                    points.Add(new Point(x + size * 0.2, y + size * 0.4));
                    points.Add(new Point(x + size * 0.3, y + size * 0.5));
                    points.Add(new Point(x + size * 0.7, y + size * 0.5));
                    points.Add(new Point(x + size * 0.8, y + size * 0.6));
                    points.Add(new Point(x + size * 0.8, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size * 0.8));
                    points.Add(new Point(x + size * 0.2, y + size * 0.8));
                    points.Add(new Point(x + size * 0.2, y + size));
                    points.Add(new Point(x + size * 0.8, y + size));
                    points.Add(new Point(x + size * 0.8, y + size * 0.9));
                    points.Add(new Point(x + size * 0.1, y + size * 0.9));
                    points.Add(new Point(x + size * 0.1, y + size * 0.1));
                    points.Add(new Point(x + size * 0.8, y + size * 0.1));
                    break;

                case 'O':
                    points.Add(new Point(x + size * 0.3, y + size * 0.1));
                    points.Add(new Point(x + size * 0.7, y + size * 0.1));
                    points.Add(new Point(x + size * 0.9, y + size * 0.3));
                    points.Add(new Point(x + size * 0.9, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size * 0.9));
                    points.Add(new Point(x + size * 0.3, y + size * 0.9));
                    points.Add(new Point(x + size * 0.1, y + size * 0.7));
                    points.Add(new Point(x + size * 0.1, y + size * 0.3));
                    points.Add(new Point(x + size * 0.3, y + size * 0.1));
                    // Inner hole
                    points.Add(new Point(x + size * 0.3, y + size * 0.3));
                    points.Add(new Point(x + size * 0.3, y + size * 0.7));
                    points.Add(new Point(x + size * 0.4, y + size * 0.7));
                    points.Add(new Point(x + size * 0.6, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size * 0.7));
                    points.Add(new Point(x + size * 0.7, y + size * 0.3));
                    points.Add(new Point(x + size * 0.6, y + size * 0.3));
                    points.Add(new Point(x + size * 0.4, y + size * 0.3));
                    break;

                default:
                    return null; // Skip unknown characters
            }

            polygon.Points = points;
            return polygon;
        }

        private void DrawSpiralVisualization(Point center)
        {
            var spiralGroup = new Canvas();

            // Draw spiral particles
            for (int i = 0; i < spiralConfig.ParticleCount; i++)
            {
                var calculatedRadius = i * spiralConfig.Radius * 0.01;
                var angle = Math.PI * spiralConfig.ParticleSpacing * 10 * i;

                var x = Math.Sin(angle) * calculatedRadius;
                var y = Math.Cos(angle) * calculatedRadius;

                var particle = new Ellipse
                {
                    Width = spiralConfig.Size,
                    Height = spiralConfig.Size,
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.LightBlue,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(particle, center.X + x - spiralConfig.Size / 2);
                Canvas.SetTop(particle, center.Y + y - spiralConfig.Size / 2);

                spiralGroup.Children.Add(particle);
            }

            // Draw sine wave particles
            for (int i = 0; i < spiralConfig.ParticleCount; i++)
            {
                var x = i * spiralConfig.ParticleSpacing * 1000;
                var y = spiralConfig.Amplitude * -Math.Sin(i * spiralConfig.Frequency * 100);

                var particle = new Ellipse
                {
                    Width = spiralConfig.Size,
                    Height = spiralConfig.Size,
                    Fill = Brushes.MediumPurple,
                    Stroke = Brushes.MediumPurple,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(particle, center.X + x - spiralConfig.Size / 2);
                Canvas.SetTop(particle, center.Y + y - spiralConfig.Size / 2);

                spiralGroup.Children.Add(particle);
            }

            SandboxCanvas.Children.Add(spiralGroup);
            drawnObjects.Add(spiralGroup);
            UpdateObjectCount();
        }

        private void DrawLineFromTo(Point start, Point end)
        {
            var line = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };

            SandboxCanvas.Children.Add(line);
            drawnObjects.Add(line);
            UpdateObjectCount();
            UpdateStatus($"Line drawn from ({start.X:F0}, {start.Y:F0}) to ({end.X:F0}, {end.Y:F0})");
        }

        private void DrawCircleFromTo(Point center, Point edge)
        {
            var radius = Math.Sqrt(Math.Pow(edge.X - center.X, 2) + Math.Pow(edge.Y - center.Y, 2));

            var circle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Canvas.SetLeft(circle, center.X - radius);
            Canvas.SetTop(circle, center.Y - radius);

            SandboxCanvas.Children.Add(circle);
            drawnObjects.Add(circle);
            UpdateObjectCount();
            UpdateStatus($"Circle drawn at ({center.X:F0}, {center.Y:F0}) with radius {radius:F0}");
        }

        private void DrawPreviewLine(Point start, Point current)
        {
            RemovePreviewElements();

            var previewLine = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = current.X,
                Y2 = current.Y,
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 5 },
                Tag = "Preview"
            };

            SandboxCanvas.Children.Add(previewLine);
        }

        private void DrawPreviewCircle(Point center, Point current)
        {
            RemovePreviewElements();

            var radius = Math.Sqrt(Math.Pow(current.X - center.X, 2) + Math.Pow(current.Y - center.Y, 2));

            var previewCircle = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Stroke = Brushes.Gray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 5, 5 },
                Fill = Brushes.Transparent,
                Tag = "Preview"
            };

            Canvas.SetLeft(previewCircle, center.X - radius);
            Canvas.SetTop(previewCircle, center.Y - radius);

            SandboxCanvas.Children.Add(previewCircle);
        }

        private void RemovePreviewElements()
        {
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement element in SandboxCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "Preview")
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (var element in elementsToRemove)
            {
                SandboxCanvas.Children.Remove(element);
            }
        }

        private void DrawParticleAdventuresText()
        {
            var canvas = SandboxCanvas;
            var centerX = canvas.ActualWidth / 2;
            var startY = 50; // Starting Y position for the text

            // Clear any existing text first
            ClearParticleAdventuresText();

            // Draw "PARTICLE" on the first line
            DrawParticleText(centerX - 200, startY);

            // Draw "ADVENTURES" on the second line
            DrawAdventuresText(centerX - 250, startY + 100);
        }

        private void ClearParticleAdventuresText()
        {
            var elementsToRemove = new List<UIElement>();
            foreach (UIElement element in SandboxCanvas.Children)
            {
                if (element is FrameworkElement fe && fe.Tag?.ToString() == "ParticleAdventuresText")
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (var element in elementsToRemove)
            {
                SandboxCanvas.Children.Remove(element);
                drawnObjects.Remove(element);
            }
        }

        private void DrawParticleText(double startX, double startY)
        {
            var letterSpacing = 55;
            var currentX = startX;

            // P
            DrawLetterP(currentX, startY);
            currentX += letterSpacing;

            // A
            DrawLetterA(currentX, startY);
            currentX += letterSpacing;

            // R
            DrawLetterR(currentX, startY);
            currentX += letterSpacing;

            // T
            DrawLetterT(currentX, startY);
            currentX += letterSpacing;

            // I
            DrawLetterI(currentX, startY);
            currentX += letterSpacing;

            // C
            DrawLetterC(currentX, startY);
            currentX += letterSpacing;

            // L
            DrawLetterL(currentX, startY);
            currentX += letterSpacing;

            // E
            DrawLetterE(currentX, startY);
        }

        private void DrawAdventuresText(double startX, double startY)
        {
            var letterSpacing = 45;
            var currentX = startX;

            // A
            DrawLetterA(currentX, startY);
            currentX += letterSpacing;

            // D
            DrawLetterD(currentX, startY);
            currentX += letterSpacing;

            // V
            DrawLetterV(currentX, startY);
            currentX += letterSpacing;

            // E
            DrawLetterE(currentX, startY);
            currentX += letterSpacing;

            // N
            DrawLetterN(currentX, startY);
            currentX += letterSpacing;

            // T
            DrawLetterT(currentX, startY);
            currentX += letterSpacing;

            // U
            DrawLetterU(currentX, startY);
            currentX += letterSpacing;

            // R
            DrawLetterR(currentX, startY);
            currentX += letterSpacing;

            // E
            DrawLetterE(currentX, startY);
            currentX += letterSpacing;

            // S
            DrawLetterS(currentX, startY);
        }

        // Letter drawing methods with cartoony effects
        private void DrawLetterP(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Cyan),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x + 25, y));
            polyline.Points.Add(new Point(x + 35, y + 10));
            polyline.Points.Add(new Point(x + 35, y + 20));
            polyline.Points.Add(new Point(x + 25, y + 30));
            polyline.Points.Add(new Point(x, y + 30));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterA(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Orange),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 165, 0)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x + 17.5, y));
            polyline.Points.Add(new Point(x + 35, y + 60));
            polyline.Points.Add(new Point(x + 25, y + 35));
            polyline.Points.Add(new Point(x + 10, y + 35));
            polyline.Points.Add(new Point(x, y + 60));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterR(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x + 25, y));
            polyline.Points.Add(new Point(x + 35, y + 10));
            polyline.Points.Add(new Point(x + 35, y + 20));
            polyline.Points.Add(new Point(x + 25, y + 30));
            polyline.Points.Add(new Point(x, y + 30));
            polyline.Points.Add(new Point(x + 35, y + 60));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterT(double x, double y)
        {
            var polyline1 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };
            polyline1.Points.Add(new Point(x, y));
            polyline1.Points.Add(new Point(x + 35, y));

            var polyline2 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };
            polyline2.Points.Add(new Point(x + 17.5, y));
            polyline2.Points.Add(new Point(x + 17.5, y + 60));

            SandboxCanvas.Children.Add(polyline1);
            SandboxCanvas.Children.Add(polyline2);
            drawnObjects.Add(polyline1);
            drawnObjects.Add(polyline2);
        }

        private void DrawLetterI(double x, double y)
        {
            var polyline1 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Lime),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };
            polyline1.Points.Add(new Point(x, y));
            polyline1.Points.Add(new Point(x + 30, y));

            var polyline2 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Lime),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };
            polyline2.Points.Add(new Point(x + 15, y));
            polyline2.Points.Add(new Point(x + 15, y + 60));

            var polyline3 = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Lime),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };
            polyline3.Points.Add(new Point(x, y + 60));
            polyline3.Points.Add(new Point(x + 30, y + 60));

            SandboxCanvas.Children.Add(polyline1);
            SandboxCanvas.Children.Add(polyline2);
            SandboxCanvas.Children.Add(polyline3);
            drawnObjects.Add(polyline1);
            drawnObjects.Add(polyline2);
            drawnObjects.Add(polyline3);
        }

        private void DrawLetterC(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Pink),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 255, 192, 203)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x + 35, y + 10));
            polyline.Points.Add(new Point(x + 10, y));
            polyline.Points.Add(new Point(x, y + 15));
            polyline.Points.Add(new Point(x, y + 45));
            polyline.Points.Add(new Point(x + 10, y + 60));
            polyline.Points.Add(new Point(x + 35, y + 50));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterL(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.LightBlue),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x + 30, y + 60));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterE(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x + 30, y));
            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x, y + 30));
            polyline.Points.Add(new Point(x + 20, y + 30));
            polyline.Points.Add(new Point(x, y + 30));
            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x + 30, y + 60));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterD(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Purple),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 128, 0, 128)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x + 20, y));
            polyline.Points.Add(new Point(x + 35, y + 15));
            polyline.Points.Add(new Point(x + 35, y + 45));
            polyline.Points.Add(new Point(x + 20, y + 60));
            polyline.Points.Add(new Point(x, y + 60));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterV(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Green),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x + 17.5, y + 60));
            polyline.Points.Add(new Point(x + 35, y));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterN(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Gold),
                StrokeThickness = 4,
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y + 60));
            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x + 35, y + 60));
            polyline.Points.Add(new Point(x + 35, y));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterU(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Violet),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 238, 130, 238)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x, y));
            polyline.Points.Add(new Point(x, y + 45));
            polyline.Points.Add(new Point(x + 10, y + 60));
            polyline.Points.Add(new Point(x + 25, y + 60));
            polyline.Points.Add(new Point(x + 35, y + 45));
            polyline.Points.Add(new Point(x + 35, y));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        private void DrawLetterS(double x, double y)
        {
            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(Colors.Turquoise),
                StrokeThickness = 4,
                Fill = new SolidColorBrush(Color.FromArgb(100, 64, 224, 208)),
                Tag = "ParticleAdventuresText"
            };

            polyline.Points.Add(new Point(x + 35, y + 10));
            polyline.Points.Add(new Point(x + 10, y));
            polyline.Points.Add(new Point(x, y + 10));
            polyline.Points.Add(new Point(x + 10, y + 25));
            polyline.Points.Add(new Point(x + 25, y + 35));
            polyline.Points.Add(new Point(x + 35, y + 50));
            polyline.Points.Add(new Point(x + 25, y + 60));
            polyline.Points.Add(new Point(x, y + 50));

            SandboxCanvas.Children.Add(polyline);
            drawnObjects.Add(polyline);
        }

        #endregion

        #region Tool Selection Handlers

        private void DrawSpiralButton_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "Spiral";
            UpdateCurrentToolDisplay();
            UpdateStatus("Spiral tool selected. Click on the canvas to draw spirals.");
        }

        private void DrawTextButton_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "Text";
            UpdateCurrentToolDisplay();
            UpdateStatus("Text tool selected. Click on the canvas to draw 'Particle Adventures'.");
        }

        private void DrawCircleButton_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "Circle";
            UpdateCurrentToolDisplay();
            UpdateStatus("Circle tool selected. Click and drag to draw circles.");
        }

        private void DrawLineButton_Click(object sender, RoutedEventArgs e)
        {
            currentTool = "Line";
            UpdateCurrentToolDisplay();
            UpdateStatus("Line tool selected. Click and drag to draw lines.");
        }

        private void ClearCanvasButton_Click(object sender, RoutedEventArgs e)
        {
            SandboxCanvas.Children.Clear();
            drawnObjects.Clear();
            activeSpirals.Clear();
            UpdateObjectCount();
            UpdateStatus("Canvas cleared.");
        }

        #endregion

        #region Configuration Handlers

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spiralConfig.SaveConfig();
                UpdateStatus("Spiral configuration saved successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error saving configuration: {ex.Message}");
            }
        }

        private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spiralConfig = SpiralAndSineConfig.LoadConfig();
                DataContext = new { SpiralConfig = spiralConfig };
                UpdateStatus("Spiral configuration loaded successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error loading configuration: {ex.Message}");
            }
        }

        #endregion

        #region Slider Event Handlers

        private void RadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.Radius = e.NewValue;
            }
        }

        private void ParticleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.ParticleCount = (int)e.NewValue;
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.Speed = e.NewValue;
            }
        }

        private void AmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.Amplitude = e.NewValue;
            }
        }

        private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.Frequency = e.NewValue;
            }
        }

        private void ParticleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (spiralConfig != null)
            {
                spiralConfig.Size = (int)e.NewValue;
                spiralConfig.Size = (int)e.NewValue;
            }
        }

        #endregion

        #region Animation Control

        private void StartAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isAnimating)
            {
                animationTimer.Start();
                isAnimating = true;
                UpdateStatus("Animation started.");
            }
        }

        private void StopAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            if (isAnimating)
            {
                animationTimer.Stop();
                isAnimating = false;
                UpdateStatus("Animation stopped.");
            }
        }

        private void ResetAnimationButton_Click(object sender, RoutedEventArgs e)
        {
            animationTimer.Stop();
            isAnimating = false;
            // Reset any animated properties here
            UpdateStatus("Animation reset.");
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Animation logic would go here
            // For now, this is a placeholder for future animation features
        }

        #endregion

        #region Window Event Handlers

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    currentTool = "None";
                    UpdateCurrentToolDisplay();
                    UpdateStatus("Tool selection cleared.");
                    break;
                case Key.Delete:
                    ClearCanvasButton_Click(sender, new RoutedEventArgs());
                    break;
                case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                    SaveConfigButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case Key.L when Keyboard.Modifiers == ModifierKeys.Control:
                    LoadConfigButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            // Handle key up events if needed
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Helper Methods

        private void UpdateCurrentToolDisplay()
        {
            CurrentToolText.Text = $"Tool: {currentTool}";
        }

        private void UpdateCanvasInfo()
        {
            if (SandboxCanvas != null && CanvasSizeText != null)
            {
                CanvasSizeText.Text = $"{SandboxCanvas.ActualWidth:F0} x {SandboxCanvas.ActualHeight:F0}";
            }
        }

        private void UpdateObjectCount()
        {
            if (ObjectCountText != null)
            {
                ObjectCountText.Text = drawnObjects.Count.ToString();
            }
        }

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;

                // Clear status after 5 seconds
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (StatusText != null)
                    {
                        StatusText.Text = "Ready";
                    }
                };
                timer.Start();
            }
        }

        #endregion
    }
}
