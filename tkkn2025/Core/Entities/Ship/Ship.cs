using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using tkkn2025.GameObjects.Ship;
using tkkn2025.Settings;

namespace tkkn2025.Core.Entities.Ship
{
    /// <summary>
    /// Represents the player's ship with all movement, input handling, and boost mechanics
    /// </summary>
    public class Ship : Entity
    {
        #region Private Fields

        private readonly ShipSprite shipSprite;
        private readonly Canvas gameCanvas;
        
        // Input state tracking
        private readonly bool[] keysPressed = new bool[7]; // Up, Down, Left, Right, LeftShift, RightShift, Space
        
        // Boost mechanics
        private DateTime lastSpaceKeyPress = DateTime.MinValue;
        private bool isSuperBoostActive = false;
        private const double DoubleTapThreshold = 0.15; // 150ms for double-tap detection
        
        // Ship settings (snapshot from current game settings)
        private double shipSpeed;
        private double shipBoost;
        private double shipSuperBoost;
        
        // Canvas bounds for movement constraints
        private double canvasWidth;
        private double canvasHeight;

        #endregion

        #region Properties

        /// <summary>
        /// Current ship position as Point for compatibility with existing code
        /// </summary>
        public Point ShipPosition => new Point(Position.X, Position.Y);

        /// <summary>
        /// Whether super boost is currently active
        /// </summary>
        public bool IsSuperBoostActive => isSuperBoostActive;

        /// <summary>
        /// The visual sprite component
        /// </summary>
        public ShipSprite Sprite => shipSprite;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialize ship with canvas and starting position
        /// </summary>
        /// <param name="gameCanvas">Game canvas for positioning and bounds</param>
        /// <param name="startPosition">Starting position</param>
        
        public Ship(Canvas gameCanvas, Vector2 startPosition) : base(startPosition)
        {
            this.gameCanvas = gameCanvas;
            
            // Create and add the visual sprite to canvas
            shipSprite = new ShipSprite();
            gameCanvas.Children.Add(shipSprite);
            
            // Initialize with current game settings
            UpdateGameSettings();
            UpdateCanvasDimensions();
            
            // Reset to starting position
            Reset(new Point(startPosition.X, startPosition.Y));
        }

        /// <summary>
        /// Default constructor for compatibility
        /// </summary>
        public Ship() : base()
        {
            throw new InvalidOperationException("Ship requires a canvas and starting position. Use Ship(Canvas, Vector2) constructor.");
        }

        #endregion

        #region Initialization and Settings

        /// <summary>
        /// Update ship settings from current game settings
        /// Call this when starting a new game to snapshot current settings
        /// </summary>
        public void UpdateGameSettings()
        {
            shipSpeed = GameSettings.ShipSpeed.Value;
            shipBoost = GameSettings.ShipBoost.Value;
            shipSuperBoost = GameSettings.ShipSuperBoost.Value;
        }

        /// <summary>
        /// Update canvas dimensions when window is resized
        /// </summary>
        public void UpdateCanvasDimensions()
        {
            canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : 800;
            canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : 600;
        }

