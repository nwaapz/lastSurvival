# Quick Camera Setup - 5 Minute Guide

## 1. Mark Your Locations (2 minutes)

In your **BaseBuilder** scene:

```
For each important building/area:
1. Create Empty GameObject
2. Name it: "Location_[Name]" (e.g., "Location_Port")
3. Position it at the building/area center
4. Add Component → NamedLocation
5. Set locationId: "Port" (match your building IDs!)
6. Adjust Camera Offset: (0, 10, -10) - tweak until it looks good
```

**Quick locations to create:**
- Port
- Factory  
- Colosseum
- Barracks
- TownHall
- Market

## 2. Use in Scenarios (3 minutes)

### Option A: Just Move Camera
```
Create → Scenario → Camera Move Step
- targetLocationId: "Port"
- movementDuration: 1.5
- description: "Looking at the harbor..."
```

### Option B: Build Task (Auto Camera!)
```
Create → Scenario → Build Step
- targetBuildingId: "Barracks"
- moveCameraToBuilding: ✓ (camera moves automatically!)
- description: "Build a Barracks"
```

### Option C: Camera + Dialogue Together
```
Create → Scenario → Camera Move Step
- targetLocationId: "Factory"
- completeImmediately: true
- narrationDuringMove: [Your NarrationLine]
```

## 3. Test It

1. Play the scene
2. Camera should smoothly move to locations
3. Use **F1** (if ScenarioDebugger added) to skip steps
4. Press **N** to advance

## Done! 🎉

Your scenario now guides the player's view like Evony!

---

## Common Pattern: Evony-Style Tutorial

```
Level 1 Scenario Steps:
1. DialogueStep - "Welcome!"
2. CameraMoveStep - Show Barracks area
3. DialogueStep - "Build this!"
4. BuildObjectiveStep - Build Barracks (camera already there)
5. CameraMoveStep - Return to center
6. SceneTransitionStep - Go to Fight
```

## Pro Tips

- **Match IDs**: Use same name for locationId and buildingId
- **Good offset**: (0, 10, -10) works for most cases
- **Duration**: 1.5s is smooth, 0.5s is quick, 2.5s is cinematic
- **Gizmos**: Select NamedLocation to see camera preview in Scene view
