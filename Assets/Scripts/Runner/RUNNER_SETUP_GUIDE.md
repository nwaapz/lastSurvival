# Runner Gameplay System Setup Guide

This guide explains how to set up the Evony-style dodge gameplay where the **player stands in place** and enemies attack in rows from the front. The player must strafe left/right to avoid incoming enemies.

## Overview

The player **stands in place** (no running) and enemies approach from the front. The system supports **two movement modes**:

### 1. Free Movement Mode (Default)
- Player can move **pixel-by-pixel** left/right by swiping/dragging
- Smooth, continuous horizontal movement within configurable bounds
- More responsive, modern feel

### 2. Lane-Based Movement Mode
- Player moves between **fixed lanes** (typically 3 or 5)
- Discrete lane switching with strafe animations
- Classic dodge game feel

Both modes include:
- **Row-based enemies**: Enemies spawn in rows from the front, moving toward the stationary player
- **Touch/keyboard controls**: Swipe/drag or keyboard to strafe

## Scripts Created

| Script | Purpose |
|--------|---------|
| `RunnerGameManager` | Main game controller, manages state and scoring |
| `RunnerPlayerController` | Handles player lane-switching movement |
| `RunnerInputHandler` | Processes keyboard, touch, and UI button input |
| `RunnerEnemy` | Base enemy class that moves toward player |
| `RunnerEnemyRow` | Defines row patterns (which lanes have enemies) |
| `RunnerEnemySpawner` | Spawns enemy rows at intervals |
| `RunnerLaneConfig` | ScriptableObject defining movement bounds/lanes |
| `RunnerCameraController` | Camera that follows player's X position |
| `RunnerUIManager` | Handles score display and game panels |
| `RunnerObstacle` | Static obstacles player must avoid |
| `RunnerCollectible` | Coins and power-ups |
| `RunnerGroundScroller` | Creates illusion of forward movement |
| `RunnerLaneVisualizer` | Debug visualization of lanes |

## Quick Setup

### 1. Create Movement Configuration

1. Right-click in Project window
2. Select **Create > Runner > Lane Config**
3. **For Free Movement Mode** (default):
   - **Use Free Movement**: ✓ (checked)
   - **Move Speed**: 8 (movement responsiveness)
   - **Move Smooth Time**: 0.1 (smoothing factor)
   - **Min X Position**: -3 (left boundary)
   - **Max X Position**: 3 (right boundary)

4. **For Lane-Based Mode**:
   - **Use Free Movement**: ☐ (unchecked)
   - **Lane Count**: 3 (or 5 for more challenge)
   - **Lane Width**: 2 (units between lanes)
   - **Starting Lane**: 1 (middle lane for 3 lanes)

### 2. Scene Setup

Create these GameObjects in your runner scene:

```
Runner Scene Hierarchy:
├── RunnerGameManager (attach RunnerGameManager.cs)
│   └── Assign: LaneConfig
├── Player
│   ├── Model (your character model)
│   ├── Collider (Trigger)
│   └── Components:
│       ├── RunnerPlayerController
│       └── RunnerInputHandler
├── EnemySpawner (attach RunnerEnemySpawner.cs)
│   └── Assign: Enemy Prefab
├── Main Camera
│   └── RunnerCameraController
├── Ground
│   └── RunnerGroundScroller (optional)
├── UI Canvas
│   └── RunnerUIManager
└── LaneVisualizer (optional, for debugging)
```

### 3. Player Setup

1. Create player GameObject with:
   - Character model
   - **Collider** (set as Trigger)
   - **RunnerPlayerController** component
   - **RunnerInputHandler** component

2. Configure RunnerPlayerController:
   - **Strafe Duration**: 0.2s (how fast lane switch is)
   - **Strafe Curve**: EaseInOut for smooth movement
   - Assign **Animator** if using animations

### 4. Enemy Setup

1. Create enemy prefab with:
   - Enemy model
   - **Collider** (set as Trigger)
   - **RunnerEnemy** component

2. Configure RunnerEnemy:
   - **Base Speed**: 5 (or use game speed)
   - **Damage**: 1
   - **Score Value**: 10

### 5. Enemy Spawner Setup

