using System;
using System.Numerics;
using tkkn2025.GameObjects.LevelMechanics.ParticleSprites;

namespace tkkn2025.GameObjects.LevelMechanics.ParticleSprites
{
    /// <summary>
    /// Example usage of the SpiralMechanic class
    /// </summary>
    public static class SpiralMechanicExample
    {
        /// <summary>
        /// Creates a basic spiral mechanic at the center of the screen
        /// </summary>
        /// <param name="canvasWidth">Canvas width</param>
        /// <param name="canvasHeight">Canvas height</param>
        /// <returns>Configured SpiralMechanic</returns>
        public static SpiralAndSine CreateCenterSpiral(double canvasWidth, double canvasHeight)
        {
            var centerPosition = new Vector2((float)(canvasWidth / 2), (float)(canvasHeight / 2));
            
            // Create with default configuration
            var spiral = new SpiralAndSine(
                position: centerPosition,
                activationLevel: 1,
                particleCount: 25
            );
            
            return spiral;
        }

        /// <summary>
        /// Creates a custom spiral mechanic with specific configuration
        /// </summary>
        /// <param name="position">Position for the spiral</param>
        /// <param name="activationLevel">Level at which it activates</param>
        /// <returns>Configured SpiralMechanic</returns>
        public static SpiralAndSine CreateCustomSpiral(Vector2 position, int activationLevel = 1)
        {
            // Create custom configuration
            var config = new SpiralAndSineConfig
            {
                Radius = 100,
                ParticleSpacing = 0.002,
                Size = 4,
                ParticleCount = 30,
                Frequency = 0.015,
                Amplitude = 30,
                Speed = 0.04
            };
            
            var spiral = new SpiralAndSine(
                position: position,
                activationLevel: activationLevel,
                particleCount: config.ParticleCount,
                config: config
            );
            
            return spiral;
        }

        /// <summary>
        /// Creates multiple spirals at different positions
        /// </summary>
        /// <param name="canvasWidth">Canvas width</param>
        /// <param name="canvasHeight">Canvas height</param>
        /// <returns>Array of SpiralMechanics</returns>
        public static SpiralAndSine[] CreateMultipleSpirals(double canvasWidth, double canvasHeight)
        {
            var spirals = new SpiralAndSine[4];
            
            // Create spirals at four corners (inset by 100px)
            var positions = new[]
            {
                new Vector2(100, 100),                                    // Top-left
                new Vector2((float)(canvasWidth - 100), 100),            // Top-right
                new Vector2(100, (float)(canvasHeight - 100)),           // Bottom-left
                new Vector2((float)(canvasWidth - 100), (float)(canvasHeight - 100)) // Bottom-right
            };

            for (int i = 0; i < positions.Length; i++)
            {
                spirals[i] = new SpiralAndSine(
                    position: positions[i],
                    activationLevel: i + 1, // Activate at different levels
                    particleCount: 20
                );
            }

            return spirals;
        }

        /// <summary>
        /// Example of how to use the spiral in a level generation method
        /// </summary>
        public static void AddSpiralToLevel()
        {
            // This would typically be called within LevelManager.GenerateSomeLevel()
            
            // Example configuration
            var centerPosition = new Vector2(400, 300);
            
            // Create the spiral
            var spiral = CreateCustomSpiral(centerPosition, activationLevel: 5);
            
            // Add to your mechanics list (this would be done in the actual level generation)
            // mechanics.Add(spiral);
            
            System.Diagnostics.Debug.WriteLine($"Created spiral at {centerPosition} for level 5");
        }
    }
}