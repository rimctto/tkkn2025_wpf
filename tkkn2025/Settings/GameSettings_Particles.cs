using System.Collections.Generic;
using tkkn2025.Settings.Models;

namespace tkkn2025.Settings
{
    /// <summary>
    /// Contains all basic game settings as SettingModelBase instances
    /// </summary>
    public partial class GameSettings
    {



        /// <summary>
        /// Particle movement speed setting
        /// </summary>
        public DoubleSetting ParticleSpeed { get; } = new DoubleSetting(
            name: nameof(ParticleSpeed),
            displayName: "Particle Speed",
            category: "Particles",
            defaultValue: 175.0,
            min: 25.0,
            max: 300.0
        );




        /// <summary>
        /// Particle turn speed for steering behavior (radians per second)
        /// </summary>
        public DoubleSetting ParticleTurnSpeed { get; } = new DoubleSetting(
            name: nameof(ParticleTurnSpeed),
            displayName: "Particle Turn Speed",
            category: "Particles",
            defaultValue: .9,
            min: 0.1,
            max: 1 ,
            description: "How quickly particles can change direction when chasing the ship (radians per second)"
        );


        /// <summary>
        /// Percentage variance in particle speed
        /// </summary>
        public DoubleSetting ParticleSpeedVariance { get; } = new DoubleSetting(
            name: nameof(ParticleSpeedVariance),
            displayName: "Speed Variance (%)",
            category: "Particles",
            defaultValue: 15.0,
            min: 0.0,
            max: 100.0,
            description: "Percentage variance in particle speed. Particles will spawn faster or slower."
        );

        /// <summary>
        /// Percentage of particles that get randomized speed
        /// </summary>
        public DoubleSetting ParticleRandomizerPercentage { get; } = new DoubleSetting(
            name: nameof(ParticleRandomizerPercentage),
            displayName: "Particle Randomizer %",
            category: "Particles",
            defaultValue: 30.0,
            min: 0.0,
            max: 100.0,
            description: "Percentage of particles that spawn with random properties."
        );

        /// <summary>
        /// Whether particles chase the ship's current position or move toward center
        /// </summary>
        public BoolSetting IsParticleSpawnVectorTowardsShip { get; } = new BoolSetting(
            name: nameof(IsParticleSpawnVectorTowardsShip),
            displayName: "Particles Spawn Towards Ship",
            category: "Particles",
            defaultValue: true,
            description: "If true, particles will initially move toward the ship's position. If false, they will move toward the center of the play area."
        );

        /// <summary>
        /// Whether particles actively chase the ship during gameplay
        /// </summary>
        public BoolSetting IsParticleChaseShip { get; } = new BoolSetting(
            name: nameof(IsParticleChaseShip),
            displayName: "Particles Chase Ship",
            category: "Particles",
            defaultValue: true,
            description: "If true, particles will continuously steer toward the ship. If false, they move in straight lines after initial spawn."
        );

        /// <summary>
        /// Duration of Repulsor power-up effect in seconds
        /// </summary>
        public DoubleSetting RepulsorDuration { get; } = new DoubleSetting(
            name: nameof(RepulsorDuration),
            displayName: "Repulsor Duration",
            category: "Particles",
            defaultValue: 5.0,
            min: 1.0,
            max: 15.0,
            description: "Duration in seconds that the Repulsor effect lasts when activated."
        );

        /// <summary>
        /// Strength of Repulsor force applied to particles
        /// </summary>
        public DoubleSetting RepulsorForce { get; } = new DoubleSetting(
            name: nameof(RepulsorForce),
            displayName: "Repulsor Force",
            category: "Particles",
            defaultValue: 1000.0,
            min: 100.0,
            max: 5000.0,
            description: "Strength of the repelling force applied by the Repulsor power-up."
        );
    }

}