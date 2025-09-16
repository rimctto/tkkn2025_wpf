# GameEvents Hub System

This project now uses a centralized event management system through the `GameEvents` static class. This replaces the scattered direct event wiring between classes with a clean, simple hub pattern.

## Benefits

- **Cleaner Architecture**: No more direct event wiring between classes
- **Easier Maintenance**: All events are centralized in one place
- **Better Decoupling**: Classes don't need references to each other
- **Simple to Use**: Just raise events from anywhere, subscribe from anywhere

## How It Works

### Raising Events
Instead of declaring and invoking events directly:

```csharp
// OLD WAY - Direct event in class
public event Action<string>? PowerUpCollected;
PowerUpCollected?.Invoke(powerUpType);

// NEW WAY - Use GameEvents hub
GameEvents.RaisePowerUpCollected(powerUpType);
```

### Subscribing to Events
Instead of wiring events between classes:

```csharp
// OLD WAY - Direct wiring
particleManager.CollisionDetected += OnCollisionDetected;
powerUpManager.PowerUpCollected += OnPowerUpCollected;

// NEW WAY - Subscribe to GameEvents hub
GameEvents.CollisionDetected += OnCollisionDetected;
GameEvents.PowerUpCollected += OnPowerUpCollected;
```

## Available Events

### Game State Events
- `GameStarted` - When a game begins
- `GameEnded` - When a game ends
- `CollisionDetected` - When ship collides with particle

### Power-up Events
- `PowerUpCollected(string powerUpType)` - When a power-up is picked up
- `PowerUpEffectStarted(string effectType, double duration)` - When effect begins
- `PowerUpEffectEnded(string effectType)` - When effect ends
- `PowerUpStored(string powerUpType)` - When power-up is stored for later use
- `SingularityActivated(Vector2 position)` - When singularity is activated
- `RepulsorActivated(Vector2 position)` - When repulsor is activated

### UI Events
- `MessageRequested(string message, Brush color)` - Request to show UI message
- `ParticleCountChanged(int count)` - When particle count changes
- `GameTimeUpdated(double seconds)` - When game time updates

### Configuration Events
- `ConfigurationSaved` - When a game configuration is saved
- `ConfigurationLoaded(GameConfig config)` - When configuration is loaded

### Session Events
- `GameCompleted(Game game)` - When a game is completed
- `SessionStatsUpdated(Session session)` - When session stats change

### Audio Events
- `MusicToggled(bool enabled)` - When music is toggled on/off

### Screen Navigation Events
- `ShowStartScreen` - Request to show start screen
- `ShowGameOverScreen` - Request to show game over screen
- `ShowConfigScreen` - Request to show config screen
- `HideConfigScreen` - Request to hide config screen

## Example Usage

```csharp
public class MyGameComponent
{
    public MyGameComponent()
    {
        // Subscribe to events you care about
        GameEvents.PowerUpCollected += OnPowerUpCollected;
        GameEvents.GameEnded += OnGameEnded;
    }

    private void OnPowerUpCollected(string powerUpType)
    {
        Console.WriteLine($"Power-up collected: {powerUpType}");
    }

    private void OnGameEnded()
    {
        Console.WriteLine("Game ended!");
    }

    // Don't forget to unsubscribe to prevent memory leaks
    public void Dispose()
    {
        GameEvents.PowerUpCollected -= OnPowerUpCollected;
        GameEvents.GameEnded -= OnGameEnded;
    }
}
```

## Classes Updated

The following classes have been updated to use the GameEvents hub:

- **MainWindow**: Subscribes to all relevant events, no more direct event wiring
- **ParticleManager**: Raises `CollisionDetected` and `ParticleCountChanged` events
- **PowerUpManager**: Raises all power-up related events
- **GameConfigScreen**: Raises configuration and navigation events

## Memory Management

Always remember to unsubscribe from events when your class is disposed to prevent memory leaks:

```csharp
// In your class destructor or dispose method
GameEvents.CollisionDetected -= OnCollisionDetected;
GameEvents.PowerUpCollected -= OnPowerUpCollected;
```

Or use the utility method to clear all subscriptions (mainly for testing):

```csharp
GameEvents.ClearAllSubscriptions();
```