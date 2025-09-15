using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows.Controls;
using tkkn2025.Settings;

namespace tkkn2025.GameObjects.PowerUps
{
    /// <summary>
    /// Represents a power-up in the game that can be collected by the player
    /// </summary>
    public class PowerUp : GameObject
    {
        /// <summary>
        /// Visual representation of the power-up on the canvas
        /// </summary>
        public UserControl? Visual { get; set; }

        /// <summary>
        /// The type of power-up (e.g., speed boost, shield, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this power-up does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The factor by which this power-up affects the player (e.g., 1.5 for 50% speed increase)
        /// </summary>
        public double EffectFactor { get; set; } = 1.0;

        /// <summary>
        /// Duration of the power-up effect in seconds
        /// </summary>
        public double Duration { get; set; } = 0.0;

        /// <summary>
        /// Initialize power-up with starting position
        /// </summary>
        /// <param name="startPosition">Starting position as Vector2</param>
        public PowerUp(Vector2 startPosition) : base(startPosition)
        {
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PowerUp() : base()
        {
        }

        /// <summary>
        /// Initialize power-up with all properties
        /// </summary>
        /// <param name="startPosition">Starting position</param>
        /// <param name="type">Type of power-up</param>
        /// <param name="description">Description of the power-up</param>
        /// <param name="effectFactor">Effect factor</param>
        /// <param name="duration">Duration in seconds</param>
        public PowerUp(Vector2 startPosition, string type, string description, double effectFactor, double duration) 
            : base(startPosition)
        {
            Type = type;
            Description = description;
            EffectFactor = effectFactor;
            Duration = duration;
        }
    }
}
