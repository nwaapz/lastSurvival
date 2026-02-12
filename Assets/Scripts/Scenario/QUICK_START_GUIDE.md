# Quick Start Guide - Your First Evony-Style Level

## Goal
Create Level 1 where:
1. Narrator introduces the story (BaseBuilder scene)
2. Player builds a Barracks
3. Narrator warns of zombies
4. Scene transitions to Fight
5. Player defends against 3 waves
6. Level complete → Advance to Level 2

## Step-by-Step Setup

### Part 1: Create the Scenario Steps (ScriptableObjects)

#### 1. Create Introduction Dialogue
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Level1_Intro"
```
- Click to edit
- Add `NarrationLine` assets (you should already have these)
- Example lines:
  - "Welcome, Commander! Our city needs defenses."
  - "Start by building a Barracks to train soldiers."
- Set `advanceOnClick = true`
- Set `description = "Listen to the introduction"`

#### 2. Create Build Objective
```
Right-click in Project → Create → Scenario → Build Step
Name: "Level1_BuildBarracks"
```
- `targetBuildingId = "Barracks"` (must match your BuildingDefinition ID)
- `targetLevel = 1`
- `restrictInteractions = true` (player can only click Barracks)
- `description = "Build a Barracks (Level 1)"`

#### 3. Create Pre-Battle Dialogue
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Level1_BattleWarning"
```
- Add NarrationLine: "Zombies are approaching! Prepare for battle!"
- `description = "Prepare for combat"`

#### 4. Create Scene Transition
```
Right-click in Project → Create → Scenario → Scene Transition Step
Name: "Level1_TransitionToFight"
```
- `targetSceneName = "Fight"` (must match your scene name exactly)
- `autoComplete = true`
- `preTransitionDialogue = null` (we already showed dialogue in step 3)
- `description = "Moving to battlefield..."`

#### 5. Create Battle Objective
```
Right-click in Project → Create → Scenario → Wave Step
Name: "Level1_FirstBattle"
```
- `initialZombies = 5`
- `zombiesIncrease = 2`
- `wavesInLevel = 3`
- `description = "Defend against 3 waves of zombies"`

#### 6. Create Victory Dialogue
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Level1_Victory"
```
- Add NarrationLine: "Well done, Commander! The city is safe... for now."
- `description = "Victory!"`

#### 7. Create Return Transition
```
Right-click in Project → Create → Scenario → Scene Transition Step
Name: "Level1_ReturnToBase"
```
- `targetSceneName = "basebuilder"`
- `autoComplete = true`
- `description = "Returning to base..."`

### Part 2: Create the Level Config

```
Right-click in Project → Create → Scenario → Level Scenario Config
Name: "Level_1_Scenario"
```

Configure:
- `levelNumber = 1`
- `steps` (drag in this order):
  1. Level1_Intro
  2. Level1_BuildBarracks
  3. Level1_BattleWarning
  4. Level1_TransitionToFight
  5. Level1_FirstBattle
  6. Level1_Victory
  7. Level1_ReturnToBase
- `advanceLevelOnComplete = true`
- `nextSceneOnComplete = ""` (leave empty, we handle it in step 7)

### Part 3: Setup ScenarioManager

#### In BaseBuilder Scene:
1. Create empty GameObject named "ScenarioManager"
2. Add component: `ScenarioManager`
3. Configure:
   - `Level Scenarios` → Size = 1
   - Element 0 = "Level_1_Scenario"
   - `autoStart = true`
   - `persistAcrossScenes = true`

4. Add component: `ScenarioDebugger` (optional, for testing)
   - `showDebugUI = true`

### Part 4: Setup ObjectiveUI

#### In BaseBuilder Scene:
1. Find your Canvas
2. Create UI → Panel (name it "ObjectivePanel")
3. Position it (e.g., top-left corner)
4. Inside ObjectivePanel, create:
   - TextMeshProUGUI named "ObjectiveText" (larger font, main objective)
   - TextMeshProUGUI named "StepCounterText" (smaller font, "Step 2/7")

5. Add component to ObjectivePanel: `ObjectiveUI`
6. Assign references:
   - `objectivePanel = ObjectivePanel` (itself)
   - `objectiveText = ObjectiveText`
   - `stepCounterText = StepCounterText`
   - `hideWhenNoObjective = false` (keep visible)

#### In Fight Scene:
1. Repeat the same ObjectiveUI setup
2. ScenarioManager will persist from BaseBuilder, so ObjectiveUI will automatically connect

### Part 5: Testing

1. **Start the game** in BaseBuilder scene
2. **Check console** for:
   ```
   [ScenarioManager] Starting scenario for level 1 at step 1
   [ScenarioManager] Starting step 1/7: Level1_Intro
   ```

3. **Press F1** to open debug window (if you added ScenarioDebugger)

4. **Test flow**:
   - Click to advance dialogue
   - Build Barracks → Step auto-completes
   - Click through battle warning
   - Scene loads to Fight
   - Waves spawn → Defend
   - Victory dialogue → Return to base

5. **Debug shortcuts** (if using ScenarioDebugger):
   - `F1`: Toggle debug window
   - `N`: Skip current step
   - `R`: Reset scenario progress

## Common Issues

### "No scenario found for level 1"
- Check that `Level_1_Scenario.levelNumber = 1`
- Verify it's assigned to ScenarioManager's `levelScenarios` list

### "Target scene name is empty"
- Check SceneTransitionStep has `targetSceneName` set
- Verify scene name matches Build Settings exactly (case-sensitive)

### ObjectiveUI not showing
- Check ObjectivePanel is active in hierarchy
- Verify ObjectiveUI component has all references assigned
- Check ScenarioManager.Instance is not null (console)

### Scene transition doesn't work
- Verify both scenes are in Build Settings (File → Build Settings)
- Check scene names match exactly: "Fight" and "basebuilder"

### Barracks won't complete
- Check `targetBuildingId` matches your BuildingDefinition.Id exactly
- Verify BuildingProgressManager is in the scene
- Check console for BuildingProgressManager events

## Next Steps

Once Level 1 works:

1. **Create Level 2**: 
   - New LevelScenarioConfig with `levelNumber = 2`
   - Different buildings, harder waves
   - Add to ScenarioManager's `levelScenarios` at index 1

2. **Add more step types**:
   - Create custom steps by inheriting from `ScenarioStep`
   - Examples: CollectResourcesStep, TrainUnitsStep, etc.

3. **Polish UI**:
   - Add animations to ObjectiveUI
   - Create a nicer narrator panel
   - Add sound effects on step completion

4. **Expand narration**:
   - Create more NarrationLine assets
   - Add different character portraits
   - Write branching dialogue (if needed)

## Example Level 2 Scenario

```
Level 2 Flow:
1. Intro dialogue: "More zombies are coming!"
2. Build objective: Upgrade Barracks to Level 2
3. Build objective: Build Watchtower
4. Transition to Fight
5. Wave objective: 5 waves (harder)
6. Victory dialogue
7. Return to base
```

Create `Level_2_Scenario` with `levelNumber = 2` and assign to ScenarioManager.

---

**You're all set!** Your Evony-style scenario system is ready. Start by creating Level 1 following this guide, then expand from there.
