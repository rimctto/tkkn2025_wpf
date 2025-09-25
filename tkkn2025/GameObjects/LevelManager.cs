using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using tkkn2025.GameObjects.LevelMechanics;
using tkkn2025.Settings;

namespace tkkn2025.GameObjects
{
    /// <summary>
    /// Manages game levels and triggers level-specific mechanics
    /// </summary>
    public class LevelManager
    {


        #region Static Canvas Variables (initialized once per game)
 
        protected static Canvas? gameCanvas;

        protected static double canvasWidth;
        protected static double canvasHeight;
        protected static Point centerScreen;

        #endregion

        #region Static Speed Management for Shared Reference

        /// <summary>
        /// Current level speed that all mechanics should use for new particles
        /// This is updated each level and shared across all mechanics
        /// </summary>
        public static double CurrentLevelSpeed { get; private set; } = GameSettings.InitialLevelSpeed.Value;
        
        /// <summary>
        /// Target speed we're ramping toward
        /// </summary>
        private static double targetLevelSpeed = GameSettings.InitialLevelSpeed.Value;

        /// <summary>
        /// Speed increase rate per second for smooth ramping
        /// /// </summary>
        private static double speedRampRate = 5.0; // Speed units per second

        /// <summary>
        /// Update the current level speed for all mechanics to use
        /// Now implements smooth speed ramping instead of instant changes
        /// </summary>
        /// <param name="level">Current level</param>
        public static void UpdateCurrentLevelSpeed(int level)
        {
            double newTargetSpeed = GameSettings.InitialLevelSpeed.Value + (level * GameSettings.SpeedIncreasePerLevel.Value);
            targetLevelSpeed = newTargetSpeed;
        }

        /// <summary>
        /// Update the speed ramping system - call this each frame
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private static void UpdateSpeedRamping(double deltaTime)
        {
            if (Math.Abs(CurrentLevelSpeed - targetLevelSpeed) > 0.1)
            {
                double speedDifference = targetLevelSpeed - CurrentLevelSpeed;
                double speedChange = Math.Sign(speedDifference) * speedRampRate * deltaTime;
                
                // Don't overshoot the target
                if (Math.Abs(speedChange) > Math.Abs(speedDifference))
                {
                    CurrentLevelSpeed = targetLevelSpeed;
                }
                else
                {
                    CurrentLevelSpeed += speedChange;
                }
            }
        }

        /// <summary>
        /// Frame counter for debug logging
        /// </summary>
        private static int frameCounter = 0;

        #endregion

        private int currentLevel;
        private DateTime gameStartTime;
        private DateTime lastLevelUpTime;
        private double levelDuration;


        private readonly List<IParticleMechanics> allMechanics = new List<IParticleMechanics>();
        private readonly List<IParticleMechanics> sequenceMechanics = new List<IParticleMechanics>();
        private readonly List<IParticleMechanics> levelMechanics = new List<IParticleMechanics>();

        public int CurrentLevel => currentLevel;

        public event EventHandler<int>? LevelChanged;
        public event EventHandler<string>? LevelMechanicTriggered;

        public LevelManager()
        {
            sequenceMechanics = new List<IParticleMechanics>();
        }
        public LevelManager(List<IParticleMechanics> sequenceMechanics, Canvas gameCanvas)
        {
            CurrentLevelSpeed = GameSettings.InitialLevelSpeed.Value;

            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);

            this.sequenceMechanics = sequenceMechanics ?? throw new ArgumentNullException(nameof(sequenceMechanics));
          
            this.allMechanics.AddRange(this.sequenceMechanics);
            this.allMechanics.AddRange(this.levelMechanics);

            Reset(gameCanvas);
        }

        public void ActivateMechanics()
        {
            if (GameSettings.LevelMechanicsEnabled)
            {
                sequenceMechanics[0].Activate();
                sequenceMechanics[1].Activate(); 
            }
        }
      
