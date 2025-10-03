# Health Bar UI Setup Guide

This guide explains how to set up animated health bars above characters using the `HealthBarUI` component.

## Overview

The health bar system provides:
- **Smooth animated transitions** when health changes
- **Billboard effect** (always faces camera)
- **Color gradient** (green → yellow → red based on health)
- **Auto-hide** when at full health or dead
- **Event-driven** (listens to CombatEvents, no polling)

## Quick Setup

### Step 1: Create Health Bar Prefab

1. **Create World Space Canvas:**
   - Hierarchy → Right-click → UI → Canvas
   - Rename to "HealthBarCanvas"
   - Inspector → Canvas component:
     - Render Mode: **World Space**
     - Width: 100
     - Height: 20
   - Rect Transform:
     - Scale: (0.01, 0.01, 0.01) ← Important for world-space sizing

2. **Add Background Image:**
   - Right-click HealthBarCanvas → UI → Image
   - Rename to "Background"
   - Rect Transform: Anchor to **Stretch** (full width/height)
   - Image:
     - Color: Black with ~50% alpha
     - (Optional) Use a sprite for rounded corners

3. **Add Fill Image:**
   - Right-click HealthBarCanvas → UI → Image
   - Rename to "Fill"
   - Rect Transform:
     - Anchor: **Stretch**
     - Left/Right/Top/Bottom: 2 (small padding from background)
   - Image:
     - Image Type: **Filled**
     - Fill Method: **Horizontal**
     - Fill Origin: **Left**
     - Fill Amount: 1
     - Color: Green

