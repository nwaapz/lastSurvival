# BaseBuilder Scene Setup Guide

## Hero Movement System with NavMesh

### Components Created

1. **HeroMovementBaseBuilder.cs** - Core NavMesh movement component
2. **HeroClickToMoveBaseBuilder.cs** - Integrates movement with click manager
3. **BaseBuilderClickManager.cs** - Handles click/touch input (already created)

---

## Setup Instructions

### 1. Prepare the Scene

#### A. Bake NavMesh
1. Select all walkable ground objects in your scene
2. Mark them as **Navigation Static** (Inspector → Navigation → Navigation Static checkbox)
3. Open Navigation window: `Window → AI → Navigation`
4. Click **Bake** tab
5. Adjust settings:
   - Agent Radius: 0.5
   - Agent Height: 2.0
   - Max Slope: 45
   - Step Height: 0.4
6. Click **Bake** button

#### B. Setup Click Manager (if not done)
1. Create empty GameObject: "BaseBuilderClickManager"
2. Add `BaseBuilderClickManager` component
3. Add to ServiceLocator's services array
4. Configure LayerMask to include walkable layers

#### C. Setup Camera (for Top-Down View)
1. Rotate camera 90° on X-axis (for top-down NavMesh view)
2. Set camera to Orthographic
3. Add `CameraHelper` component to camera or separate GameObject
4. Configure CameraHelper:
   - **Camera Pan Button**: 1 (Right Click) - avoids conflict with click-to-move
   - **Pan Speed**: 10
   - **Zoom Speed**: 5
   - **Drag Threshold**: 15
   - **Min/Max Zoom**: Set based on your scene size
   - **Min/Max X and Z**: Set boundaries for your play area
   - **Fixed Y**: Camera height (e.g., 10)
5. Add CameraHelper to ServiceLocator's services array

---

### 2. Setup Hero Character

**Important**: Your sprites should also be rotated 90° on X-axis to match the camera/NavMesh setup.

#### A. Add Required Components
1. Select your Hero prefab/GameObject
2. Add components in this order:
   - `NavMeshAgent` (auto-added by HeroMovementBaseBuilder)
   - `HeroMovementBaseBuilder`
   - `HeroClickToMoveBaseBuilder`

#### B. Configure NavMeshAgent
- **Speed**: 3.5 (auto-set by movement script)
- **Angular Speed**: 120
- **Acceleration**: 8
- **Stopping Distance**: 0.1
- **Auto Braking**: True
- **Radius**: 0.5
- **Height**: 2.0
- **Base Offset**: 0 (adjust if hero floats/sinks)

#### C. Configure HeroMovementBaseBuilder
- **Move Speed**: 3.5
- **Rotation Speed**: 120
- **Stopping Distance**: 0.1
- **Animator**: Drag hero's Animator component here
- **Idle Animation Bool**: "Idle" (matches your animator parameter)
- **Walk Animation Bool**: "walk" (matches your animator parameter)
- **Run Animation Bool**: "run" (matches your animator parameter)
- **Run Speed Threshold**: 5.0 (speed above this triggers run animation)
- **Show Debug Logs**: True (for testing, disable later)
- **Show Path Gizmos**: True (shows path in Scene view)

#### D. Configure HeroClickToMoveBaseBuilder
- **Walkable Layer Mask**: Set to your ground/floor layers
- **Only Move On Walkable Layer**: True
- **Show Debug Logs**: True (for testing, disable later)

**Note**: Audio and visual markers removed. Use SFX manager for sounds if needed.

---

### 3. Animation Setup (Already Configured)

Your animator already has the required parameters:
- `Idle` (Bool) - Active when hero is not moving
- `walk` (Bool) - Active when hero is moving at normal speed
- `run` (Bool) - Active when hero speed exceeds threshold (5.0)

#### How It Works:
- **Idle**: True when velocity = 0, False when moving
- **Walk**: True when moving at speed < 5.0, False otherwise
- **Run**: True when moving at speed ≥ 5.0, False otherwise

The script automatically manages these states - no manual setup needed!

---

## Usage

### Basic Controls
- **Left Click**: Move hero to position (click-to-move)
- **Right Click + Drag**: Pan camera (configurable)
- **Mouse Wheel**: Zoom in/out
- **Touch (Mobile)**:
  - Single finger drag: Pan camera
  - Two finger pinch: Zoom
  - Tap: Move hero

