# Weapon Trail Setup Guide

This guide shows how to add motion trails to weapons during attacks (like swords leaving a glowing trail).

## What You'll Get

- **Motion trails** that follow weapon during attacks
- **Customizable colors** per weapon
- **Automatic enable/disable** during attack animations
- **No performance overhead** (uses Unity's built-in TrailRenderer)

---

## Quick Setup

### Step 1: Add Trail Renderer to Weapon

1. **Find your weapon model** in the character hierarchy:
   ```
   Character
   └── Weapon (the actual 3D model or empty GameObject)
       └── Sword_Mesh (or weapon mesh)
   ```

2. Select the **Weapon** GameObject (or weapon mesh)
3. **Add Component** → **Effects** → **Trail Renderer**

---

### Step 2: Configure Trail Renderer

In the Trail Renderer component:

**Time Settings:**
- **Time:** 0.3 (how long trail lasts)
- **Min Vertex Distance:** 0.1 (smoother trail)
- **Autodestruct:** Unchecked

**Width:**
- **Width:** Create a curve from 0.1 → 0 (tapers at the end)
- Click the width curve to edit

**Color:**
- **Color:** White (will be overridden by WeaponConfig)
- **Gradient:** Linear fade from opaque → transparent

**Materials:**
- **Material:** Use `Default-Particle` or create a glow material

**Rendering:**
- **Alignment:** View (faces camera)
- **Texture Mode:** Stretch

---

### Step 3: Create Trail Material (Optional but Recommended)

1. **Create Material:**
   - Right-click in Project → **Create** → **Material**
   - Name it "WeaponTrail"

2. **Configure Material:**
   - **Shader:** Select "Particles/Additive"
   - **Texture:** Use a gradient texture (white → transparent)
   - **Tint Color:** White (weapon config will override)

3. **Assign to Trail:**
   - Select weapon's Trail Renderer
   - Drag material into **Materials** slot

---

### Step 4: Configure WeaponFeedback Component

1. Select your **character** (parent GameObject)
2. Find the **WeaponFeedback** component (should already be there)
3. In the Inspector:
   - **Weapon Trail:** Drag your weapon's Trail Renderer here

---

### Step 5: Configure Weapon Config

1. Find your **WeaponConfig** asset (e.g., `Assets/Data/Weapons/Sword.asset`)
2. In the Inspector:
   - **Enable Weapon Trail:** ✓ Check this
   - **Trail Color:** Choose color (white, blue, red, etc.)

---

### Step 6: Test

1. Enter Play Mode
2. Attack with the weapon
3. You should see a trail during the swing!

---

## Configuration Options

### Trail Renderer Settings

```
Time: 0.3 seconds
```
- How long the trail stays visible
- **0.2 =** Short, snappy trail
- **0.5 =** Long, flowing trail

```
Min Vertex Distance: 0.1
```
- Lower = smoother but more vertices
- Higher = choppier but better performance
- **Recommended:** 0.05 - 0.15

```
Width Curve
```
- Controls trail thickness along its length
- **Tip:** Start thick (0.1) → End thin (0)
- Creates a tapered, elegant look

### WeaponConfig Settings

```
Enable Weapon Trail: ✓
```
- Check to enable trail for this weapon
- Uncheck for weapons that shouldn't have trails

```
Trail Color: White
```
- Color of the trail
- Examples:
  - **Fire Sword:** Orange/Red
  - **Ice Blade:** Cyan/Blue
  - **Holy Weapon:** Yellow/Gold
  - **Dark Blade:** Purple/Black

---

## Advanced Customization

### Gradient Trail (Fades Over Time)

1. Select Trail Renderer
2. In **Color** section, click the gradient
3. Set it to: `White (Alpha 1.0) → White (Alpha 0.0)`
4. This makes trail fade out smoothly

### Glowing Trail Effect

1. Create material with **Particles/Additive** shader
2. Increase **Tint Color** brightness (HDR color, values > 1)
3. This creates a bright, glowing trail

### Multiple Trails (Dual Wielding)

If character has two weapons:

```
Character
├── WeaponFeedback (main component)
├── RightWeapon
│   └── TrailRenderer (assign this to WeaponFeedback)
└── LeftWeapon
    └── TrailRenderer (add second WeaponFeedback for this)
```

### Textured Trails

Use a custom texture for unique effects:

1. Create/import a trail texture (long gradient image)
2. Assign to material
3. Set Texture Mode to "Stretch" or "Tile"

---

## Troubleshooting

### Trail doesn't appear

1. Check **WeaponFeedback** has Trail Renderer assigned
2. Verify **Enable Weapon Trail** is checked in WeaponConfig
3. Make sure Trail Renderer is on the **weapon**, not character root
4. Check if trail **Time** is > 0

### Trail is wrong color

- Set Trail Renderer **Color** to **White** (weapon config will tint it)
- Check **Trail Color** in WeaponConfig asset

### Trail stays visible after attack

- Verify **Emitting** is unchecked by default in Trail Renderer
- Check WeaponFeedback is disabling it after animation

### Trail is too thick/thin

- Adjust **Width** curve in Trail Renderer
- Try values between 0.05 - 0.2

### Trail looks choppy/pixelated

- Lower **Min Vertex Distance** (try 0.05)
- Increase **Time** slightly for smoother appearance

### Trail appears in wrong position

- Make sure Trail Renderer is on the **weapon mesh**, not character
- Check weapon pivot point (trail starts from object's origin)

---

## Trail Placement Tips

### For Swords/Blades
- Attach to the **blade mesh** or tip
- Width: Start 0.1 → End 0
- Time: 0.3 seconds

### For Axes/Hammers
- Attach to the **head** of the weapon
- Width: Thicker trail (0.15 → 0)
- Time: 0.4 seconds (heavier weapons)

### For Magical Staffs
- Attach to the **staff tip**
- Color: Magical colors (cyan, purple)
- Time: 0.5 seconds (lingering magic)

---

## Performance Notes

- Trail Renderer is very efficient (built-in Unity component)
- Only renders when emitting (no cost when disabled)
- No pooling needed (unlike particles)
- Minimal draw calls (single mesh)

✅ Safe to use on all weapons without performance impact!

---

## Example Configurations

### Fire Sword
```
Trail Color: Orange (255, 100, 0)
Trail Time: 0.35
Width: 0.12 → 0
Material: Additive shader with fire texture
```

### Ice Blade
```
Trail Color: Cyan (0, 200, 255)
Trail Time: 0.4
Width: 0.08 → 0
Material: Additive shader, bright HDR color
```

### Dark Weapon
```
Trail Color: Purple (150, 0, 200)
Trail Time: 0.5
Width: 0.15 → 0
Material: Additive with dark particle texture
```

---

## Next Steps

Once you have weapon trails working:
- Add **HitStop** (time freeze on impact)
- Implement **Camera Shake** for heavy hits
- Add **Knockback** physics
- Create **combo trails** (multiple colors for combos)
