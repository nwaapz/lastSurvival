# Scenario System - Evony-Style Linear Story

## Overview
This scenario system creates a linear, story-driven experience like Evony where:
- **Narrator guides the player** through missions with character portraits and dialogue
- **Alternates between scenes**: BaseBuilder (building upgrades) and Fight (combat)
- **Story progression is linear**: Player must complete specific tasks to advance
- **Progress is saved**: Player can resume where they left off

## Core Components

### 1. ScenarioManager (Singleton)
- **Location**: `Assets/Scripts/Scenario/ScenarioManager.cs`
- **Purpose**: Orchestrates the entire scenario system
- **Features**:
  - Persists across scene loads (DontDestroyOnLoad)
  - Loads level-specific scenarios from `LevelScenarioConfig`
  - Saves progress to `SaveManager`
  - Fires events when steps change (`OnStepChanged`, `OnScenarioComplete`)

### 2. ScenarioStep (Abstract ScriptableObject)
- **Location**: `Assets/Scripts/Scenario/ScenarioStep.cs`
- **Purpose**: Base class for all scenario steps
- **Key Fields**:
  - `description`: Text shown to player in ObjectiveUI
  - `isBlocking`: Whether step must complete before advancing
- **Override Methods**:
  - `OnEnter()`: Called when step starts
  - `UpdateStep()`: Called every frame, return `true` when complete
  - `OnExit()`: Called when step ends

### 3. LevelScenarioConfig (ScriptableObject)
- **Location**: `Assets/Scripts/Scenario/LevelScenarioConfig.cs`
- **Purpose**: Defines the complete scenario for a specific level
- **How to Create**: Right-click → Create → Scenario → Level Scenario Config
- **Fields**:
  - `levelNumber`: Which level this scenario is for (1, 2, 3...)
  - `steps`: Ordered list of ScenarioSteps to complete
  - `advanceLevelOnComplete`: Auto-advance to next level when done
  - `nextSceneOnComplete`: Optional scene to load after completion

## Available Step Types

### DialogueScenarioStep
- **Menu**: Create → Scenario → Dialogue Step
- **Purpose**: Show narrator dialogue with character portraits
- **Fields**:
  - `lines`: List of `NarrationLine` assets
  - `advanceOnClick`: Player clicks to advance dialogue

### BuildObjectiveStep
- **Menu**: Create → Scenario → Build Step
- **Purpose**: Wait for player to build/upgrade a specific building
- **Fields**:
  - `targetBuildingId`: ID of building to build
  - `targetLevel`: Required level
  - `restrictInteractions`: Only allow clicking target building

### MoveObjectiveStep
- **Menu**: Create → Scenario → Move Step
- **Purpose**: Wait for player to move to a location
- **Fields**:
  - `targetPosition`: World position to reach
  - `radius`: How close player must get
  - `playerTag`: Tag to find player object

### WaveScenarioStep
- **Menu**: Create → Scenario → Wave Step
- **Purpose**: Spawn zombie waves and wait for completion
- **Fields**:
  - `initialZombies`: Starting zombie count
  - `zombiesIncrease`: Zombies added per wave
  - `wavesInLevel`: Total waves to complete

### SceneTransitionStep ⭐ NEW
- **Menu**: Create → Scenario → Scene Transition Step
- **Purpose**: Switch between Fight and BaseBuilder scenes
- **Fields**:
  - `targetSceneName`: Scene to load (e.g., "Fight" or "basebuilder")
  - `autoComplete`: Complete immediately after loading
  - `preTransitionDialogue`: Optional narration before transition

### CameraMoveStep ⭐ NEW
- **Menu**: Create → Scenario → Camera Move Step
- **Purpose**: Smoothly move camera to show important locations (Port, Factory, etc.)
- **Fields**:
  - `targetLocationId`: Name of NamedLocation to move to
  - `movementDuration`: How long the camera takes to move
  - `completeImmediately`: Finish step instantly or wait for camera
  - `narrationDuringMove`: Optional dialogue during movement
- **Requires**: NamedLocation markers in your scene (see CAMERA_LOCATIONS_GUIDE.md)

## UI Components

