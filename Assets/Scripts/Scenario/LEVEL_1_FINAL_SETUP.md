# Level 1 Onboarding - Final Setup Guide (with ServiceLocator)

## Overview
Complete step-by-step guide for setting up Level 1 scenario with:
- ✅ Character control by CharacterName enum
- ✅ Player input locking/unlocking
- ✅ ServiceLocator integration

---

## Part 1: Scene Setup (10 minutes)

### Step 1.1: Setup Locations

Add **NamedLocation** to your 3 location transforms:

#### Castle
```
1. Select your Castle transform
2. Add Component → NamedLocation
3. Configure:
   - Location Id: "Castle"
   - Display Name: "Castle"
   - Camera Offset: (0, 0, 0)
   - Camera Rotation: (0, 0, 0)
   - Show Gizmo: ✓
```

#### Port
```
1. Select your Port transform
2. Add Component → NamedLocation
3. Configure:
   - Location Id: "Port"
   - Display Name: "Port"
   - Camera Offset: (0, 0, 0)
   - Camera Rotation: (0, 0, 0)
   - Show Gizmo: ✓
```

#### Blacksmith
```
1. Select your Blacksmith transform
2. Add Component → NamedLocation
3. Configure:
   - Location Id: "Blacksmith"
   - Display Name: "Blacksmith"
   - Camera Offset: (0, 0, 0)
   - Camera Rotation: (0, 0, 0)
   - Show Gizmo: ✓
```

---

### Step 1.2: Setup Characters

For each character GameObject (Hero, Janet, Pedi, Commander):

```
1. Select character GameObject (e.g., Hero)
2. Verify it has these components:
   ✓ HeroMovementBaseBuilder
   ✓ HeroClickToMoveBaseBuilder
   ✓ NavMeshAgent

3. Add Component → CharacterController

4. Configure CharacterController:
   - Character Name: Hero (dropdown: Hero, Janet, Pedi, or Commander)
   - Player Control Enabled: ✓ (checked)
```

**Repeat for all characters:**
- Hero → characterName = Hero
- Janet → characterName = Janet
- Pedi → characterName = Pedi
- Commander → characterName = Commander

---

### Step 1.3: Setup ServiceLocator (IMPORTANT!)

#### Option A: ServiceLocator Already Exists

If you already have a ServiceLocator GameObject:

```
1. Select ServiceLocator GameObject
2. Find CharacterManager component (or create one - see below)
3. In ServiceLocator component, find "Services" array
4. Add CharacterManager to the array:
   - Increase Size by 1
   - Drag CharacterManager component into new slot
```

#### Option B: Create New ServiceLocator

If you don't have ServiceLocator yet:

```
1. Create Empty GameObject
2. Name it: "ServiceLocator"
3. Add Component → ServiceLocator
4. Add Component → CharacterManager
5. In ServiceLocator component:
   - Services → Size = 1
   - Element 0 → Drag CharacterManager component here
```

**CRITICAL:** CharacterManager must be in the ServiceLocator's Services array!

---

### Step 1.4: Setup ScenarioManager

```
1. Create Empty GameObject (if not exists)
2. Name it: "ScenarioManager"
3. Add Component → ScenarioManager
4. Configure:
   - Auto Start: ✓
   - Persist Across Scenes: ✓
   - Level Scenarios: (empty for now)

5. (Optional) Add Component → ScenarioDebugger
   - Allows F1 debug window and N to skip steps
```

---

### Step 1.5: Setup ObjectiveUI

```
1. Find your Canvas in BaseBuilder scene
2. Right-click Canvas → UI → Panel
3. Name it: "ObjectivePanel"
4. Position at top-left corner

5. Right-click ObjectivePanel → UI → Text - TextMeshPro
6. Name it: "ObjectiveText"
7. Configure:
   - Font Size: 20-24
   - Alignment: Left
   - Color: White

8. Right-click ObjectivePanel → UI → Text - TextMeshPro
9. Name it: "StepCounterText"
10. Configure:
    - Font Size: 14-16
    - Alignment: Left
    - Color: Gray

11. Select ObjectivePanel
12. Add Component → ObjectiveUI
13. Assign:
    - Objective Panel: ObjectivePanel (drag itself)
    - Objective Text: ObjectiveText
    - Step Counter Text: StepCounterText
    - Hide When No Objective: ✓
```

---

## Part 2: Create Narration Lines (5 minutes)

Create 3 NarrationLine ScriptableObjects:

### Narration 1: Soldier Report
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_SoldierReport"

Configure:
- Character Name: Hero (or your soldier character)
- Pose Type: Serious
- Message: "Commander! Scouts report zombies gathering near the Port!"
```

### Narration 2: Commander Response
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_CommanderResponse"

Configure:
- Character Name: Commander
- Pose Type: Determined
- Message: "We must defend the Port at all costs. Prepare for battle!"
```

