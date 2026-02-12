# Level 1 Onboarding - Complete Setup Guide

## Your Scenario Flow
1. **Camera moves to Castle**
2. **Soldier walks into Castle**
3. **Narration conversation** about zombies near the Port
4. **"CLASH" button appears at Port**
5. **Player clicks button** → Transitions to Fight scene

---

## Part 1: Scene Setup (5 minutes)

### Step 1.1: Setup Your Locations

You already have 3 transforms. Add **NamedLocation** to each:

#### Castle Location
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

#### Port Location
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

#### Blacksmith Location (for later)
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

### Step 1.2: Setup Your Soldier/Hero

Make sure your soldier GameObject has:
```
✓ Tag: "Player" (or create a "Soldier" tag)
✓ Component: HeroClickToMoveBaseBuilder
✓ Component: HeroMovementBaseBuilder
✓ NavMeshAgent (if using NavMesh)
```

### Step 1.3: Create ScenarioManager

```
1. Create Empty GameObject in BaseBuilder scene
2. Name it: "ScenarioManager"
3. Add Component → ScenarioManager
4. Configure:
   - Auto Start: ✓
   - Persist Across Scenes: ✓
5. (Optional) Add Component → ScenarioDebugger
```

### Step 1.4: Create ObjectiveUI

```
1. Find your Canvas in BaseBuilder scene
2. Create UI → Panel (name: "ObjectivePanel")
3. Position it at top-left corner
4. Inside ObjectivePanel, create:
   - UI → Text - TextMeshPro (name: "ObjectiveText")
   - UI → Text - TextMeshPro (name: "StepCounterText")
5. Select ObjectivePanel
6. Add Component → ObjectiveUI
7. Assign:
   - Objective Panel: ObjectivePanel
   - Objective Text: ObjectiveText
   - Step Counter Text: StepCounterText
```

---

## Part 2: Create Narration Lines (5 minutes)

You need to create NarrationLine assets for the conversation.

### Narration 1: Soldier Reports
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_SoldierReport"

Configure:
- Character Name: [Your soldier character]
- Pose Type: [Serious/Alert pose]
- Message: "Commander! Scouts report zombies gathering near the Port!"
```

### Narration 2: Commander Responds
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_CommanderResponse"

Configure:
- Character Name: [Your commander character]
- Pose Type: [Determined pose]
- Message: "We must defend the Port at all costs. Prepare for battle!"
```

### Narration 3: Call to Action
```
Right-click in Project → Create → Narration → Narration Line
Name: "Narration_CallToAction"

Configure:
- Character Name: [Your commander character]
- Pose Type: [Pointing/Commanding pose]
- Message: "Move to the Port immediately!"
```

---

## Part 3: Create Scenario Steps (10 minutes)

Now create each step as a ScriptableObject:

### Step 1: Camera Moves to Castle
```
Right-click in Project → Create → Scenario → Camera Move Step
Name: "Step1_ShowCastle"

Configure:
- Target Location Id: "Castle"
- Movement Duration: 1.5
- Complete Immediately: false (wait for camera)
- Narration During Move: (leave empty)
- Description: "Camera moves to the Castle"
```

### Step 2: Soldier Walks into Castle
```
Right-click in Project → Create → Scenario → Move Unit to Location Step
Name: "Step2_SoldierEntersCastle"

Configure:
- Unit Tag: "Player" (or "Soldier" if you created that tag)
- Target Location Id: "Castle"
- Arrival Radius: 1.0
- Camera Follows Unit: false
- Description: "Soldier enters the Castle"
```

### Step 3: Conversation Part 1 - Soldier Reports
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step3_SoldierReport"

Configure:
- Lines: [Drag "Narration_SoldierReport" here]
- Advance On Click: true
- Description: "Soldier reports zombie threat"
```

### Step 4: Conversation Part 2 - Commander Responds
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step4_CommanderResponse"

Configure:
- Lines: [Drag "Narration_CommanderResponse" here]
- Advance On Click: true
- Description: "Commander responds"
```

### Step 5: Conversation Part 3 - Call to Action
```
Right-click in Project → Create → Scenario → Dialogue Step
Name: "Step5_CallToAction"

Configure:
- Lines: [Drag "Narration_CallToAction" here]
- Advance On Click: true
- Description: "Commander orders action"
```

