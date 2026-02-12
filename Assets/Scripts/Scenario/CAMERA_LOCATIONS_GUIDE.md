# Camera Location System - Setup Guide

## Overview
The camera location system allows your scenario to guide the player's view to important places like the Port, Factory, Colosseum, etc. This is essential for Evony-style tutorials where the narrator says "Build a Barracks" and the camera smoothly moves to show where the Barracks is.

## Components

### 1. NamedLocation
Marks a spot in your world with a name so the camera can find it.

### 2. CameraHelper (Enhanced)
Now supports smooth movement to named locations.

### 3. CameraMoveStep
Scenario step that moves the camera to a location.

### 4. BuildObjectiveStep (Enhanced)
Automatically moves camera to target building when step starts.

---

## Setup: Marking Locations in BaseBuilder Scene

### Step 1: Create Location Markers

For each important area in your base (Port, Factory, Colosseum, Barracks, etc.):

1. **Create empty GameObject**
   - Right-click in Hierarchy → Create Empty
   - Name it descriptively (e.g., "Location_Port")

2. **Position it** where you want the camera to focus
   - Move it to the center of the area you want to show
   - Y position doesn't matter much (camera will use offset)

3. **Add NamedLocation component**
   - Select the GameObject
   - Add Component → Named Location

4. **Configure NamedLocation**:
   ```
   Location Id: "Port" (must be unique, used in scenario steps)
   Display Name: "Harbor District" (optional, shown to player)
   Camera Offset: (0, 10, -10) (adjust to get good view angle)
   Camera Rotation: (45, 0, 0) (adjust for best view)
   Show Gizmo: ✓ (helps you see it in Scene view)
   ```

5. **Adjust camera preview**
   - Select the Location GameObject
   - In Scene view, you'll see:
     - Cyan sphere at location center
     - Yellow line showing camera offset
     - Yellow cube showing camera position
   - Adjust `Camera Offset` and `Camera Rotation` until the view looks good

### Step 2: Mark All Important Locations

Create NamedLocations for:
- **Port** (locationId: "Port")
- **Factory** (locationId: "Factory")
- **Colosseum** (locationId: "Colosseum")
- **Barracks** (locationId: "Barracks")
- **TownHall** (locationId: "TownHall")
- **Market** (locationId: "Market")
- Any other important buildings or areas

**Tip**: For buildings, place the NamedLocation at the building's position and use the building's ID as the locationId. This way `BuildObjectiveStep` will automatically find it.

---

## Usage in Scenarios

### Example 1: Move Camera Then Show Dialogue

```
Step 1: CameraMoveStep
- targetLocationId: "Port"
- movementDuration: 1.5
- completeImmediately: false
- description: "Looking at the harbor..."

Step 2: DialogueStep
- Narrator: "This is the Port. Build it to unlock trade routes!"
```

### Example 2: Dialogue + Camera Movement Together

```
CameraMoveStep:
- targetLocationId: "Factory"
- movementDuration: 2.0
- completeImmediately: true (moves in background)
- narrationDuringMove: (NarrationLine asset)
  - "Let me show you the Factory district..."
```

### Example 3: Build Objective with Auto Camera

```
BuildObjectiveStep:
- targetBuildingId: "Barracks"
- targetLevel: 1
- moveCameraToBuilding: ✓ (camera moves automatically!)
- cameraMoveTime: 1.5
- description: "Build a Barracks"
```

The camera will automatically move to show the Barracks location.

---

## Complete Level 1 Example with Camera

### Scenario Flow:
1. Intro dialogue (camera at default position)
2. Camera moves to Barracks area
3. Narrator explains Barracks
4. Player builds Barracks (camera already there)
5. Camera moves to Town Center
6. Narrator warns of zombies
7. Transition to Fight scene

### ScriptableObject Setup:

#### Step 1: Intro Dialogue
```
DialogueStep: "Level1_Intro"
- lines: [Welcome message]
- description: "Welcome to your city!"
```

#### Step 2: Move to Barracks
```
CameraMoveStep: "Level1_ShowBarracks"
- targetLocationId: "Barracks"
- movementDuration: 2.0
- completeImmediately: false (wait for camera)
- description: "Looking at the training grounds..."
```

#### Step 3: Explain Barracks
```
DialogueStep: "Level1_ExplainBarracks"
- lines: ["This is where you train soldiers..."]
- description: "Learn about the Barracks"
```

