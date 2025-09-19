namespace tkkn2025.GameObjects.LevelMechanics
{
    /// <summary>
    /// Interface for particle-based level mechanics
    /// </summary>
    public interface IParticleMechanics
    {
        /// <summary>
        /// The level at which this mechanic should be activated
        /// </summary>
        int ActivationLevel { get; }

        /// <summary>
        /// The number of particles this mechanic will create
        /// </summary>
        int ParticleCount { get; }

        /// <summary>
        /// Whether this mechanic is currently active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Activate the particle mechanic
        /// </summary>
        void Activate();

        /// <summary>
        /// Stop the mechanic and remove all particles from the canvas
        /// </summary>
        void Stop();

        /// <summary>
        /// Update the mechanic (called every frame)
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        void Update(double deltaTime);

        /// <summary>
        /// Check for collisions with the ship
        /// </summary>
        /// <param name="shipPosition">Current ship position</param>
        /// <returns>True if collision detected</returns>
        bool CheckCollisions(System.Windows.Point shipPosition);

        /// <summary>
        /// Get the number of active particles for this mechanic
        /// </summary>
        /// <returns>Number of active particles</returns>
        int GetActiveParticleCount();
    }
}