### Narration 3: Call to Action
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_CallToAction"

Configure:
- Character Name: Commander
- Pose Type: Pointing
- Message: "Move to the Port immediately!"
```

---

## Part 3: Create Scenario Steps (15 minutes)

Create 8 ScriptableObject scenario steps:

### Step 0: Lock Player Control
```
Right-click in Project → Create → Scenario → Set Player Control Step
Name: "Step0_LockControl"

Configure:
- Enable Player Control: ✗ (UNCHECKED!)
- Apply To All Characters: ✓
- Complete Immediately: ✓
- Description: "Lock player input during intro"
```

### Step 1: Camera to Castle
```
Right-click in Project → Create → Scenario → Camera Move Step
Name: "Step1_ShowCastle"

Configure:
- Target Location Id: "Castle"
- Movement Duration: 1.5
- Complete Immediately: false
- Narration During Move: (leave empty)
- Description: "Camera moves to the Castle"
```

### Step 2: Hero Enters Castle
```
Right-click in Project → Create → Scenario → Move Character to Location Step
Name: "Step2_HeroEntersCastle"

Configure:
- Character To Move: Hero (DROPDOWN!)
- Target Location Id: "Castle"
- Arrival Radius: 1.0
- Camera Follows Unit: false
- Description: "Hero enters the Castle"
```

### Step 3: Soldier Reports
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step3_SoldierReport"

Configure:
- Lines: (Size = 1)
  - Element 0: Drag "Narration_SoldierReport" here
- Advance On Click: ✓
- Description: "Soldier reports zombie threat"
```

### Step 4: Commander Responds
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step4_CommanderResponse"

Configure:
- Lines: (Size = 1)
  - Element 0: Drag "Narration_CommanderResponse" here
- Advance On Click: ✓
- Description: "Commander responds"
```

### Step 5: Call to Action
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step5_CallToAction"

Configure:
- Lines: (Size = 1)
  - Element 0: Drag "Narration_CallToAction" here
- Advance On Click: ✓
- Description: "Commander orders action"
```

### Step 6: Camera to Port
```
Right-click in Project → Create → Scenario → Camera Move Step
Name: "Step6_ShowPort"

Configure:
- Target Location Id: "Port"
- Movement Duration: 2.0
- Complete Immediately: false
- Narration During Move: (leave empty)
- Description: "Camera moves to the Port"
```

### Step 7: Show CLASH Button
```
Right-click in Project → Create → Scenario → Show Button at Location Step
Name: "Step7_ClashButton"

Configure:
- Button Text: "CLASH!"
- Target Location Id: "Port"
- Scene To Load: "Fight"
- Complete On Click: ✓
- Screen Offset: (0, 50)
- Description: "Click CLASH to start battle"
```

---

## Part 4: Create Level Config (3 minutes)

### Create Level_1_Scenario

```
Right-click in Project → Create → Scenario → Level Scenario Config
Name: "Level_1_Scenario"

Configure:
- Level Number: 1
- Advance Level On Complete: ✓
- Next Scene On Complete: (leave empty)
- Steps: (Size = 8)
  - Element 0: Step0_LockControl
  - Element 1: Step1_ShowCastle
  - Element 2: Step2_HeroEntersCastle
  - Element 3: Step3_SoldierReport
  - Element 4: Step4_CommanderResponse
  - Element 5: Step5_CallToAction
  - Element 6: Step6_ShowPort
  - Element 7: Step7_ClashButton
```

**IMPORTANT:** Drag steps in this exact order!

---

## Part 5: Assign to ScenarioManager (1 minute)

```
1. Select ScenarioManager GameObject in BaseBuilder scene
2. In Inspector, find "Level Scenarios"
3. Set Size = 1
4. Drag "Level_1_Scenario" into Element 0
```

---

## Part 6: Verify Build Settings (1 minute)

```
1. File → Build Settings
2. Check that "Fight" scene is in the list
3. If not, drag Fight scene into "Scenes In Build"
4. Close Build Settings
```

---

## Part 7: Test Your Scenario! 🎮

### Press Play

You should see this exact sequence:

1. ✅ **Player control LOCKED** (try clicking - nothing happens!)
2. ✅ Camera smoothly moves to Castle (1.5 seconds)
3. ✅ **Hero automatically walks** into Castle (no player input!)
4. ✅ Narration popup: "Commander! Scouts report zombies..."
5. ✅ Click anywhere to advance
6. ✅ Narration popup: "We must defend the Port..."
7. ✅ Click to advance
8. ✅ Narration popup: "Move to the Port immediately!"
9. ✅ Click to advance
10. ✅ Camera smoothly moves to Port (2 seconds)
11. ✅ Red "CLASH!" button appears at Port location
12. ✅ Click button → Fight scene loads

### Debug Controls