### ObjectiveUI
- **Location**: `Assets/Scripts/Scenario/ObjectiveUI.cs`
- **Purpose**: Displays current objective to player
- **Setup**:
  1. Add to Canvas in both Fight and BaseBuilder scenes
  2. Assign UI references:
     - `objectivePanel`: Parent GameObject to show/hide
     - `objectiveText`: TextMeshProUGUI for objective description
     - `stepCounterText`: TextMeshProUGUI for "Step 2/5"
  3. Automatically subscribes to `ScenarioManager` events

## How to Create a Level

### Example: Level 1 Scenario

1. **Create Dialogue Steps**
   ```
   Right-click → Create → Scenario → Dialogue Step
   Name: "Intro_Dialogue"
   - Add NarrationLine assets with character portraits and text
   ```

2. **Create Build Step**
   ```
   Right-click → Create → Scenario → Build Step
   Name: "Build_Barracks"
   - targetBuildingId: "Barracks"
   - targetLevel: 1
   - description: "Build a Barracks to train soldiers"
   ```

3. **Create Scene Transition**
   ```
   Right-click → Create → Scenario → Scene Transition Step
   Name: "Transition_To_Fight"
   - targetSceneName: "Fight"
   - preTransitionDialogue: (optional narration)
   - description: "Prepare for battle!"
   ```

4. **Create Wave Step**
   ```
   Right-click → Create → Scenario → Wave Step
   Name: "First_Battle"
   - initialZombies: 5
   - wavesInLevel: 3
   - description: "Defend against 3 waves of zombies"
   ```

5. **Create Level Config**
   ```
   Right-click → Create → Scenario → Level Scenario Config
   Name: "Level_1_Scenario"
   - levelNumber: 1
   - steps: [Intro_Dialogue, Build_Barracks, Transition_To_Fight, First_Battle]
   - advanceLevelOnComplete: true
   ```

6. **Assign to ScenarioManager**
   - Find ScenarioManager GameObject in your starting scene
   - Add "Level_1_Scenario" to `levelScenarios` list at index 0

## Scene Setup

### BaseBuilder Scene
1. Add `ScenarioManager` GameObject (if not already present)
2. Add `ObjectiveUI` to Canvas
3. Ensure `BuildingProgressManager` exists

### Fight Scene
1. Add `ObjectiveUI` to Canvas
2. Ensure `ZombieWaveManager` exists
3. ScenarioManager will persist from BaseBuilder scene

## Save System Integration

The scenario system automatically saves:
- **CurrentLevel**: Which level the player is on
- **CurrentScenarioStepIndex**: Which step in the scenario
- **ScenarioCompleted**: Whether current level is done

Progress is saved after each step completion.

## Example Flow

**Level 1 Story Flow:**
1. Player starts in BaseBuilder scene
2. Narrator appears: "Welcome, Commander! Build a Barracks."
3. Player clicks to dismiss dialogue
4. Objective UI shows: "Build a Barracks"
5. Player builds Barracks → Step completes
6. Narrator: "Zombies approaching! Prepare for battle!"
7. Scene transitions to Fight
8. Objective UI: "Defend against 3 waves"
9. Player fights waves → Level complete
10. SaveManager advances to Level 2
11. Next level scenario loads

## Tips

- **Use `description` field**: This text appears in ObjectiveUI
- **Test with SaveManager**: Use SaveManagerEditor to reset progress
- **Scene names must match**: "Fight" and "basebuilder" in Build Settings
- **One ScenarioManager**: It persists across scenes (DontDestroyOnLoad)
- **Dialogue timing**: Add delays in DialogueStep if needed

## Troubleshooting

**Scenario doesn't start:**
- Check ScenarioManager has `autoStart = true`
- Verify LevelScenarioConfig is assigned to ScenarioManager
- Check SaveManager.Data.CurrentLevel matches scenario levelNumber

**Scene transition fails:**
- Verify scene name spelling matches Build Settings
- Check SceneTransitionStep.targetSceneName is set

**Objective UI not updating:**
- Ensure ObjectiveUI is in the scene
- Check ScenarioManager.Instance is not null
- Verify UI references are assigned in Inspector
