# Character Control System - Complete Guide

## Overview
The scenario system can now control specific characters by their **CharacterName** enum (Hero, Janet, Pedi, Commander) and lock/unlock player input.

## New Components

### 1. CharacterController
- **Attach to**: Each character GameObject (Hero, Janet, Pedi, Commander)
- **Purpose**: Identifies the character and controls player input
- **Required Components**: HeroMovementBaseBuilder, HeroClickToMoveBaseBuilder

### 2. CharacterManager (Singleton)
- **Purpose**: Finds and manages all characters in the scene
- **Auto-registers**: All CharacterController components on scene start

### 3. SetPlayerControlStep (NEW)
- **Purpose**: Lock/unlock player control during scenarios
- **Use Case**: Disable clicks during cutscenes, enable after tutorial

---

## Setup: Character GameObjects

For each character in your BaseBuilder scene (Hero, Janet, Pedi, Commander):

### Step 1: Add CharacterController Component

```
1. Select your character GameObject (e.g., Hero)
2. Add Component → CharacterController
3. Configure:
   - Character Name: Hero (or Janet, Pedi, Commander)
   - Player Control Enabled: ✓ (or uncheck to start locked)
```

### Step 2: Verify Required Components

Each character must have:
```
✓ HeroMovementBaseBuilder
✓ HeroClickToMoveBaseBuilder
✓ CharacterController (newly added)
✓ NavMeshAgent (for pathfinding)
```

### Step 3: Create CharacterManager

```
1. Create Empty GameObject in BaseBuilder scene
2. Name it: "CharacterManager"
3. Add Component → CharacterManager
```

That's it! CharacterManager will auto-register all characters on Start.

---

## Usage in Scenarios

### Example 1: Lock Player Control (Cutscene Start)

```
Create → Scenario → Set Player Control Step
Name: "LockPlayerControl"

Configure:
- Enable Player Control: ✗ (unchecked)
- Apply To All Characters: ✓
- Complete Immediately: ✓
- Description: "Disable player input for cutscene"
```

### Example 2: Move Specific Character

```
Create → Scenario → Move Character to Location Step
Name: "HeroEntersCastle"

Configure:
- Character To Move: Hero (dropdown)
- Target Location Id: "Castle"
- Arrival Radius: 1.0
- Description: "Hero walks into Castle"
```

### Example 3: Unlock Player Control (Tutorial End)

```
Create → Scenario → Set Player Control Step
Name: "UnlockPlayerControl"

Configure:
- Enable Player Control: ✓ (checked)
- Apply To All Characters: ✓
- Complete Immediately: ✓
- Description: "Player can now control characters"
```

### Example 4: Move Multiple Characters

```
Step 1: Move Hero
- Character To Move: Hero
- Target Location Id: "Castle"

Step 2: Move Janet
- Character To Move: Janet
- Target Location Id: "Port"

(They move sequentially, one after another)
```

---

## Updated Level 1 Scenario

Here's your Level 1 with the new character system:

### Scenario Flow

```
1. LockPlayerControl (all characters locked)
2. CameraMoveToCastle
3. HeroEntersCastle (Hero moves automatically)
4. Dialogue_SoldierReport
5. Dialogue_CommanderResponse
6. Dialogue_CallToAction
7. CameraMoveToPort
8. ShowClashButton
9. (Player clicks CLASH → Fight scene)
```

### Create the Steps

#### Step 0: Lock Player Control
```
SetPlayerControlStep: "Step0_LockControl"
- Enable Player Control: ✗
- Apply To All Characters: ✓
- Description: "Lock player input during intro"
```

#### Step 1: Camera to Castle
```
CameraMoveStep: "Step1_ShowCastle"
- Target Location Id: "Castle"
- Movement Duration: 1.5
- Description: "Camera moves to Castle"
```