        /// <summary>
        /// Reset ship to specified position and clear all states
        /// </summary>
        /// <param name="position">Position to reset to</param>
        public void Reset(Point position)
        {
            // Update position
            Position = new Vector2((float)position.X, (float)position.Y);
            
            // Reset all input states
            for (int i = 0; i < keysPressed.Length; i++)
            {
                keysPressed[i] = false;
            }
            
            // Reset boost states
            lastSpaceKeyPress = DateTime.MinValue;
            isSuperBoostActive = false;
            
            // Update visual position and state
            UpdateVisualPosition();
            shipSprite.ShowNeutral();
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle key down events for ship movement and boost
        /// </summary>
        /// <param name="key">The key that was pressed</param>
        /// <returns>True if the key was handled by the ship</returns>
        public bool HandleKeyDown(Key key)
        {
            switch (key)
            {
                case Key.Up:
                case Key.W:
                    keysPressed[0] = true;
                    return true;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = true;
                    return true;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = true;
                    return true;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = true;
                    return true;
                case Key.LeftShift:
                    keysPressed[4] = true;
                    return true;
                case Key.RightShift:
                    keysPressed[5] = true;
                    return true;
                case Key.Space:
                    return HandleSpaceKeyDown();
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle key up events for ship movement and boost
        /// </summary>
        /// <param name="key">The key that was released</param>
        /// <returns>True if the key was handled by the ship</returns>
        public bool HandleKeyUp(Key key)
        {
            switch (key)
            {
                case Key.Up:
                case Key.W:
                    keysPressed[0] = false;
                    return true;
                case Key.Down:
                case Key.S:
                    keysPressed[1] = false;
                    return true;
                case Key.Left:
                case Key.A:
                    keysPressed[2] = false;
                    return true;
                case Key.Right:
                case Key.D:
                    keysPressed[3] = false;
                    return true;
                case Key.LeftShift:
                    keysPressed[4] = false;
                    return true;
                case Key.RightShift:
                    keysPressed[5] = false;
                    return true;
                case Key.Space:
                    return HandleSpaceKeyUp();
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle space key down for boost and super boost mechanics
        /// </summary>
        private bool HandleSpaceKeyDown()
        {
            var now = DateTime.Now;
            var timeSinceLastPress = (now - lastSpaceKeyPress).TotalSeconds;

            // Check for double-tap to activate super boost
            if (timeSinceLastPress <= DoubleTapThreshold && !isSuperBoostActive)
            {
                isSuperBoostActive = true;
                GameEvents.RaiseMessageRequested("Super Boost Activated!", System.Windows.Media.Brushes.Cyan);
            }

            lastSpaceKeyPress = now;
            keysPressed[6] = true; // Space key for regular boost
            return true;
        }

        /// <summary>
        /// Handle space key up for boost deactivation
        /// </summary>
        private bool HandleSpaceKeyUp()
        {
            keysPressed[6] = false;

            // Deactivate super boost when space key is released
            if (isSuperBoostActive)
            {
                isSuperBoostActive = false;
                GameEvents.RaiseMessageRequested("Super Boost Deactivated", System.Windows.Media.Brushes.LightGray);
            }

            return true;
        }

        #endregion

        #region Movement and Update

        /// <summary>
        /// Update ship position based on current input state and elapsed time
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update in seconds</param>
        public void UpdatePosition(double deltaTime)
        {
            double deltaX = 0, deltaY = 0;

            // Determine current speed based on boost state
            double effectiveSpeed = CalculateEffectiveSpeed();

            // Calculate movement based on input state and time
            if (keysPressed[0]) deltaY -= effectiveSpeed * deltaTime; // Up
            if (keysPressed[1]) deltaY += effectiveSpeed * deltaTime; // Down
            if (keysPressed[2]) deltaX -= effectiveSpeed * deltaTime; // Left
            if (keysPressed[3]) deltaX += effectiveSpeed * deltaTime; // Right

            // Apply movement if any input is detected
            if (deltaX != 0 || deltaY != 0)
            {
                // Calculate new position with bounds checking
                double newX = Math.Max(20, Math.Min(canvasWidth - 20, Position.X + deltaX));
                double newY = Math.Max(20, Math.Min(canvasHeight - 20, Position.Y + deltaY));

                Position = new Vector2((float)newX, (float)newY);
                UpdateVisualPosition();

                // Update ship visual based on movement direction
                UpdateShipTilt(deltaX);
            }
            else
            {
                // No movement - show neutral position
                shipSprite.ShowNeutral();
            }
        }

        /// <summary>
        /// Calculate effective speed based on current boost states
        /// </summary>
        private double CalculateEffectiveSpeed()
        {
            // Check for super boost (double-tap space key detection)
            bool isSuperBoostPressed = isSuperBoostActive && keysPressed[6]; // Space key held down after double-tap

            // Check if either shift key or space key is pressed for regular boost
            bool isRegularBoostActive = (keysPressed[4] || keysPressed[5] || keysPressed[6]) && !isSuperBoostPressed;

            if (isSuperBoostPressed)
            {
                return shipSpeed + shipSuperBoost;
            }
            else if (isRegularBoostActive)
            {
                return shipSpeed + shipBoost;
            }
            else
            {
                return shipSpeed;
            }
        }

        /// <summary>
        /// Update ship visual tilt based on movement direction
        /// </summary>
        /// <param name="deltaX">Horizontal movement delta</param>
        private void UpdateShipTilt(double deltaX)
        {
            if (deltaX < 0) // Moving left
            {
                shipSprite.ShowLeftTilt();
            }
            else if (deltaX > 0) // Moving right
            {
                shipSprite.ShowRightTilt();
            }
            else // No horizontal movement
            {
                shipSprite.ShowNeutral();
            }
        }

        /// <summary>
        /// Update the visual position of the ship sprite on the canvas
        /// </summary>
        private void UpdateVisualPosition()
        {
            Canvas.SetLeft(shipSprite, Position.X - shipSprite.Width / 2);
            Canvas.SetTop(shipSprite, Position.Y - shipSprite.Height / 2);
        }

        #endregion

        #region Status and State

        /// <summary>
        /// Check if any movement keys are currently pressed
        /// </summary>
        public bool IsMoving()
        {
            return keysPressed[0] || keysPressed[1] || keysPressed[2] || keysPressed[3];
        }

        /// <summary>
        /// Check if any boost keys are currently pressed
        /// </summary>
        public bool IsBoosting()
        {
            return keysPressed[4] || keysPressed[5] || keysPressed[6];
        }

        /// <summary>
        /// Get current boost level as a string for display
        /// </summary>
        public string GetBoostStatus()
        {
            if (isSuperBoostActive && keysPressed[6])
                return "SUPER BOOST";
            else if (IsBoosting())
                return "BOOST";
            else
                return "";
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Remove ship sprite from canvas when disposing
        /// </summary>
        public void Dispose()
        {
            if (gameCanvas.Children.Contains(shipSprite))
            {
                gameCanvas.Children.Remove(shipSprite);
            }
        }

        #endregion
    }
}