        public static List<IParticleMechanics> GenerateMaze_Medium()
        {
            var mechanics = new List<IParticleMechanics>();
            var random = new Random();
            
            // Series tracking
            var series1Lines = new List<CustomLineMechanic>();
            var series2Lines = new List<CustomLineMechanic>();
            
            // Generate two parallel series of custom lines
            const int numberOfSegments = 200; // Number of line segments per series
            const double minXDistance_Normal = 0.15; // Minimum X distance in pixels
            const double minYDifference = 0.35; // Minimum difference between y1 and y2

            // Starting positions for both series (ensure minimum distance)
            double series1x1 = 0.1;
            double series2x1 = 0.9;
                        

            double y1 = 0.001;

            double series1x2 = 0.45; //GenerateNextX(series1x1, random, 0.1, 0.9);
            double series2x2 = 0.75; // Math.Min(0.99, series1x1 + Math.Max(minXDistance, random.NextDouble()));

            double series1y2 = .86; // series1Y2 = currentY1 - minYDifference + random.NextDouble() * minYDifference;
            double series2y2 = series1y2;


            for (int i = 0; i < numberOfSegments; i++)
            {
                if (i > 0)
                {
                    series1x2 = GenerateNextXForLine1(series1x1, random, 0.1, 0.99-minXDistance_Normal);
                    series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal, random.NextDouble()));

                    if (Math.Abs(series1x1-series1x2) > 0.85)
                    {
                        series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal+ 0.25, random.NextDouble()));
                    }
                    else if (Math.Abs(series1x1 - series1x2) > 0.75)
                    {
                        series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal + 0.15, random.NextDouble()));
                    }
                    else if (Math.Abs(series1x1 - series1x2) > 0.65)
                    {
                        series2x2 = Math.Min(0.99, series1x2 + Math.Max(minXDistance_Normal + 0.1, random.NextDouble()));
                    }

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
                    particleSpeed: CurrentLevelSpeed,
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
                    particleSpeed: CurrentLevelSpeed,
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
            
            System.Diagnostics.Debug.WriteLine($"?? Generated {numberOfSegments * 2} custom line mechanics in two parallel series");
                                 
            return mechanics;
        }
        



        /// <summary>
        /// Generate next X coordinate with some randomness but within bounds
        /// </summary>
        private static double GenerateNextXForLine1(double currentX, Random random, double minBound, double maxBound)
        {
            double variation = (random.NextDouble() - 0.5) * 1.5; // ±10% variation
            double nextX = currentX + variation;

            if (Math.Abs(nextX-currentX) < 0.35) 
            {
                nextX = GenerateNextXForLine1(currentX, random, minBound, maxBound);
            }
            return Math.Clamp(nextX, minBound, maxBound);
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

      
        /// <summary>
        /// Reset level manager for a new game
        /// </summary>
        public void Reset(Canvas gameCanvas)
        {
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
            centerScreen = new Point(canvasWidth / 2, canvasHeight / 2);


            ParticleMechanicsBase.Reset(gameCanvas);
            

            currentLevel = 1;
            gameStartTime = DateTime.Now;
            lastLevelUpTime = gameStartTime;
            levelDuration = GameSettings.LevelDuration.Value;

            // Initialize the global level speed for level 1
            UpdateCurrentLevelSpeed(currentLevel);
            // Reset current speed to initial value for smooth ramping
            CurrentLevelSpeed = GameSettings.InitialLevelSpeed.Value + (currentLevel * GameSettings.SpeedIncreasePerLevel.Value);
            targetLevelSpeed = CurrentLevelSpeed;
            frameCounter = 0;

            // Stop all active mechanics
            foreach (var mechanic in sequenceMechanics)
            {
                mechanic.Stop();
            }
            
            System.Diagnostics.Debug.WriteLine($"?? Level Manager reset - Starting at level {currentLevel}");
            LogMechanicActivationLevels();
        }

        /// <summary>
        /// Update level progression based on elapsed time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        public void Update(double deltaTime)
        {
            frameCounter++;
            
            // Update speed ramping system for smooth speed transitions
            UpdateSpeedRamping(deltaTime);
            
            var now = DateTime.Now;
            var timeSinceLastLevel = (now - lastLevelUpTime).TotalSeconds;

            // Check if it's time to level up
            if (timeSinceLastLevel >= levelDuration)
            {
                LevelUp();
                lastLevelUpTime = now;
            }

            // Update all active mechanics
            UpdateActiveMechanics(deltaTime);
        }

        /// <summary>
        /// Force level up
        /// </summary>
        public void LevelUp()
        {
            currentLevel++;
            
            System.Diagnostics.Debug.WriteLine($"?? Level Up! Now at level {currentLevel}");
            
            // Update the global level speed for all mechanics to use
            UpdateCurrentLevelSpeed(currentLevel);
            
            // Update all active particles to match current level speed
            UpdateAllParticleSpeeds();
            
            // Trigger level changed event
            LevelChanged?.Invoke(this, currentLevel);
            
            // Check for level-specific mechanics
            ActivateMechanicsForLevel(currentLevel);
        }

        /// <summary>
        /// Update all active particles from all mechanics to match current level speed
        /// This prevents faster particles from higher levels catching up to slower particles from previous levels
        /// </summary>
        private void UpdateAllParticleSpeeds()
        {
            // Use the global CurrentLevelSpeed
            double currentLevelSpeed = CurrentLevelSpeed;
            
            // Update all active mechanics' particle speeds
            foreach (var mechanic in sequenceMechanics.Where(m => m.IsActive))
            {
                // Call update speed method if the mechanic supports it
                if (mechanic is ParticleMechanicsBase baseMechanic)
                {
                    baseMechanic.UpdateParticleSpeed(currentLevelSpeed);
                }
            }
        }

        /// <summary>
        /// Activate all mechanics that should trigger at the specified level
        /// </summary>
        /// <param name="level">Current level</param>
        private void ActivateMechanicsForLevel(int level)
        {
            if (GameSettings.LevelMechanicsEnabled == false) return;

            var mechanicsToActivate = allMechanics.Where(m => m.ActivationLevel == level).ToList();
            
            foreach (var mechanic in mechanicsToActivate)
            {
                try
                {
                    mechanic.Activate();
                    
                    string mechanicName = mechanic.GetType().Name.Replace("Mechanic", "");
                    string message = $"Level {level}: {mechanicName} activated! {mechanic.ParticleCount} particles incoming!";
                    
                    System.Diagnostics.Debug.WriteLine($"?? {message}");
                    LevelMechanicTriggered?.Invoke(this, message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error activating mechanic {mechanic.GetType().Name}: {ex.Message}");
                }
            }
            
            if (mechanicsToActivate.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"?? No mechanics to activate at level {level}");
            }
        }

        /// <summary>
        /// Update all active mechanics
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        private void UpdateActiveMechanics(double deltaTime)
        {
            foreach (var mechanic in allMechanics.Where(m => m.IsActive))
            {
                mechanic.Update(deltaTime);
            }
        }

        /// <summary>
        /// Check for collisions with any active level mechanic particles
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        public bool CheckParticleMechanicsCollisions(System.Windows.Point shipPosition)
        {
            return allMechanics.Where(m => m.IsActive).Any(m => m.CheckCollisions(shipPosition));
        }

        /// <summary>
        /// Get status information about current level and mechanics
        /// </summary>
        /// <returns>Level status string</returns>
        public string GetLevelStatus()
        {
            var timeSinceLastLevel = (DateTime.Now - lastLevelUpTime).TotalSeconds;
            var timeToNextLevel = Math.Max(0, levelDuration - timeSinceLastLevel);
            
            string status = $"Level {currentLevel}";
            
            if (timeToNextLevel > 0)
            {
                status += $" (Next in {timeToNextLevel:F1}s)";
            }
            
            // Add active mechanics status
            var activeMechanics = allMechanics.Where(m => m.IsActive).ToList();
            if (activeMechanics.Any())
            {
                var mechanicStatuses = activeMechanics.Select(m => 
                {
                    string name = m.GetType().Name.Replace("Mechanic", "");
                    return $"{name}: {m.GetActiveParticleCount()}";
                });
                status += $" | {string.Join(", ", mechanicStatuses)}";
            }

            return status;
        }

        /// <summary>
        /// Get the total number of level mechanic particles currently active
        /// </summary>
        /// <returns>Number of active level mechanic particles</returns>
        public int GetActiveLevelMechanicParticleCount()
        {
            return sequenceMechanics.Sum(m => m.GetActiveParticleCount());
        }

        /// <summary>
        /// Stop all level mechanics and clear particles
        /// </summary>
        public void StopAllMechanics()
        {
            foreach (var mechanic in allMechanics)
            {
                mechanic.Stop();
            }
            System.Diagnostics.Debug.WriteLine("?? All level mechanics stopped");
        }


        /// <summary>
        /// Manually trigger mechanics for a specific level (for testing)
        /// </summary>
        /// <param name="level">Level to trigger mechanics for</param>
        public void TriggerMechanicsForLevel(int level)
        {
            System.Diagnostics.Debug.WriteLine($"?? Manually triggering mechanics for level {level}");
            ActivateMechanicsForLevel(level);
        }

        /// <summary>
        /// Log the activation levels of all mechanics for debugging
        /// </summary>
        private void LogMechanicActivationLevels()
        {
            System.Diagnostics.Debug.WriteLine("?? Registered mechanics:");
            foreach (var mechanic in sequenceMechanics.OrderBy(m => m.ActivationLevel))
            {
                string name = mechanic.GetType().Name.Replace("Mechanic", "");
                System.Diagnostics.Debug.WriteLine($"  - {name}: Level {mechanic.ActivationLevel} ({mechanic.ParticleCount} particles)");
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            try
            {
                StopAllMechanics();
                
                // Dispose individual mechanics if they implement IDisposable
                foreach (var mechanic in sequenceMechanics)
                {
                    if (mechanic is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                sequenceMechanics.Clear();
                
                System.Diagnostics.Debug.WriteLine("?? Level Manager disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Level Manager: {ex.Message}");
            }
        }





        public static List<IParticleMechanics> GenerateLevel_Test_Star()
        {
            var mechanics = new List<IParticleMechanics>();

            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                // Random star properties
                int starParticleCount = random.Next(15, 30);
                double starLaunchTiming = random.NextDouble() * 0.05 + 0.02; // 0.02-0.07s intervals

                // Randomize size and points
                double minStarSize = 20.0 + random.NextDouble() * 10.0; // 20-30
                double maxStarSize = minStarSize + random.NextDouble() * 10.0; // +0-10 more

                int minPoints = random.Next(5, 8); // 5-7 points minimum
                int maxPoints = minPoints + random.Next(2, 4); // +2-3 more points

                // Random center position within safe bounds using available canvas dimensions
                double centerX = random.NextDouble() * (canvasWidth * 0.6) + (canvasWidth * 0.2); // 20%-80% of width
                double centerY = random.NextDouble() * (canvasHeight * 0.6) + (canvasHeight * 0.2); // 20%-80% of height
                var starCenter = new Vector2((float)centerX, (float)centerY);

                var starMechanic = new StarMechanic(
                    activationLevel: 1,
                    particleCount: starParticleCount,
                    launchTiming: starLaunchTiming,
                    particleSpeed: CurrentLevelSpeed, // Use shared speed reference
                    centerPosition: starCenter,
                    minStarSize: minStarSize,
                    maxStarSize: maxStarSize,
                    minPoints: minPoints,
                    maxPoints: maxPoints,
                    randomizeSize: true
                );

                mechanics.Add(starMechanic);
            }

            return mechanics;
        }

        public static List<IParticleMechanics> GenerateLevel_Medium_ZigZag()
        {
            var mechanics = new List<IParticleMechanics>();
            var zigzagMechanic = new ZigZagMechanic();
            var lastStartPosition = 0.45;

            // Make zigzag widths wider and maintain them across levels
            var minWidth = 0.25;  // Increased from 0.15 to 0.25
            var maxWidth = 0.55;  // Increased from 0.45 to 0.55
            var maxStartPosition = 1 - 2 * maxWidth;

            var random = new Random();
            for (int i = 0; i < 100; i++)
            {
                // All mechanics now use the shared CurrentLevelSpeed property
                // This ensures all particles use the same speed that gets updated each level

                int particleCount = random.Next(10, 20);

                // Launch interval that decreases as speed increases
                // Create a relationship where higher speeds result in faster firing
                double baseSpeed = GameSettings.InitialLevelSpeed.Value; // Use setting instead of hard-coded value
                double currentLevelSpeed = baseSpeed + (i * GameSettings.SpeedIncreasePerLevel.Value); // Use setting for speed increase

                // Calculate launch timing based on speed - inverse relationship
                // Higher speed = lower launch interval (faster firing)
                double speedFactor = baseSpeed / currentLevelSpeed; // Factor decreases as speed increases
                double baseLaunchTiming = 0.125; // Starting base timing
                double minTiming = 0.01; // Minimum timing to prevent it from becoming too fast

                // Apply speed factor to timing - as speed increases, timing decreases
                double speedBasedTiming = baseLaunchTiming * speedFactor;

                // Add progressive level reduction for additional difficulty
                double levelReduction = i * 0.0002; // Very small additional reduction per level
                double progressiveTiming = Math.Max(minTiming, speedBasedTiming - levelReduction);

                // Add some random variation to the progressive timing
                double randomVariation = (random.NextDouble() - 0.5) * 0.005; // ±0.0025 variation
                double launchTiming = Math.Max(minTiming, progressiveTiming + randomVariation);

                var num = random.NextDouble();
                double factor = Math.Min(1.3, Math.Max(0.7, num + 0.5));
                double startPosition = factor * lastStartPosition;

                startPosition = Math.Min(maxStartPosition, startPosition);

                var repeat = 2; // Set repeat to 2

                // Modified width calculation - remove difficulty factor that narrows width
                // Keep width consistent across levels for better gameplay
                var width = random.NextDouble() * (maxWidth - minWidth) + minWidth;
                width = Math.Min(maxWidth, width);

                // Separate distance variable that slowly decreases with each level
                // Start with width as base distance, but slowly reduce it
                double baseDistance = 0.5; // Base distance equal to width
                double distanceReduction = i * 0.002; // Reduce by 0.002 per level (slow decrease)
                double minDistance = 0.075; // Minimum distance to maintain some separation
                double zigzagDistance = Math.Max(minDistance, baseDistance - distanceReduction);

                // Randomize direction/reverse parameter for zigzag mechanics
                bool randomReverse1 = random.NextDouble() < 0.5; // 50% chance
                bool randomReverse2 = random.NextDouble() < 0.5; // 50% chance

                zigzagMechanic = new ZigZagMechanic(
                    activationLevel: i,
                    particleCount: particleCount,
                    particleSpeed: CurrentLevelSpeed, // Now uses shared speed reference
                    launchTiming: launchTiming,
                    startPosition: startPosition,
                    width: width,
                    repeat: repeat,
                    reverse: randomReverse1); // Randomized reverse parameter

                mechanics.Add(zigzagMechanic);

                // Use the separate distance variable instead of width for positioning
                zigzagMechanic = new ZigZagMechanic(
                                    activationLevel: i,
                                    particleCount: particleCount,
                                    particleSpeed: CurrentLevelSpeed, // Now uses shared speed reference
                                    launchTiming: launchTiming,
                                    startPosition: startPosition + zigzagDistance, // Use separate distance variable
                                    width: width,
                                    repeat: repeat,
                                    reverse: randomReverse2); // Randomized reverse parameter

                mechanics.Add(zigzagMechanic);

                // Add LineMechanic that launches simultaneously with ZigZag mechanics
                // Opening width spans both zigzag mechanics using the distance
                // Now uses shared speed reference
                var lineMechanic = new LineMechanic(
                    activationLevel: i,
                    particleSpeed: CurrentLevelSpeed, // Now uses shared speed reference
                    openingStart: startPosition, // Start where first zigzag starts
                    openingWidth: zigzagDistance, // Use distance for line opening width
                    repeatCount: 1, // Only one line
                    delayMilliseconds: 0); // Launch immediately (same time as zigzags)

                mechanics.Add(lineMechanic);

                // Add random StarMechanic - 30% chance to spawn a star on each level
                if (random.NextDouble() < 0.3)
                {
                    // Random star properties with smaller sizes
                    int starParticleCount = random.Next(15, 30); // Random particle count for stars
                    double starLaunchTiming = random.NextDouble() * 0.05 + 0.02; // 0.02-0.07s intervals

                    // Smaller random star sizes to work with the new compact formation
                    double minStarSize = 30.0 + random.NextDouble() * 20.0; // 30-50 (reduced from 60-100)
                    double maxStarSize = minStarSize + random.NextDouble() * 40.0; // +0-40 more (reduced from +0-80)

                    // Random star points
                    int minPoints = random.Next(4, 6); // 4-5 points minimum
                    int maxPoints = minPoints + random.Next(1, 4); // +1-3 more points

                    // Random center position within safe bounds using available canvas dimensions
                    double centerX = random.NextDouble() * (canvasWidth * 0.6) + (canvasWidth * 0.2); // 20%-80% of width
                    double centerY = random.NextDouble() * (canvasHeight * 0.6) + (canvasHeight * 0.2); // 20%-80% of height
                    var starCenter = new Vector2((float)centerX, (float)centerY);

                    var starMechanic = new StarMechanic(
                        activationLevel: i,
                        particleCount: starParticleCount,
                        launchTiming: starLaunchTiming,
                        particleSpeed: CurrentLevelSpeed, // Use shared speed reference
                        centerPosition: starCenter,
                        minStarSize: minStarSize,
                        maxStarSize: maxStarSize,
                        minPoints: minPoints,
                        maxPoints: maxPoints,
                        randomizeSize: true
                    );

                    mechanics.Add(starMechanic);

                    System.Diagnostics.Debug.WriteLine($"?? Added compact random star to level {i}: {starParticleCount} particles, {minPoints}-{maxPoints} points, size {minStarSize:F0}-{maxStarSize:F0}");
                }

                lastStartPosition = startPosition;
            }

            return mechanics;
        }
    }
}