### Scripting API

#### Move to Position
```csharp
// Get reference to movement component
var movement = heroObject.GetComponent<HeroMovementBaseBuilder>();

// Set destination
movement.SetDestination(targetPosition);

// Stop movement
movement.Stop();

// Check if can reach
if (movement.CanReachDestination(position))
{
    movement.SetDestination(position);
}
```

#### Using Click-to-Move Component
```csharp
var clickMove = heroObject.GetComponent<HeroClickToMoveBaseBuilder>();

// Enable/disable click-to-move
clickMove.SetClickToMoveEnabled(true);

// Manually move (not from click)
clickMove.MoveToPosition(targetPosition);
```

#### Subscribe to Events
```csharp
void Start()
{
    var movement = GetComponent<HeroMovementBaseBuilder>();
    
    movement.OnMovementStarted += HandleMovementStart;
    movement.OnMovementStopped += HandleMovementStop;
    movement.OnDestinationReached += HandleDestinationReached;
}

void HandleMovementStart()
{
    Debug.Log("Hero started moving!");
}

void HandleMovementStop()
{
    Debug.Log("Hero stopped!");
}

void HandleDestinationReached(Vector3 destination)
{
    Debug.Log($"Hero reached: {destination}");
}
```

---

## Testing

1. Enter Play mode
2. Click anywhere on the walkable ground
3. Hero should move to clicked position
4. Green sphere shows destination (if gizmos enabled)
5. Yellow line shows path (if gizmos enabled)
6. Red circle shows stopping distance

---

## Troubleshooting

### Hero doesn't move when clicked
- Check if NavMesh is baked (blue overlay in Scene view)
- Verify BaseBuilderClickManager exists and is initialized
- Check if clicked position is on NavMesh
- Enable debug logs to see what's happening

### Hero floats or sinks into ground
- Adjust NavMeshAgent's **Base Offset** parameter

### Hero moves through obstacles
- Ensure obstacles are marked as **Navigation Static**
- Re-bake NavMesh after adding obstacles

### Animations don't play
- Verify Animator component is assigned
- Check animation parameter names match exactly
- Ensure animator parameters exist in Animator Controller

### Click not detected
- Verify EventSystem exists in scene
- Check if UI is blocking clicks
- Ensure camera is tagged as "MainCamera"

### Camera panning interferes with movement
- Set CameraHelper's `cameraPanButton` to 1 (Right Click) or 2 (Middle Click)
- This prevents camera drag from blocking click-to-move

### Camera movement feels wrong
- Verify camera is rotated 90° on X-axis for top-down view
- Check that `fixedY` is set to appropriate height
- Adjust pan speed if movement is too fast/slow

---

## Advanced Features

### Change Speed at Runtime
```csharp
movement.SetSpeed(5.0f);
// or
movement.Speed = 5.0f;
```

### Dynamic Stopping Distance
```csharp
movement.SetStoppingDistance(2.0f);
```

### Check Remaining Distance
```csharp
float distance = movement.RemainingDistance;
if (distance < 1.0f)
{
    Debug.Log("Almost there!");
}
```

### Access NavMeshAgent Directly
```csharp
NavMeshAgent agent = movement.Agent;
agent.avoidancePriority = 50;
```

---

## Performance Tips

1. Disable debug logs in production
2. Disable path gizmos in production
3. Use obstacle avoidance only when needed
4. Keep NavMesh resolution reasonable (not too detailed)
5. Use NavMesh areas for different terrain types

---

## Integration with Other Systems

### With Narration System
```csharp
// Show dialogue, then move hero
Narration_manager.Instance.ShowNarrationLine(line);
heroMovement.SetDestination(npcPosition);
```

### With UI Buttons
```csharp
public void OnMoveToMarketButton()
{
    heroMovement.SetDestination(marketPosition);
}
```

### With Decision System
```csharp
UIManager.Instance.ShowDecisionPopUp(
    "Where to go?",
    "Choose your destination",
    new ButtonHelper { 
        BtnText = "Market", 
        OnPress = () => heroMovement.SetDestination(marketPos) 
    },
    new ButtonHelper { 
        BtnText = "Home", 
        OnPress = () => heroMovement.SetDestination(homePos) 
    }
);
```
