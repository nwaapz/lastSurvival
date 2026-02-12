# Level 1 Onboarding - UPDATED with Character System

## What Changed?
- ✅ Characters identified by **CharacterName enum** (Hero, Janet, Pedi, Commander)
- ✅ Player control can be **locked/unlocked** by scenario
- ✅ No more using tags - use dropdown to select character!

---

## Part 1: Scene Setup (7 minutes)

### Step 1.1: Setup Locations (Same as before)

Add **NamedLocation** to your 3 transforms:
- Castle: locationId = "Castle"
- Port: locationId = "Port"
- Blacksmith: locationId = "Blacksmith"

### Step 1.2: Setup Characters (NEW!)

For each character GameObject (Hero, Janet, Pedi, Commander):

```
1. Select character GameObject
2. Verify it has:
   ✓ HeroMovementBaseBuilder
   ✓ HeroClickToMoveBaseBuilder
   ✓ NavMeshAgent
3. Add Component → CharacterController
4. Configure:
   - Character Name: Hero (or Janet, Pedi, Commander)
   - Player Control Enabled: ✓
```

### Step 1.3: Create CharacterManager (NEW!)

```
1. Create Empty GameObject
2. Name it: "CharacterManager"
3. Add Component → CharacterManager
```

### Step 1.4: Create ScenarioManager (Same as before)

```
1. Create Empty GameObject: "ScenarioManager"
2. Add Component → ScenarioManager
3. Configure:
   - Auto Start: ✓
   - Persist Across Scenes: ✓
4. (Optional) Add Component → ScenarioDebugger
```

### Step 1.5: Create ObjectiveUI (Same as before)

Create UI panel with ObjectiveUI component.

---

## Part 2: Create Scenario Steps (12 minutes)

### Step 0: Lock Player Control (NEW!)

```
Right-click → Create → Scenario → Set Player Control Step
Name: "Step0_LockControl"

Configure:
- Enable Player Control: ✗ (UNCHECKED!)
- Apply To All Characters: ✓
- Complete Immediately: ✓
- Description: "Lock player input during intro"
```

### Step 1: Camera Moves to Castle

```
Right-click → Create → Scenario → Camera Move Step
Name: "Step1_ShowCastle"

Configure:
- Target Location Id: "Castle"
- Movement Duration: 1.5
- Complete Immediately: false
- Description: "Camera moves to the Castle"
```

### Step 2: Hero Walks into Castle (UPDATED!)

```
Right-click → Create → Scenario → Move Character to Location Step
Name: "Step2_HeroEntersCastle"

Configure:
- Character To Move: Hero (DROPDOWN - select Hero!)
- Target Location Id: "Castle"
- Arrival Radius: 1.0
- Camera Follows Unit: false
- Description: "Hero enters the Castle"
```

### Step 3: Soldier Reports

```
Right-click → Create → Scenario → Dialogue Step
Name: "Step3_SoldierReport"

Configure:
- Lines: [Narration_SoldierReport]
- Advance On Click: true
- Description: "Soldier reports zombie threat"
```

### Step 4: Commander Responds

```
Right-click → Create → Scenario → Dialogue Step
Name: "Step4_CommanderResponse"

Configure:
- Lines: [Narration_CommanderResponse]
- Advance On Click: true
- Description: "Commander responds"
```

### Step 5: Call to Action

```
Right-click → Create → Scenario → Dialogue Step
Name: "Step5_CallToAction"

Configure:
- Lines: [Narration_CallToAction]
- Advance On Click: true
- Description: "Commander orders action"
```

### Step 6: Camera Moves to Port

```
Right-click → Create → Scenario → Camera Move Step
Name: "Step6_ShowPort"

Configure:
- Target Location Id: "Port"
- Movement Duration: 2.0
- Complete Immediately: false
- Description: "Camera moves to the Port"
```

### Step 7: Show CLASH Button

