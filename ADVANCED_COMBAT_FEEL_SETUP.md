# Advanced Combat Feel Setup Guide

This guide covers Tier 3 combat feedback: **HitStop**, **Camera Shake**, and **Knockback**.

---

## HitStop (Time Freeze on Impact)

Brief pause when attacks land - makes hits feel powerful and weighty.

### Setup

1. **Create TimeController GameObject:**
   - Hierarchy → Create Empty GameObject
   - Rename to "TimeController"
   - Add Component → `TimeController`

2. **Configure TimeController:**
   - **Enable Hit Stop:** ✓ (global toggle)
   - **Hit Stop Multiplier:** 1.0 (adjust globally)
   - **Player Only Hit Stop:** ✓ (only freeze when player involved)

3. **Configure Per-Weapon:**
   - Open your WeaponConfig asset
   - **Enable Hit Stop:** ✓
   - **Hit Stop Duration:** 0.05 (very brief pause)
     - Light weapons: 0.03-0.05
     - Heavy weapons: 0.08-0.15

4. **Test:**
   - Attack an enemy
   - You should see a brief freeze on impact

### Tips

**Duration Recommendations:**
- **Sword:** 0.05s (snappy)
- **Axe/Hammer:** 0.1s (heavy impact)
- **Dagger:** 0.03s (quick hits)
- **Boss attacks:** 0.15s (dramatic)

**When to Disable:**
- Rapid-fire weapons (machine guns, etc.)
- DoT (damage over time) effects
- Environmental damage

---

## Camera Shake

Shakes the camera on impact for visual feedback.

### Setup

1. **Add to Main Camera:**
   - Select your Main Camera
   - Add Component → `CameraShake`

2. **Configure CameraShake:**
   - **Enable Shake:** ✓
   - **Shake Multiplier:** 1.0
   - **Player Only Shake:** ✓ (only shake when player involved)
   - **Default Shake Duration:** 0.2s
   - **Shake Curve:** EaseInOut (starts strong, fades)

3. **Configure Per-Weapon:**
   - Open your WeaponConfig asset
   - **Enable Camera Shake:** ✓
   - **Camera Shake Intensity:** 0.2 (subtle shake)
     - Light: 0.1-0.2
     - Medium: 0.3-0.5
     - Heavy: 0.6-1.0
   - **Camera Shake Duration:** 0.2s

4. **Test:**
   - Attack an enemy
   - Camera should shake slightly

### Tips

**Intensity Guidelines:**
- **0.1:** Barely noticeable (fast attacks)
- **0.3:** Noticeable but not distracting (normal hits)
- **0.5:** Strong shake (heavy attacks)
- **1.0:** Very intense (only for special moves/boss attacks)

**Shake Curve Presets:**
- **EaseInOut:** Smooth, cinematic (recommended)
- **Linear:** Consistent shake (mechanical feel)
- **EaseOut:** Starts strong, fades quickly (impact emphasis)

**When to Use:**
- Player taking damage (high intensity)
- Heavy weapon hits
- Explosions
- Boss attacks

**When NOT to Use:**
- Light/fast attacks (too chaotic)
- Constant damage (motion sickness)
- Ranged weapons (unless special)

---

## Knockback

Pushes targets away from attacker - makes hits feel impactful.

### Setup

1. **Add to Characters:**
   - Select character prefab (Enemy, Player, etc.)
   - Add Component → `Knockback`