If you added ScenarioDebugger:
- **F1**: Toggle debug window (shows current step, progress)
- **N**: Skip to next step
- **R**: Reset scenario to step 0

---

## Troubleshooting

### "CharacterManager not found!"
**Problem:** ServiceLocator can't find CharacterManager
**Solution:**
1. Check ServiceLocator GameObject exists
2. Check CharacterManager component is attached to ServiceLocator (or another GameObject)
3. **CRITICAL:** CharacterManager must be in ServiceLocator's "Services" array
4. Check console for "CharacterManager Initialized via ServiceLocator" message

### Player can still click during intro
**Problem:** Player control not locked
**Solution:**
1. Verify Step0_LockControl exists
2. Check "Enable Player Control" is UNCHECKED
3. Make sure Step0_LockControl is FIRST in Level_1_Scenario steps

### Hero doesn't move
**Problem:** Character not found or not setup correctly
**Solution:**
1. Check Hero GameObject has CharacterController component
2. Verify characterName is set to "Hero" (not Janet, Pedi, etc.)
3. Check Hero has HeroMovementBaseBuilder + HeroClickToMoveBaseBuilder
4. Ensure NavMesh is baked in scene
5. Check console for error messages

### Wrong character moves
**Problem:** Wrong character selected in step
**Solution:**
1. Open Step2_HeroEntersCastle
2. Check "Character To Move" dropdown
3. Should be "Hero", not another character

### Camera doesn't move
**Problem:** NamedLocation not found
**Solution:**
1. Check Castle and Port transforms have NamedLocation component
2. Verify locationId matches exactly: "Castle" and "Port" (case-sensitive!)
3. Check CameraHelper exists in scene

### Button doesn't appear
**Problem:** Port location not found or Canvas missing
**Solution:**
1. Check Port has NamedLocation with locationId = "Port"
2. Verify Canvas exists in scene
3. Check console for errors

### Scene doesn't transition
**Problem:** Fight scene not in build or wrong name
**Solution:**
1. File → Build Settings
2. Add "Fight" scene to build
3. Verify scene name in Step7_ClashButton matches exactly

---

## Complete Checklist

### Scene Objects
- [ ] Castle transform has NamedLocation (locationId: "Castle")
- [ ] Port transform has NamedLocation (locationId: "Port")
- [ ] Blacksmith transform has NamedLocation (locationId: "Blacksmith")
- [ ] Hero has CharacterController (characterName: Hero)
- [ ] Janet has CharacterController (characterName: Janet) - if exists
- [ ] Pedi has CharacterController (characterName: Pedi) - if exists
- [ ] Commander has CharacterController (characterName: Commander) - if exists

### ServiceLocator
- [ ] ServiceLocator GameObject exists
- [ ] CharacterManager component attached (to ServiceLocator or other GameObject)
- [ ] CharacterManager is in ServiceLocator's "Services" array
- [ ] Console shows "CharacterManager Initialized via ServiceLocator" on Play

### Managers
- [ ] ScenarioManager GameObject exists
- [ ] ScenarioManager has Auto Start enabled
- [ ] ScenarioManager has Persist Across Scenes enabled
- [ ] (Optional) ScenarioDebugger component added

### UI
- [ ] ObjectivePanel exists on Canvas
- [ ] ObjectiveText (TextMeshPro) inside ObjectivePanel
- [ ] StepCounterText (TextMeshPro) inside ObjectivePanel
- [ ] ObjectiveUI component on ObjectivePanel with references assigned

### Assets Created
- [ ] 3 NarrationLine assets (SoldierReport, CommanderResponse, CallToAction)
- [ ] 8 Scenario Step assets (Step0 through Step7)
- [ ] 1 Level_1_Scenario config

### Configuration
- [ ] Level_1_Scenario has all 8 steps in correct order
- [ ] Level_1_Scenario assigned to ScenarioManager's Level Scenarios
- [ ] Fight scene in Build Settings

### Testing
- [ ] Press Play - player can't click to move
- [ ] Camera moves to Castle automatically
- [ ] Hero walks to Castle automatically
- [ ] Narration appears and advances on click
- [ ] Camera moves to Port
- [ ] CLASH button appears
- [ ] Clicking CLASH loads Fight scene

---

## What's Next?

After Level 1 works:
1. **Create Level 2** with more complex objectives
2. **Add more characters** (Janet, Pedi, Commander movements)
3. **Create BuildObjectiveStep** scenarios (upgrade buildings)
4. **Add WaveScenarioStep** for fight scenes
5. **Polish UI** with animations and effects

---

## Summary

You now have a complete Evony-style onboarding with:
- ✅ Cinematic camera movements
- ✅ Scripted character movements
- ✅ Player input locking during cutscenes
- ✅ Character identification by enum (not tags!)
- ✅ ServiceLocator integration
- ✅ Narration system
- ✅ Scene transitions

Press Play and enjoy your scenario! 🎉