```
Right-click → Create → Scenario → Show Button at Location Step
Name: "Step7_ClashButton"

Configure:
- Button Text: "CLASH!"
- Target Location Id: "Port"
- Scene To Load: "Fight"
- Complete On Click: true
- Screen Offset: (0, 50)
- Description: "Click CLASH to start battle"
```

---

## Part 3: Create Level Config (2 minutes)

```
Right-click → Create → Scenario → Level Scenario Config
Name: "Level_1_Scenario"

Configure:
- Level Number: 1
- Advance Level On Complete: ✓
- Steps (drag in order):
  0. Step0_LockControl ← NEW!
  1. Step1_ShowCastle
  2. Step2_HeroEntersCastle
  3. Step3_SoldierReport
  4. Step4_CommanderResponse
  5. Step5_CallToAction
  6. Step6_ShowPort
  7. Step7_ClashButton
```

---

## Part 4: Assign to ScenarioManager (1 minute)

```
1. Select ScenarioManager GameObject
2. Level Scenarios → Size = 1
3. Element 0 = Level_1_Scenario
```

---

## Part 5: Test! 🎮

### Expected Flow:

1. ✓ **Player control LOCKED** (can't click to move)
2. ✓ Camera moves to Castle
3. ✓ **Hero automatically walks** into Castle
4. ✓ Narration: "Zombies near Port!"
5. ✓ Click to advance
6. ✓ Narration: "We must defend!"
7. ✓ Click to advance
8. ✓ Narration: "Move to Port!"
9. ✓ Click to advance
10. ✓ Camera moves to Port
11. ✓ Red "CLASH!" button appears
12. ✓ Click button → Fight scene loads

### Key Differences from Old System:

**OLD (Tag-based):**
- Used unitTag = "Player"
- Any GameObject with "Player" tag would move
- No control over player input

**NEW (CharacterName-based):**
- Use characterToMove = Hero (dropdown!)
- Specific character moves (Hero, Janet, Pedi, Commander)
- Player control locked during cutscene
- Player can't interfere with scripted movements

---

## Troubleshooting

### Player can still click during intro
- Check **Step0_LockControl** exists
- Verify **Enable Player Control** is UNCHECKED
- Make sure it's the FIRST step in scenario

### Hero doesn't move
- Check Hero has **CharacterController** component
- Verify **characterName** is set to "Hero"
- Check **CharacterManager** exists in scene
- Ensure NavMesh is baked

### Wrong character moves
- Check **Character To Move** dropdown in Step2
- Should be "Hero", not "Janet" or others

### Character moves but player can also click
- Player control wasn't locked
- Add **Step0_LockControl** at start

---

## Optional: Unlock Control Later

If you want to give player control after the intro:

```
Create → Scenario → Set Player Control Step
Name: "Step8_UnlockControl"

Configure:
- Enable Player Control: ✓ (CHECKED!)
- Apply To All Characters: ✓
- Description: "Player can now control characters"

Add to Level_1_Scenario after Step7_ClashButton
```

---

## Summary Checklist

Scene Objects:
- [ ] Castle, Port, Blacksmith have NamedLocation
- [ ] Hero has CharacterController (characterName = Hero)
- [ ] CharacterManager GameObject exists
- [ ] ScenarioManager GameObject exists
- [ ] ObjectiveUI on Canvas

Scenario Steps (8 total):
- [ ] Step0_LockControl (SetPlayerControlStep)
- [ ] Step1_ShowCastle (CameraMoveStep)
- [ ] Step2_HeroEntersCastle (MoveCharacterStep - Hero)
- [ ] Step3-5 (DialogueSteps)
- [ ] Step6_ShowPort (CameraMoveStep)
- [ ] Step7_ClashButton (ShowButtonAtLocationStep)

Level Config:
- [ ] Level_1_Scenario has all 8 steps in order
- [ ] Assigned to ScenarioManager

You're ready! Press Play and watch your character-controlled scenario! 🎉
