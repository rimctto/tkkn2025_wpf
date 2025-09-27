using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using tkkn2025.Core.Behaviors;
using tkkn2025.Core.Entities.Ship;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;
using tkkn2025.Settings;

namespace tkkn2025.Core.GameModes.MazeMode
{
    class MazeGenerator
    {

        private static float particleSpeed_Normal = (float)GameSettings.ParticleSpeed;

        public static List<IParticleMechanic> GenerateMaze_Medium(Ship ship, double canvasWidth, double canvasHeight)
        {
            var mechanics = new List<IParticleMechanic>();
            var random = new Random();

            // Series tracking
            var series1Lines = new List<CustomLineMechanic>();
            var series2Lines = new List<CustomLineMechanic>();

            // Generate two parallel series of custom lines
            const int numberOfSegments = 200; // Number of line segments per series
            const double minXDistance_Normal = 0.15; // Minimum X distance in pixels
            const double minYDifference = 0.35; // Minimum difference between y1 and y2

            // Starting positions for both series (ensure minimum distance)
            double series1x1 = 0.05;
            double series2x1 = 0.95;


            double y1 = 0.001;

            double series1x2 = 0.35; //GenerateNextX(series1x1, random, 0.1, 0.9);
            double series2x2 = 0.75; // Math.Min(0.99, series1x1 + Math.Max(minXDistance, random.NextDouble()));

            double series1y2 = .86; // series1Y2 = currentY1 - minYDifference + random.NextDouble() * minYDifference;
            double series2y2 = series1y2;

            // Generate levels for spiral placement (every 3 levels starting from level 3)
            var spiralLevels = new HashSet<int>();
            for (int level = 2; level < numberOfSegments; level += 3)
            {
                spiralLevels.Add(level);
            }

            for (int i = 0; i < numberOfSegments; i++)
            {
                if (i > 0)
                {
                    series1x2 = GenerateXForLine1(series1x1, random, 0.01, 0.99 - minXDistance_Normal);
                    series2x2 = GenerateXforLine2(random, minXDistance_Normal, series1x1, series1x2);
                }

                // Calculate Y coordinate with conditional logic based on X distance
                double xDistance = Math.Abs(series1x2 - series1x1);
                if (xDistance > 0.75)
                {
                    // If X distance is more than 0.5, ensure Y2 is more than 0.5
                    series1y2 = Math.Max(0.85, random.NextDouble() * (1 - minYDifference) + minYDifference);
                }
                else if (xDistance > 0.65)
                {
                    // If X distance is more than 0.5, ensure Y2 is more than 0.5
                    series1y2 = Math.Max(0.75, random.NextDouble() * (1 - minYDifference) + minYDifference);
                }
                else if (xDistance > 0.5)
                {
                    // If X distance is more than 0.5, ensure Y2 is more than 0.5
                    series1y2 = Math.Max(0.5, random.NextDouble() * (1 - minYDifference) + minYDifference);
                }
                else
                {
                    // Otherwise, use the original calculation
                    series1y2 = random.NextDouble() * (1 - minYDifference) + minYDifference;
                }

                series2y2 = series1y2;


                // Create particles count based on line length
                int particleCount = CalculateParticleCount(series1x1, y1, series1x2, series1y2);

                var seriesColor = i % 2 == 0 ? Brushes.MediumPurple : Brushes.LightBlue;

                // Create Series 1 line
                var series1Line = new CustomLineMechanic(
                    activationLevel: 0, // Not level-activated
                    particleCount: particleCount,
                    particleSpeed: particleSpeed_Normal,
                    x1Percent: series1x1,
                    y1Percent: y1,
                    x2Percent: series1x2,
                    y2Percent: series1y2,
                    color: seriesColor,
                    firstLine: i == 0,
                    previousLine: i > 0 ? series1Lines[i - 1] : null
                );

                // Create Series 2 line
                var series2Line = new CustomLineMechanic(
                    activationLevel: 0, // Not level-activated
                    particleCount: particleCount,
                    particleSpeed: particleSpeed_Normal,
                    x1Percent: series2x1,
                    y1Percent: y1,
                    x2Percent: series2x2,
                    y2Percent: series2y2,
                    color: seriesColor,
                    firstLine: i == 0,
                    previousLine: i > 0 ? series2Lines[i - 1] : null
                );

                series1Lines.Add(series1Line);
                series2Lines.Add(series2Line);
                mechanics.Add(series1Line);
                mechanics.Add(series2Line);

                // Add spiral mechanic every 3 levels using SpiralMechanicExample
                if (spiralLevels.Contains(i) || false)
                {
                    SineWave sineWave = null;

                    var newPosition = new Vector2((float)random.NextDouble() * (float)canvasWidth, (float)random.NextDouble() * (float)canvasHeight);


                    sineWave = new SineWave(
                        position: newPosition,
                        activationLevel: i,
                        particleCount: 25
                    );
                    sineWave.Behaviors.Add(new ChaseBehavior(sineWave, ship, (float)GameSettings.ParticleTurnSpeed, (float)GameSettings.ParticleSpeed));

                    if (sineWave != null)
                    {
                        mechanics.Add(sineWave);
                    }
                }

                // Update positions for next iteration
                series1x1 = series1x2;
                series2x1 = series2x2;


                System.Diagnostics.Debug.WriteLine($"?? Generated line segment {i}: Series1({series1x1:P1},{y1:P1}→{series1y2:P1}) Series2({series2x1:P1},{y1:P1}→{series2y2:P1}) XDist:{xDistance:F2}");
            }

            // Set up activation chains after all lines are created
            for (int i = 0; i < numberOfSegments - 1; i++)
            {
                // Capture the current index in local variables to avoid closure issues
                int currentIndex = i;
                int nextIndex = i + 1;

                // Series 1 activation chain
                var currentSeries1 = series1Lines[currentIndex];
                var nextSeries1 = series1Lines[nextIndex];
                currentSeries1.NextLineActivated += (sender, current) => {
                    System.Diagnostics.Debug.WriteLine($"?? Chaining activation: Series1 line {currentIndex} triggering line {nextIndex}");
                    nextSeries1.Activate();
                };

                // Series 2 activation chain
                var currentSeries2 = series2Lines[currentIndex];
                var nextSeries2 = series2Lines[nextIndex];
                currentSeries2.NextLineActivated += (sender, current) => {
                    System.Diagnostics.Debug.WriteLine($"?? Chaining activation: Series2 line {currentIndex} triggering line {nextIndex}");
                    nextSeries2.Activate();
                };
            }

            int spiralCount = spiralLevels.Count;
            System.Diagnostics.Debug.WriteLine($"?? Generated {numberOfSegments * 2} custom line mechanics in two parallel series with {spiralCount} spiral mechanics (every 3 levels) using SpiralMechanicExample");

            return mechanics;
        }