1. Add **RunnerEnemySpawner** to scene
2. Configure:
   - **Default Enemy Prefab**: Your enemy prefab
   - **Spawn Z**: 50 (distance in front of player)
   - **Despawn Z**: -10 (behind player)
   - **Base Spawn Interval**: 2s
   - **Use Random Patterns**: true (or define custom patterns)

### 6. Camera Setup

1. Add **RunnerCameraController** to Main Camera
2. Configure:
   - **Offset**: (0, 5, -10) - behind and above player
   - **Follow X**: true (follow player's lane changes)
   - **Follow Y/Z**: false (stay fixed)
   - **Look At Target**: true

## Input Controls

### Free Movement Mode

**Keyboard:**
- **Hold A / Left Arrow**: Move left continuously
- **Hold D / Right Arrow**: Move right continuously

**Touch/Mobile:**
- **Drag Left/Right**: Move player pixel-by-pixel in drag direction
- Movement follows finger position with smooth damping

### Lane-Based Mode

**Keyboard:**
- **Press A / Left Arrow**: Switch one lane left
- **Press D / Right Arrow**: Switch one lane right

**Touch/Mobile:**
- **Swipe Left**: Switch one lane left
- **Swipe Right**: Switch one lane right

### UI Buttons
- Connect buttons to `RunnerInputHandler.OnLeftButtonPressed()` and `OnRightButtonPressed()`

## Enemy Row Patterns

The system supports predefined patterns:

```csharp
// Single enemy patterns
RunnerEnemyRowPatterns.LeftOnly(3);    // Enemy in lane 0
RunnerEnemyRowPatterns.MiddleOnly(3);  // Enemy in lane 1
RunnerEnemyRowPatterns.RightOnly(3);   // Enemy in lane 2

// Two enemy patterns (one safe lane)
RunnerEnemyRowPatterns.LeftAndRight(3);   // Safe: middle
RunnerEnemyRowPatterns.LeftAndMiddle(3);  // Safe: right
RunnerEnemyRowPatterns.MiddleAndRight(3); // Safe: left

// Random pattern (always has at least one safe lane)
RunnerEnemyRowPatterns.Random(3);
```

## Game Flow

1. **Start**: Call `RunnerGameManager.Instance.StartGame()`
2. **Playing**: Enemies spawn, player strafes to avoid
3. **Hit**: Player collides with enemy → `RegisterPlayerHit()` → Game Over
4. **Score**: Points for time survived + enemies passed
5. **Restart**: Call `RunnerGameManager.Instance.RestartGame()`

## Events

Subscribe to these events for custom behavior:

```csharp
// Game Manager Events
RunnerGameManager.Instance.OnGameStateChanged += (state) => { };
RunnerGameManager.Instance.OnScoreChanged += (score) => { };
RunnerGameManager.Instance.OnEnemyDefeated += (count) => { };
RunnerGameManager.Instance.OnPlayerHit += () => { };

// Player Events
playerController.OnLaneChanged += (lane) => { };
playerController.OnStrafeStarted += () => { };
playerController.OnStrafeCompleted += () => { };

// Enemy Events
enemy.OnEnemyDestroyed += (enemy) => { };
enemy.OnEnemyReachedEnd += (enemy) => { };
```

## Difficulty Progression

The spawner automatically increases difficulty:
- **Spawn Interval**: Decreases over time (enemies spawn faster)
- **Game Speed**: Increases over time (enemies move faster)

Configure in RunnerEnemySpawner:
- **Interval Decrease Rate**: How fast spawn rate increases
- **Min Spawn Interval**: Fastest spawn rate
- **Difficulty Increase Time**: Seconds between difficulty bumps

## Tips

1. **Always ensure at least one safe lane** in each row
2. **Test lane positions** using RunnerLaneVisualizer
3. **Use object pooling** for better performance (enabled by default)
4. **Add visual feedback** for lane changes (particles, animations)
5. **Consider adding a brief invincibility** after being hit

## Extending the System

### Add New Enemy Types
1. Create new prefab with RunnerEnemy component
2. Customize speed, damage, animations
3. Assign to specific row patterns

### Add Power-ups
1. Use RunnerCollectible with different types
2. Implement power-up effects in RunnerGameManager

### Add Boss Encounters
1. Create special enemy with multiple hit points
2. Spawn at specific intervals or score thresholds