#### Step 2: Hero Enters Castle
```
MoveUnitToLocationStep: "Step2_HeroEntersCastle"
- Character To Move: Hero (dropdown!)
- Target Location Id: "Castle"
- Arrival Radius: 1.0
- Description: "Hero walks into Castle"
```

#### Steps 3-7: Same as before
(Dialogue, camera move, button)

---

## Character Name Enum

Your characters are defined in `CharacterName.cs`:

```csharp
public enum CharacterName
{
    Hero,
    Janet,
    Pedi,
    Commander
}
```

Each character GameObject must have **CharacterController** with the matching enum value.

---

## Player Control States

### Locked (Player Cannot Click)
- Used during cutscenes
- Used during scripted character movements
- Used when you want to force player to watch

### Unlocked (Player Can Click)
- Used during normal gameplay
- Used after tutorial completes
- Used when giving player freedom

---

## Common Patterns

### Pattern 1: Cutscene with Character Movement

```
1. SetPlayerControlStep (lock all)
2. CameraMoveStep (show location)
3. MoveCharacterStep (Hero moves)
4. DialogueStep (conversation)
5. SetPlayerControlStep (unlock all)
```

### Pattern 2: Tutorial - Guide Then Release

```
1. SetPlayerControlStep (lock all)
2. DialogueStep ("Click here to move")
3. SetPlayerControlStep (unlock Hero only)
4. WaitForPlayerStep (wait for Hero to reach location)
5. DialogueStep ("Good job!")
```

### Pattern 3: Multi-Character Coordination

```
1. SetPlayerControlStep (lock all)
2. MoveCharacterStep (Hero → Castle)
3. MoveCharacterStep (Janet → Port)
4. MoveCharacterStep (Pedi → Blacksmith)
5. DialogueStep ("Everyone in position!")
6. SetPlayerControlStep (unlock all)
```

---

## Troubleshooting

### "Character 'Hero' not found in scene!"
- Check that Hero GameObject has **CharacterController** component
- Verify **characterName** is set to "Hero"
- Ensure **CharacterManager** exists in scene

### Player can still click during cutscene
- Add **SetPlayerControlStep** at start of scenario
- Set **Enable Player Control: ✗**
- Set **Apply To All Characters: ✓**

### Character doesn't move
- Check character has **HeroMovementBaseBuilder**
- Check character has **HeroClickToMoveBaseBuilder**
- Verify **NavMesh** is baked in scene
- Check **NamedLocation** exists with correct locationId

### Wrong character moves
- Check **Character To Move** dropdown in MoveCharacterStep
- Verify CharacterController.characterName matches

---

## API Reference

### CharacterController

```csharp
// Enable/disable player control
character.SetPlayerControlEnabled(true/false);

// Move character (scenario control)
character.MoveToPosition(position);

// Stop movement
character.Stop();

// Check if moving
bool isMoving = character.IsMoving;
```

### CharacterManager

```csharp
// Get character
CharacterController hero = CharacterManager.Instance.GetCharacter(CharacterName.Hero);

// Set control for one character
CharacterManager.Instance.SetPlayerControl(CharacterName.Hero, true);

// Set control for all characters
CharacterManager.Instance.SetAllPlayerControl(false);

// Move character
CharacterManager.Instance.MoveCharacterTo(CharacterName.Hero, position);
```

---

## Summary Checklist

Scene Setup:
- [ ] Each character has CharacterController component
- [ ] CharacterController.characterName set correctly (Hero, Janet, Pedi, Commander)
- [ ] CharacterManager GameObject exists in scene
- [ ] All characters have HeroMovementBaseBuilder + HeroClickToMoveBaseBuilder

Scenario Steps:
- [ ] Use SetPlayerControlStep to lock/unlock input
- [ ] Use MoveCharacterStep with CharacterName dropdown
- [ ] Lock control at scenario start
- [ ] Unlock control when appropriate

Testing:
- [ ] Characters can't be clicked when locked
- [ ] Characters move when commanded by scenario
- [ ] Player can click when unlocked

You now have full control over character movement and player input! 🎮
