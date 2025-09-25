# LineMechanic Documentation

## Overview
The LineMechanic creates horizontal lines of particles that move straight down across the screen with configurable openings (gaps) for the player to pass through. Multiple lines can be created with delays between them for increased challenge.

## Constructor Parameters
```csharp
LineMechanic(int activationLevel = 2, double particleSpeed = 150.0, double openingStart = 0.4, double openingWidth = 0.2, int repeatCount = 1, double delayMilliseconds = 500.0)
```

### Parameters:
- **activationLevel**: The level at which this mechanic activates (default: 2)
- **particleSpeed**: Initial speed at which the lines move down in pixels per second (default: 150.0, minimum: 50.0)
- **openingStart**: Starting position of the opening as percentage of canvas width (0.0 to 1.0, default: 0.4 = 40% from left)
- **openingWidth**: Width of the opening as percentage of canvas width (0.1 to 0.8, default: 0.2 = 20% of screen width)
- **repeatCount**: Number of lines to create (default: 1, minimum: 1)
- **delayMilliseconds**: Delay between lines in milliseconds (default: 500.0, minimum: 100.0)

## Visual Properties
- **Color**: Cyan particles to distinguish from other mechanics
- **Particle Size**: 8.0 pixels diameter
- **Particle Spacing**: 12.0 pixels between particles

## Usage Examples

### Single Line (Basic)
```csharp
new LineMechanic(activationLevel: 3, particleSpeed: 120, openingStart: 0.35, openingWidth: 0.3)
```
- Activates at level 3
- Initial speed of 120 pixels/second (will be updated dynamically)
- Opening starts at 35% from left edge
- Opening is 30% of screen width
- Creates 1 line (default)

### Multiple Lines with Delay
```csharp
new LineMechanic(activationLevel: 5, particleSpeed: 250, openingStart: 0.4, openingWidth: 0.2, repeatCount: 5, delayMilliseconds: 750)
```
- Activates at level 5
- Initial speed of 250 pixels/second (will be updated dynamically)
- Opening starts at 40% from left edge
- Opening is 20% of screen width
- Creates 5 lines with 750ms delay between each

### Coordinated with ZigZag (GenerateLevel_Medium)
```csharp
new LineMechanic(activationLevel: i, particleSpeed: baseSpeed, openingStart: startPosition, openingWidth: width, repeatCount: 1, delayMilliseconds: 0)
```
- Activates simultaneously with ZigZag mechanics (delay: 0ms)
- **Dynamic Speed System**: All mechanics use base speed that gets updated when level changes
- **Synchronized Movement**: All active particles move at current level speed
- **Smaller Opening**: Opening covers only the first ZigZag area (50% smaller than before)
- **Enhanced ZigZag Patterns**: Each ZigZag mechanic has repeat=2 and randomized reverse parameter
- Single line per activation for precise timing

## Behavior Details

### Dynamic Speed Update System
- **Initial Speed**: All particles start with the speed specified at creation
- **Level-Based Updates**: When the game level increases, ALL active particles are updated to the new level speed
- **Synchronized Movement**: Prevents faster particles from higher levels overtaking slower particles from previous levels
- **Consistent Challenge**: All particles on screen move at the same speed regardless of when they were created

### Repeat Mechanics
- The first line is created immediately when the mechanic activates
- Subsequent lines are created using a timer with the specified delay
- Each line follows the same path and speed
- All lines have identical opening positions and sizes

### Timing System
- Uses WPF's DispatcherTimer for precise timing
- Timer stops automatically after all lines are created
- Each line is independent once created
- Setting delayMilliseconds to 0 creates immediate launch (simultaneous with activation)

### Particle Management
- Total ParticleCount = (particles per line) × repeatCount
- All particles move straight down at the same speed
- Particles are removed when they leave the bottom of the screen
- Mechanic completes when all particles from all lines have left the screen
- **Speed Synchronization**: All active particles automatically update to current level speed

## Integration in GenerateLevel_Medium
The LineMechanic has been optimized for coordinated gameplay with dynamic speed management:

### Current Configuration:
- **Repeat Count**: 1 (single line per activation)
- **Delay**: 0ms (launches simultaneously with ZigZag mechanics)
- **Dynamic Speed System**: All mechanics start with base speed (100 pixels/second)
- **Speed Formula**: `100 + (currentLevel × 50)` pixels/second
- **Automatic Updates**: All active particles update to current level speed when level changes
- **Reduced Opening**: Only covers first ZigZag width (50% smaller than previous version)
- **Enhanced ZigZag Mechanics**: 
  - Repeat count increased to 2 (double the zigzag patterns)
  - Randomized direction/reverse parameters for unpredictable patterns
- **Timing**: Synchronized launches create coordinated challenge patterns

### Dynamic Speed Progression:
- **All Levels**: All active particles always move at current level speed
- **Level 1**: 150 pixels/second (100 + 1×50)
- **Level 5**: 350 pixels/second (100 + 5×50)
- **Level 10**: 600 pixels/second (100 + 10×50)
- **Level 20**: 1,100 pixels/second (100 + 20×50)
- **Level 40**: 2,100 pixels/second (100 + 40×50)

### Key Benefits of Dynamic Speed System:
- **Fair Gameplay**: No particle overtaking issues
- **Consistent Challenge**: All threats move at the same speed
- **Progressive Difficulty**: Speed increases affect all active particles simultaneously
- **Strategic Gameplay**: Players can predict particle behavior based on current level
- **Synchronized Patterns**: Complex multi-mechanic patterns remain coordinated

### Enhanced Challenge Mechanics:
- **Uniform Acceleration**: All particles accelerate together when level increases
- **Complex ZigZag Patterns**: Repeat=2 creates more elaborate zigzag movements
- **Unpredictable Directions**: Random ZigZag directions prevent pattern memorization
- **Tighter Navigation**: Smaller LineMechanic opening requires precise positioning
- **Coordinated Timing**: All mechanics launch simultaneously with synchronized speeds

This creates a fair and challenging gameplay experience where all particles maintain consistent movement speeds, preventing the chaos of mixed-speed particles while still providing escalating difficulty through coordinated speed increases.