2. **Configure Knockback:**
   - **Enable Knockback:** ✓
   - **Knockback Multiplier:** 1.0
   - **Knockback Duration:** 0.3s
   - **Disable Control During Knockback:** ✓ (character can't act while knocked back)
   - **Use NavMeshAgent:** ✓ (works with NavMesh)
   - **Upward Force:** 0.5 (slight launch effect)

3. **Configure Per-Weapon (Future):**
   - Currently uses default force (5 units)
   - Will be configurable via WeaponConfig later

4. **Test:**
   - Attack an enemy
   - Enemy should slide backward

### Tips

**Duration Recommendations:**
- **Light knockback:** 0.2s (quick push)
- **Medium knockback:** 0.3-0.4s (noticeable)
- **Heavy knockback:** 0.5-0.7s (dramatic launch)

**Upward Force:**
- **0:** No lift (slide only)
- **0.5:** Subtle hop (default)
- **1-2:** Noticeable launch (heavy hits)
- **3-5:** Big launch (special attacks)

**Use Cases:**
- Heavy weapons (axes, hammers)
- Charge attacks
- Explosions
- Blocking/parrying

**Considerations:**
- May push enemies off ledges (feature or bug?)
- Can interrupt enemy attacks (good for combat flow)
- Looks weird if knockback duration > attack animation

---

## Combining Effects for Maximum Impact

### Light Weapon (Dagger/Sword)
```
HitStop: 0.03s
Camera Shake: Disabled
Knockback: Disabled
```
Fast, snappy feel - no interruptions.

### Medium Weapon (Sword/Axe)
```
HitStop: 0.05s
Camera Shake: 0.2 intensity, 0.2s duration
Knockback: 0.3s duration, 0.5 upward force
```
Balanced feel - noticeable but not overwhelming.

### Heavy Weapon (Hammer/Greatsword)
```
HitStop: 0.1s
Camera Shake: 0.5 intensity, 0.3s duration
Knockback: 0.5s duration, 1.5 upward force
```
Powerful, impactful - every hit feels heavy.

### Boss Attack
```
HitStop: 0.15s
Camera Shake: 1.0 intensity, 0.4s duration
Knockback: 0.7s duration, 3.0 upward force
```
Dramatic, devastating - player feels the danger.

---

## Integration with Weapon System

All three systems read from `WeaponConfig`:

```csharp
// In WeaponConfig.asset
[Header("Impact Feedback")]
enableHitStop = true
hitStopDuration = 0.05

enableCameraShake = true
cameraShakeIntensity = 0.3
cameraShakeDuration = 0.2

enableKnockback = true  // Coming soon
knockbackForce = 5
```

This allows per-weapon customization without code changes!

---

## Troubleshooting

### HitStop Issues

**Time doesn't freeze:**
- Check TimeController exists in scene
- Verify "Enable Hit Stop" is checked (TimeController + WeaponConfig)
- Ensure player is involved (if playerOnlyHitStop = true)

**Game stays frozen:**
- Check Console for errors
- Time.timeScale should restore to 1.0
- Try manually setting Time.timeScale = 1 in Console

**Audio continues during freeze:**
- Expected behavior - audio uses unscaled time
- To fix: Set AudioSource.ignoreListenerPause = false

### Camera Shake Issues

**Camera doesn't shake:**
- Verify CameraShake is on Main Camera
- Check "Enable Shake" is checked
- Ensure weapon has enableCameraShake = true

**Shake too intense/weak:**
- Adjust Shake Multiplier (global) or Intensity (per-weapon)
- Check Shake Curve isn't flat

**Camera position doesn't restore:**
- CameraShake saves originalLocalPosition in Start()
- Don't move camera parent after scene starts
- Check for other scripts modifying camera position

### Knockback Issues

**Character doesn't move:**
- Verify Knockback component exists
- Check Mover component exists (if useNavMeshAgent = true)
- Ensure NavMesh is baked

**Character moves wrong direction:**
- Knockback direction = (target - attacker).normalized
- Check attacker position is valid
- Try visualizing direction with Debug.DrawRay

**Knockback too short/long:**
- Adjust Knockback Multiplier
- Change Knockback Duration (0.2-0.5s recommended)
- Future: Configure knockbackForce in WeaponConfig

**Character falls through floor:**
- Ensure NavMesh is baked properly
- Disable upward force if causing issues
- Check colliders are set up correctly

---

## Performance Considerations

**HitStop:**
- ✅ Zero performance cost (just changes Time.timeScale)
- ⚠️ Can feel stuttery if overused (limit to 1 hit per 0.2s)

**Camera Shake:**
- ✅ Minimal cost (just position changes in Update)
- ⚠️ Can cause motion sickness if too intense/frequent

**Knockback:**
- ✅ Works with NavMesh (respects pathfinding)
- ⚠️ Coroutine per knockback (fine for <10 simultaneous)

---

## Advanced Techniques

### Directional Knockback

Modify Knockback.cs to use hit normal instead of attacker direction:

```csharp
// In HandleDamageDealt:
Vector3 knockbackDirection = e.HitNormal; // Use hit direction
```

### Radial Knockback (Explosions)

For AoE attacks, apply knockback from explosion center:

```csharp
public void ApplyRadialKnockback(Vector3 explosionCenter, float radius, float force)
{
    // Calculate direction away from explosion
    Vector3 direction = (transform.position - explosionCenter).normalized;
    ApplyKnockback(direction, force);
}
```

### Screen Space Shake

For UI shake instead of camera:

```csharp
// Shake UI element instead of camera
RectTransform uiElement;
// Apply offset to anchoredPosition instead of localPosition
```

### Time Dilation (Slow Motion)

Use TimeController for slow-mo on kill:

```csharp
TimeController.Instance.ApplySlowMotion(0.3f, 1f); // 30% speed for 1 second
```

---

## Testing Checklist

- [ ] HitStop creates brief pause on hit
- [ ] Camera shakes when enabled in weapon config
- [ ] Characters are knocked back on hit
- [ ] All effects restore to normal (no stuck states)
- [ ] Player-only modes work correctly
- [ ] Multiple simultaneous hits don't cause issues
- [ ] Effects scale with weapon type (light < heavy)
- [ ] Debug logging works when enabled

---

## Next Steps

- **Polish:** Fine-tune durations/intensities for each weapon
- **Juice:** Add screen flash, damage numbers, blood decals
- **Advanced:** Implement perfect parry (slow-mo), critical hits (extra shake)
- **Combo System:** Stack effects for combo hits