        /// <summary>
        /// Generate next X coordinate with some randomness but within bounds
        /// </summary>
        private static double GenerateXForLine1(double currentX, Random random, double minBound, double maxBound)
        {
            double variation = (random.NextDouble() - 0.5) * 1.5; // ±10% variation
            double nextX = currentX + variation;

            if (Math.Abs(nextX - currentX) < 0.35)
            {
                nextX = GenerateXForLine1(currentX, random, minBound, maxBound);
            }
            return Math.Clamp(nextX, minBound, maxBound);
        }

        private static double GenerateXforLine2(Random random, double minXDistance_Normal, double series1x1, double series1x2)
        {
            double series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal, random.NextDouble()));
            if (Math.Abs(series1x1 - series1x2) > 0.85)
            {
                series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal + 0.25, random.NextDouble()));
            }
            else if (Math.Abs(series1x1 - series1x2) > 0.75)
            {
                series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal + 0.15, random.NextDouble()));
            }
            else if (Math.Abs(series1x1 - series1x2) > 0.65)
            {
                series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal + 0.1, random.NextDouble()));
            }

            return series2x2;
        }


        /// <summary>
        /// Calculate particle count based on line length
        /// </summary>
        private static int CalculateParticleCount(double x1, double y1, double x2, double y2)
        {
            double length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            int baseCount = (int)(length * 50); // Base particles per unit length
            return Math.Max(10, Math.Min(30, baseCount)); // Clamp between 10-30 particles
        }

    }
}