#### Step 4: Build Barracks
```
BuildObjectiveStep: "Level1_BuildBarracks"
- targetBuildingId: "Barracks"
- targetLevel: 1
- moveCameraToBuilding: false (already there from step 2!)
- restrictInteractions: true
- description: "Build the Barracks"
```

#### Step 5: Return to Town Center
```
CameraMoveStep: "Level1_ReturnToCenter"
- targetLocationId: "TownHall"
- movementDuration: 1.5
- completeImmediately: false
- description: "Returning to town center..."
```

#### Step 6: Battle Warning
```
DialogueStep: "Level1_BattleWarning"
- lines: ["Zombies approaching!"]
- description: "Prepare for battle"
```

#### Step 7: Transition to Fight
```
SceneTransitionStep: "Level1_ToFight"
- targetSceneName: "Fight"
- description: "Moving to battlefield..."
```

---

## Tips & Best Practices

### Camera Offset Guidelines
For typical isometric/top-down view:
- **X offset**: 0 (centered on location)
- **Y offset**: 8-15 (height above location)
- **Z offset**: -8 to -15 (behind location)
- **Rotation**: (30-60, 0, 0) for isometric look

### Location Naming Conventions
- Use **PascalCase** or **lowercase** consistently
- Match building IDs exactly for auto-camera in BuildObjectiveStep
- Examples: "Port", "Factory", "Colosseum", "TownHall"

### Movement Duration
- **Quick pan**: 0.5-1.0 seconds (nearby locations)
- **Normal**: 1.5-2.0 seconds (most transitions)
- **Cinematic**: 2.5-3.0 seconds (dramatic reveals)

### Complete Immediately?
- **true**: Camera moves in background, step finishes instantly
  - Use when: Dialogue plays during movement
- **false**: Wait for camera to reach destination
  - Use when: You want player to see the location before continuing

### Combining with Dialogue
**Option A**: Sequential (camera then dialogue)
```
1. CameraMoveStep (completeImmediately: false)
2. DialogueStep
```

**Option B**: Simultaneous (dialogue during movement)
```
1. CameraMoveStep (completeImmediately: true, narrationDuringMove: set)
```

---

## Testing Camera Locations

### In Scene View:
1. Select a NamedLocation GameObject
2. You'll see gizmos showing:
   - Location center (cyan sphere)
   - Camera position (yellow cube)
   - Camera view direction (green ray when selected)
3. Adjust offsets until the view looks good

### In Play Mode:
1. Add `ScenarioDebugger` to ScenarioManager
2. Press **F1** to open debug window
3. Press **N** to skip to next step
4. Watch camera movements in real-time

### Manual Testing:
Add this to any script to test camera movement:
```csharp
// Test moving to Port
if (Input.GetKeyDown(KeyCode.P))
{
    CameraHelper.Instance.MoveToLocation("Port", 2.0f);
}
```

---

## Troubleshooting

### "Location 'Port' not found!"
- Check that NamedLocation exists in the scene
- Verify `locationId` matches exactly (case-sensitive)
- Make sure NamedLocation component is enabled

### Camera doesn't move smoothly
- Check `movementDuration` isn't too small (< 0.5s)
- Verify CameraHelper is in the scene and enabled
- Check console for errors

### Camera moves to wrong position
- Select the NamedLocation in Scene view
- Adjust `cameraOffset` to change camera position
- Adjust `cameraRotation` to change viewing angle
- Use the gizmos as a preview

### BuildObjectiveStep doesn't move camera
- Set `moveCameraToBuilding = true`
- Create a NamedLocation with `locationId` matching `targetBuildingId`
- OR: The system will fallback to finding the BuildingView directly

---

## Advanced: Custom Camera Positions

If you need precise control, use `CameraHelper.MoveToPosition()` directly:

```csharp
Vector3 targetPos = new Vector3(10, 15, -10);
Quaternion targetRot = Quaternion.Euler(45, 0, 0);
float targetZoom = 12f;

CameraHelper.Instance.MoveToPosition(targetPos, targetRot, targetZoom, 2.0f, () => {
    Debug.Log("Camera movement complete!");
});
```

---

## Summary Checklist

- [ ] Create NamedLocation GameObjects for all important areas
- [ ] Configure camera offsets and rotations (use gizmos!)
- [ ] Use CameraMoveStep to guide player's view
- [ ] Enable `moveCameraToBuilding` in BuildObjectiveStep
- [ ] Test camera movements in Play mode
- [ ] Adjust durations for smooth flow

Your scenario will now guide the player's camera like Evony! 🎥