4. **Save as Prefab:**
   - Drag HealthBarCanvas into Project window
   - Save as: `Assets/Prefabs/UI/HealthBarCanvas.prefab`
   - Delete from scene (we'll attach it via script or manually)

### Step 2: Attach to Character

**Option A: Manual Setup (Recommended)**

1. Select your character prefab (e.g., Enemy, Player)
2. Add Component → `HealthBarUI`
3. Drag the HealthBarCanvas prefab as a **child** of the character
4. In HealthBarUI component:
   - **Health Bar Canvas:** Drag the HealthBarCanvas child
   - **Fill Image:** Drag the Fill image from the canvas
   - **Background Image:** (Optional) Drag the Background image
5. Adjust settings:
   - **World Offset:** (0, 2.5, 0) ← Adjust Y to position above character
   - **Hide When Full:** Check if you only want to show when damaged
   - **Use Color Gradient:** Check for green → yellow → red effect

**Option B: Prefab Variant**

1. Create a prefab variant for each character type
2. Pre-configure the health bar settings per type
3. Enemy variants can have different offsets/colors

### Step 3: Test

1. Enter Play Mode
2. Attack a character with a health bar
3. You should see:
   - ✅ Health bar appears (if hideWhenFull is enabled)
   - ✅ Bar smoothly animates down
   - ✅ Color changes from green → yellow → red
   - ✅ Bar always faces camera as you move

## Configuration Options

### Positioning

```
World Offset: (0, 2.5, 0)
```
- Adjust **Y** value to position above character's head
- Taller characters need higher Y values
- Use negative Y for grounded health bars

```
Billboard To Camera: ✓
```
- Keep checked for 3D games (bar always faces player)
- Uncheck for 2D games or fixed camera angles

### Animation

```
Animation Duration: 0.3 seconds
```
- Lower = snappy (0.1-0.2s)
- Higher = smooth (0.4-0.5s)

```
Animation Curve: EaseInOut
```
- Click to edit curve in Inspector
- Presets: Linear, EaseIn, EaseOut, EaseInOut

### Visibility

```
Hide When Full: ✓
Hide When Dead: ✓
Hide Delay: 2 seconds
```

**Use Cases:**
- **Enemies:** Hide when full (clutter reduction)
- **Player:** Always show (important info)
- **Bosses:** Always show with custom colors

### Colors

```
High Health Color: Green (RGB: 0, 255, 0)
Medium Health Color: Yellow (RGB: 255, 255, 0)
Low Health Color: Red (RGB: 255, 0, 0)
Use Color Gradient: ✓
```

**Health Thresholds:**
- **> 60%:** High health color (green)
- **30-60%:** Gradient from medium → high
- **< 30%:** Gradient from low → medium

**Custom Color Schemes:**
- **Enemy:** Red → Orange → Yellow
- **Ally:** Blue → Cyan → Green
- **Boss:** Purple → Magenta → Red

## Advanced Customization

### Custom Health Bar Sprite

1. Create a rounded bar sprite in Photoshop/GIMP
2. Set Sprite Mode to "Sliced" with 9-slice borders
3. Assign to Fill Image
4. Set Image Type to "Sliced" or "Filled"

### Multiple Health Bar Types

Create different prefabs for different character types:

```
Assets/Prefabs/UI/
├── HealthBarCanvas_Enemy.prefab    (Red theme, hide when full)
├── HealthBarCanvas_Player.prefab   (Blue theme, always visible)
├── HealthBarCanvas_Boss.prefab     (Large, custom colors, effects)
```

### Add Text Display

1. Add child Text (TextMeshPro) to HealthBarCanvas
2. Display current/max health: "80/100"
3. Script example:

```csharp
// In HealthBarUI.cs, add:
[SerializeField] private TextMeshProUGUI healthText;

// In HandleDamageTaken:
if (healthText != null)
{
    healthText.text = $"{e.CurrentHealth:F0}/{e.MaxHealth:F0}";
}
```

### Delayed Damage (Two-Bar System)

Show recent damage with a delayed second bar:

1. Add second fill image behind main fill
2. Animate it with a delay to show "damage preview"
3. Common in action games (God of War, Devil May Cry)

### Shield/Armor Bars

Stack multiple bars for different damage types:

```
Fill_Shield (Blue) ← Depletes first
Fill_Health (Green) ← Depletes when shield is gone
```

## Troubleshooting

### Health bar is too big/small

- Adjust Canvas Rect Transform **Scale**
- Default: (0.01, 0.01, 0.01)
- Smaller = (0.005, 0.005, 0.005)
- Larger = (0.02, 0.02, 0.02)

### Health bar doesn't appear

1. Check Console for warnings from HealthBarUI
2. Ensure Health component exists on same GameObject
3. Verify Canvas/Fill Image are assigned in Inspector
4. Check if "Hide When Full" is enabled (damage the character to test)

### Health bar doesn't face camera

- Ensure "Billboard To Camera" is checked
- Verify Camera.main is set (tag Main Camera on your camera)
- Canvas must be in **World Space** render mode

### Health bar doesn't animate

- Check Animation Duration > 0
- Verify Fill Image is assigned
- Check Animation Curve has keyframes

### Colors don't change

- Enable "Use Color Gradient"
- Verify color values are different (not all white)
- Check Fill Image Color is not overriding in Inspector

### Health bar appears in wrong position

- Adjust **World Offset** in HealthBarUI component
- Y value controls height above character
- Try: (0, 3, 0) for tall characters, (0, 1.5, 0) for short ones

### Performance issues with many health bars

- Disable billboarding for distant enemies
- Use object pooling for health bar canvases
- Implement LOD system (hide bars when far from camera)

## Integration with Combat System

The HealthBarUI component automatically integrates with:

- ✅ **CombatEvents** (listens to OnDamageTaken, OnDeath)
- ✅ **Health component** (gets max health, current health)
- ✅ **No manual updates needed** (fully event-driven)

## Performance Notes

- Each health bar is a separate Canvas (batched if using same material)
- LateUpdate runs every frame for billboarding
- Consider disabling billboarding for stationary/distant enemies
- Health bar only animates when damaged (no constant polling)

## Examples

### Basic Enemy Setup
```
Enemy GameObject
├── Health (100 max)
├── HealthBarUI
│   ├── World Offset: (0, 2.5, 0)
│   ├── Hide When Full: ✓
│   ├── Use Color Gradient: ✓
└── HealthBarCanvas (World Space)
    ├── Background (Black, 50% alpha)
    └── Fill (Filled Horizontal, Green)
```

### Boss Setup
```
Boss GameObject
├── Health (500 max)
├── HealthBarUI
│   ├── World Offset: (0, 4, 0)
│   ├── Hide When Full: ✗
│   ├── Animation Duration: 0.5s (smoother)
│   ├── High Health: Purple
│   ├── Low Health: Red
└── HealthBarCanvas (Larger scale)
    ├── Background (with glow effect)
    └── Fill (custom boss bar sprite)
```

### Player Setup
```
Player GameObject
├── Health (100 max)
├── HealthBarUI
│   ├── Hide When Full: ✗ (always show)
│   ├── Billboard: ✓
│   ├── Colors: Blue theme
└── HealthBarCanvas
    └── Fill + Text (shows "80/100")
```

## Next Steps

- Add shield/armor bars
- Implement damage numbers (floating text)
- Add screen-space health bars for bosses
- Create health bar presets for different enemy types
