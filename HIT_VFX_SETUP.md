# Hit VFX Setup Guide

This guide explains how to add particle effects when hitting enemies.

## What You'll Get

- **Blood/impact particles** when attacking enemies
- **Object pooling** (no performance drops from constant spawning)
- **Automatic positioning** at the exact hit point
- **Configurable per-character** (different VFX for different enemies)

---

## Quick Setup

### Step 1: Create or Import a Particle Effect

**Option A: Use Unity's built-in particles**

1. In Hierarchy → Right-click → **Effects → Particle System**
2. Rename it to "HitVFX_Blood" (or whatever you like)
3. Configure the particle system:
   - **Duration:** 0.5
   - **Start Lifetime:** 0.3-0.5
   - **Start Speed:** 2-5
   - **Start Size:** 0.1-0.3
   - **Start Color:** Red (or any impact color)
   - **Max Particles:** 20-30
4. Enable **Emission**:
   - Rate over Time: 0
   - Bursts: Add 1 burst at time 0 with 10-20 particles
5. **Save as prefab**: Drag to `Assets/Prefabs/VFX/HitVFX_Blood.prefab`
6. Delete from scene

**Option B: Import from Asset Store**

1. Asset Store → Search "Hit VFX" or "Impact Particles"
2. Import a pack (many free options available)
3. Use one of the prefabs directly

---

### Step 2: Add HitVFXSpawner to Character

1. Select your character prefab (Enemy, Player, etc.)
2. **Add Component** → `HitVFXSpawner`
3. In the Inspector:
   - **Default Hit VFX Prefab:** Drag your particle prefab here
   - **VFX Scale:** Adjust size (1 = normal, 2 = double size)
   - **Pool Initial Size:** 3 (pre-creates 3 instances for performance)
   - **Use Global Pool:** Check this (keeps pooled objects organized)

---

### Step 3: Test

1. Enter Play Mode
2. Attack the character
3. You should see particles spawn at the hit point!

---

## Configuration Options

### VFX Settings

```
Default Hit VFX Prefab: [Your particle prefab]
```
- The particle system to spawn when hit
- Leave empty to disable VFX for this character

```
Hit Point Offset: (0, 0, 0)
```
- Adjust position relative to hit point
- Use (0, 0.5, 0) to raise VFX higher
- Use (0, -0.5, 0) to lower VFX

```
VFX Scale: 1.0
```
- Multiplier for particle size
- 0.5 = half size (subtle)
- 2.0 = double size (dramatic)

### Pooling Settings

```
Pool Initial Size: 3
```
- How many VFX instances to pre-create
- Higher = better first-hit performance
- Lower = less memory usage
- **Recommended:** 2-5 for common enemies

```
Use Global Pool: ✓
```
- **Checked:** All characters share one pool (cleaner, recommended)
- **Unchecked:** Each character has its own pool (more control)

---

## Advanced Customization

### Different VFX Per Enemy Type

Create multiple particle prefabs:
```
Assets/Prefabs/VFX/
├── HitVFX_Blood.prefab      (for flesh enemies)
├── HitVFX_Sparks.prefab     (for metal enemies)
├── HitVFX_Dust.prefab       (for stone enemies)
└── HitVFX_Magic.prefab      (for magical enemies)
```

Assign different prefabs to different enemy prefabs!

### Multiple Hit Effects

You can add multiple HitVFXSpawner components to spawn different effects:

```
Enemy GameObject
├── HitVFXSpawner (blood particles)
├── HitVFXSpawner (impact flash)
└── HitVFXSpawner (damage numbers - future)
```

### Particle System Tips

**For Blood Effects:**
- Color: Red to dark red gradient
- Shape: Cone (15-30 degree spread)
- Velocity over Lifetime: Slow down particles
- Color over Lifetime: Fade to transparent

**For Sparks:**
- Color: Yellow to orange
- Shape: Cone (wide spread)
- Start Speed: Fast (5-10)
- Gravity: Enable (particles fall)

**For Magic:**
- Color: Bright cyan/purple
- Shape: Sphere
- Start Size: Small
- Size over Lifetime: Grow then shrink

---

## Troubleshooting

### No VFX spawns when hitting

1. Check Console for errors/warnings
2. Verify `HitVFXSpawner` component is on the **target** (not attacker)
3. Ensure **Default Hit VFX Prefab** is assigned
4. Make sure prefab has a **ParticleSystem** component
5. Check if character has **Health** component (required for damage events)

### VFX spawns at wrong location

1. Check **Hit Point Offset** in HitVFXSpawner
2. Adjust Y value to raise/lower
3. Try setting it to (0, 1, 0) for testing

### VFX is too big/small

- Adjust **VFX Scale** in HitVFXSpawner (not the particle system itself)
- 0.5 = smaller, 2.0 = bigger

### VFX doesn't face the right direction

- Particle systems use their own rotation settings
- In the particle system prefab:
  - **Shape:** Change cone angle/direction
  - **Simulation Space:** Try "World" instead of "Local"

### Performance issues (lag when spawning VFX)

1. Increase **Pool Initial Size** (pre-creates instances)
2. Reduce **Max Particles** in particle system (lower count)
3. Disable **Collision** module if enabled (expensive)
4. Use simpler particle shapes (sphere > mesh)

### Particles don't disappear

- Check particle system **Duration** is set (not looping)
- Verify **Stop Action** is set to "Disable" (not "Destroy")
- Look for "Looping" checkbox - make sure it's **unchecked**

---

## Integration with Weapon System

In the future, you can make VFX weapon-specific:

```csharp
// In WeaponConfig.cs
public GameObject hitVFXPrefab; // Different VFX per weapon

// In HitVFXSpawner.cs
// Uncomment the TODO section to use weapon-specific VFX
```

This allows:
- Fire sword → Fire particles
- Ice axe → Ice shards
- Lightning staff → Electric sparks

---

## Performance Best Practices

✅ **DO:**
- Use object pooling (HitVFXSpawner does this automatically)
- Keep particle counts low (10-30 per hit)
- Set short durations (0.3-0.5 seconds)
- Pre-warm pools for common enemies

❌ **DON'T:**
- Use looping particles for hit effects
- Enable collision on particles (very expensive)
- Use Sub Emitters unless necessary
- Create hundreds of particles per hit

---

## Example Particle Settings

### Simple Blood Hit
```
Duration: 0.5
Start Lifetime: 0.3
Start Speed: 3
Start Size: 0.2
Start Color: Red
Max Particles: 15
Emission Burst: 10 particles at time 0
Shape: Cone, Angle 20
Gravity: 5
```

### Sparks (Metal Impact)
```
Duration: 0.4
Start Lifetime: 0.2
Start Speed: 5
Start Size: 0.1
Start Color: Yellow to Orange
Max Particles: 20
Emission Burst: 15 particles at time 0
Shape: Cone, Angle 30
Gravity: 10
Color over Lifetime: Fade to black
```

### Magical Hit
```
Duration: 0.6
Start Lifetime: 0.4
Start Speed: 2
Start Size: 0.15 to 0.3
Start Color: Cyan
Max Particles: 25
Emission Burst: 20 particles at time 0
Shape: Sphere
Size over Lifetime: Shrink curve
Color over Lifetime: Fade to transparent
```

---

## Next Steps

Once you have hit VFX working:
- Add **weapon trails** (coming next)
- Create **weapon-specific VFX** (fire, ice, lightning)
- Add **screen shake** on heavy hits
- Implement **damage numbers** (floating text)
