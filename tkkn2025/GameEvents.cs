using System;
using System.Numerics;
using System.Windows.Media;

namespace tkkn2025
{
    /// <summary>
    /// Central static event hub for managing all game events.
    /// This eliminates the need for direct event wiring between classes.
    /// </summary>
    public static class GameEvents
    {
        // Game State Events
        public static event Action? GameStarted;
        public static event Action? GameEnded;
        public static event Action? CollisionDetected;
        
        // Power-up Events
        public static event Action<string>? PowerUpCollected;
        public static event Action<string, double>? PowerUpEffectStarted;
        public static event Action<string>? PowerUpEffectEnded;
        public static event Action<string>? PowerUpStored;
        public static event Action<Vector2>? SingularityActivated;
        public static event Action<Vector2>? RepulsorActivated;
        
        // UI Events
        public static event Action<string, Brush>? MessageRequested;
        public static event Action<int>? ParticleCountChanged;
        public static event Action<double>? GameTimeUpdated;
        
        // Configuration Events
        public static event Action? ConfigurationSaved;
        public static event Action<GameConfig>? ConfigurationLoaded;
        
        // Session Events
        public static event Action<Game>? GameCompleted;
        public static event Action<Session>? SessionStatsUpdated;
        
        // Audio Events
        public static event Action<bool>? MusicToggled;
        
        // Screen Navigation Events
        public static event Action? ShowStartScreen;
        public static event Action? ShowGameOverScreen;
        public static event Action? ShowConfigScreen;
        public static event Action? HideConfigScreen;

        #region Event Raisers - Game State
        
        public static void RaiseGameStarted() => GameStarted?.Invoke();
        
        public static void RaiseGameEnded() => GameEnded?.Invoke();
        
        public static void RaiseCollisionDetected() => CollisionDetected?.Invoke();
        
        #endregion

        #region Event Raisers - Power-ups
        
        public static void RaisePowerUpCollected(string powerUpType) => PowerUpCollected?.Invoke(powerUpType);
        
        public static void RaisePowerUpEffectStarted(string effectType, double duration) => 
            PowerUpEffectStarted?.Invoke(effectType, duration);
        
        public static void RaisePowerUpEffectEnded(string effectType) => PowerUpEffectEnded?.Invoke(effectType);
        
        public static void RaisePowerUpStored(string powerUpType) => PowerUpStored?.Invoke(powerUpType);
        
        public static void RaiseSingularityActivated(Vector2 position) => SingularityActivated?.Invoke(position);
        
        public static void RaiseRepulsorActivated(Vector2 position) => RepulsorActivated?.Invoke(position);
        
        #endregion

        #region Event Raisers - UI
        
        public static void RaiseMessageRequested(string message, Brush color) => 
            MessageRequested?.Invoke(message, color);
        
        public static void RaiseParticleCountChanged(int count) => 
            ParticleCountChanged?.Invoke(count);
        
        public static void RaiseGameTimeUpdated(double seconds) => 
            GameTimeUpdated?.Invoke(seconds);
        
        #endregion

        #region Event Raisers - Configuration
        
        public static void RaiseConfigurationSaved() => ConfigurationSaved?.Invoke();
        
        public static void RaiseConfigurationLoaded(GameConfig config) => ConfigurationLoaded?.Invoke(config);
        
        #endregion

        #region Event Raisers - Session
        
        public static void RaiseGameCompleted(Game game) => GameCompleted?.Invoke(game);
        
        public static void RaiseSessionStatsUpdated(Session session) => SessionStatsUpdated?.Invoke(session);
        
        #endregion

        #region Event Raisers - Audio
        
        public static void RaiseMusicToggled(bool enabled) => MusicToggled?.Invoke(enabled);
        
        #endregion

        #region Event Raisers - Screen Navigation
        
        public static void RaiseShowStartScreen() => ShowStartScreen?.Invoke();
        
        public static void RaiseShowGameOverScreen() => ShowGameOverScreen?.Invoke();
        
        public static void RaiseShowConfigScreen() => ShowConfigScreen?.Invoke();
        
        public static void RaiseHideConfigScreen() => HideConfigScreen?.Invoke();
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Clear all event subscriptions (useful for testing or reset scenarios)
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            GameStarted = null;
            GameEnded = null;
            CollisionDetected = null;
            PowerUpCollected = null;
            PowerUpEffectStarted = null;
            PowerUpEffectEnded = null;
            PowerUpStored = null;
            SingularityActivated = null;
            RepulsorActivated = null;
            MessageRequested = null;
            ParticleCountChanged = null;
            GameTimeUpdated = null;
            ConfigurationSaved = null;
            ConfigurationLoaded = null;
            GameCompleted = null;
            SessionStatsUpdated = null;
            MusicToggled = null;
            ShowStartScreen = null;
            ShowGameOverScreen = null;
            ShowConfigScreen = null;
            HideConfigScreen = null;
        }
        
        #endregion
    }
}