### Step 6: Camera Moves to Port
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

### Step 7: Show CLASH Button at Port
```
Right-click in Project → Create → Scenario → Show Button at Location Step
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

## Part 4: Create Level 1 Scenario Config (2 minutes)

### Create the Config
```
Right-click in Project → Create → Scenario → Level Scenario Config
Name: "Level_1_Scenario"
```

### Configure It
```
Level Number: 1
Advance Level On Complete: true
Next Scene On Complete: (leave empty, handled by button)

Steps (drag in this exact order):
1. Step1_ShowCastle
2. Step2_SoldierEntersCastle
3. Step3_SoldierReport
4. Step4_CommanderResponse
5. Step5_CallToAction
6. Step6_ShowPort
7. Step7_ClashButton
```

---

## Part 5: Assign to ScenarioManager (1 minute)

```
1. Select ScenarioManager GameObject in BaseBuilder scene
2. In Inspector, find "Level Scenarios"
3. Set Size = 1
4. Drag "Level_1_Scenario" into Element 0
```

---

## Part 6: Test Your Scenario! 🎮

### Press Play

You should see this sequence:

1. ✓ Camera smoothly moves to Castle
2. ✓ Soldier walks into Castle
3. ✓ Narration popup: "Commander! Scouts report zombies..."
4. ✓ Click to advance
5. ✓ Narration popup: "We must defend the Port..."
6. ✓ Click to advance
7. ✓ Narration popup: "Move to the Port immediately!"
8. ✓ Click to advance
9. ✓ Camera moves to Port
10. ✓ Red "CLASH!" button appears at Port
11. ✓ Click button → Fight scene loads

### Debug Controls (if you added ScenarioDebugger)

- **F1**: Toggle debug window
- **N**: Skip current step
- **R**: Reset scenario progress

---

## Troubleshooting

### Camera doesn't move
- Check that NamedLocation components exist on Castle and Port
- Verify locationId matches exactly: "Castle" and "Port"
- Check CameraHelper exists in scene

### Soldier doesn't move
- Verify soldier has tag "Player" (or whatever you set in Step2)
- Check HeroClickToMoveBaseBuilder component exists
- Ensure NavMesh is baked (if using NavMesh)
- Check Castle NamedLocation exists

### Narration doesn't show
- Verify Narration_manager exists in scene
- Check NarrationLine assets are assigned to DialogueSteps
- Ensure narration popup UI is setup

### Button doesn't appear
- Check Port NamedLocation exists with locationId "Port"
- Verify Canvas exists in scene
- Check console for errors

### Scene doesn't transition
- Verify "Fight" scene is in Build Settings
- Check scene name matches exactly (case-sensitive)
- Ensure ScenarioManager has "Persist Across Scenes" enabled

---

## What Happens Next?

After clicking CLASH:
1. Fight scene loads
2. ScenarioManager persists (DontDestroyOnLoad)
3. Player fights zombies
4. On victory/defeat, can return to BaseBuilder
5. Level 2 scenario begins (if you create it)

---

## Next Steps

1. **Create Level 2**: More complex scenario with multiple objectives
2. **Add more narration**: Expand the story
3. **Create more step types**: Custom objectives for your game
4. **Polish UI**: Better button visuals, animations

---

## Quick Checklist

Scene Setup:
- [ ] Castle has NamedLocation (locationId: "Castle")
- [ ] Port has NamedLocation (locationId: "Port")
- [ ] Soldier has tag "Player" and movement components
- [ ] ScenarioManager GameObject exists
- [ ] ObjectiveUI on Canvas

Assets Created:
- [ ] 3 NarrationLine assets (soldier report, commander response, call to action)
- [ ] 7 Scenario Step assets (camera, move, dialogues, button)
- [ ] 1 Level_1_Scenario config

ScenarioManager:
- [ ] Level_1_Scenario assigned to Level Scenarios list
- [ ] Auto Start enabled
- [ ] Persist Across Scenes enabled

Build Settings:
- [ ] "Fight" scene added to build settings

You're ready to go! Press Play and watch your Evony-style onboarding come to life! 🎉
