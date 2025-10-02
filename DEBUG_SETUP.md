# Debug Configuration Setup

## Quick Setup

1. In Unity, create the DebugConfig asset:
   - Right-click in `Assets/Resources/` folder
   - Create > RPG > Debug Config
   - Name it exactly: **DebugConfig** (no prefix/suffix)

2. Configure debug settings:
   - Select the DebugConfig asset in Unity
   - Toggle the debug categories you want to see
   - **Master toggle**: `Enable Debug Logs` - turns ALL logs on/off

## Available Debug Categories

### Combat System
- **Log Combat Events** - Damage dealt/taken events
- **Log Attack Events** - Attack started/hit events
- **Log Fighter State** - Fighter targeting and state changes

### Audio System
- **Log Hit Sounds** - When impact sounds play
- **Log Weapon Sounds** - Swing sounds and weapon trails

### Visual Feedback
- **Log Hit Flash** - Flash initialization and triggers
- **Log Hit React** - Hit reaction animations

### Movement System
- **Log Mover Actions** - MoveTo, Cancel, Stop commands
- **Log Pathfinding** - NavMesh pathfinding operations

### AI System
- **Log AI Controller** - AI state changes and decisions
- **Log Patrol Behavior** - Patrol routes and waypoints

### Player Control
- **Log Player Controller** - Player input and actions

### Stats & Health
- **Log Health System** - Health changes and death
- **Log Stats System** - Stat modifications

## Usage Examples

**Turn off all logs:**
- Disable "Enable Debug Logs" in DebugConfig

**Only see combat feedback:**
- Enable: Log Hit Flash, Log Hit Sounds, Log Hit React
- Disable: Everything else

**Debug movement issues:**
- Enable: Log Mover Actions, Log Pathfinding
- Disable: Everything else

## Notes

- All debug logs are automatically stripped in builds (they only run in Editor)
- The DebugConfig MUST be in `Assets/Resources/` folder to be found at runtime
- Changes to DebugConfig take effect immediately (no restart needed)
