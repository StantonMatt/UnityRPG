# HitFlash Effect Guide (v3.0 - Checkbox System)

## Overview
The HitFlash component provides customizable visual feedback when characters take damage. **Mix and match effects using checkboxes** for complete control over your hit feedback!

## Available Effects (Mix & Match!)

### ✓ **Emission** (Bright Glow)
- Adds emission glow overlay on top of the character
- Very visible but can overwhelm texture details at high intensity
- **Best for:** Magical damage, critical hits, energy weapons

**Settings:**
- Emission Intensity: 0.5-10 (lower = subtle, higher = intense)

---

### ✓ **Color Tint** (Texture Brightening) ⭐ RECOMMENDED
- Brightens the character's texture while preserving all details
- Character model, armor colors, and textures remain visible
- Works by multiplying the texture color, making it brighter
- **Best for:** Physical damage, default hit feedback, subtle effects

**Settings:**
- Tint Strength: 0.2-1.0 (lower = subtle, higher = bright)

---

### ✓ **Outline** (Edge Highlight) 🆕
- Creates a colored outline around the character's silhouette
- Uses custom shader to expand mesh along vertex normals
- Outline fades in/out based on animation curve
- **Best for:** Stylized games, clear hit indication without obscuring character

**Settings:**
- Outline Width: 0.01-0.1 (thickness of edge)
- Outline Color: Any color you want

---

## Effect Combinations & Presets

### Subtle Hit (Default) ⭐
```
☑ Use Emission (Intensity: 2)
☑ Use Color Tint (Strength: 0.4)
☐ Use Outline
Flash Duration: 0.15s
```
Balanced visibility with texture preservation.

---

### Outline Only (Clean & Stylized)
```
☐ Use Emission
☐ Use Color Tint
☑ Use Outline (Width: 0.04, Color: White)
Flash Duration: 0.15s
```
Clean edge highlight without changing character appearance.

---

### Color Tint Only (Most Subtle)
```
☐ Use Emission
☑ Use Color Tint (Strength: 0.3-0.5)
☐ Use Outline
Flash Duration: 0.15s
```
Just brightens the character, preserves all details.

---

### Maximum Visibility (All Effects!)
```
☑ Use Emission (Intensity: 2-3)
☑ Use Color Tint (Strength: 0.4)
☑ Use Outline (Width: 0.04, Color: Yellow)
Flash Duration: 0.12s
```
Combines all effects for maximum impact.

---

### Emission Only (Bright Flash)
```
☑ Use Emission (Intensity: 4-6)
☐ Use Color Tint
☐ Use Outline
Flash Duration: 0.1s
```
Dramatic glow effect, good for magical damage.

---

### Outline + Tint (No Glow)
```
☐ Use Emission
☑ Use Color Tint (Strength: 0.5)
☑ Use Outline (Width: 0.05, Color: White)
Flash Duration: 0.15s
```
Edge highlight + brightened texture without emission glow.

---

## Parameter Reference

### Flash Effects Checkboxes
- **Use Emission**: Enable/disable emission glow
- **Use Color Tint**: Enable/disable texture brightening
- **Use Outline**: Enable/disable outline effect

### General Settings
- **Flash Color**: Base color for emission and tint effects
- **Flash Duration**: How long the effect lasts (0.05-0.5s)

### Emission Settings (when enabled)
- **Emission Intensity**: Brightness multiplier
  - 0.5-1.5 = Subtle glow
  - 2-4 = Medium glow
  - 5-10 = Intense flash

### Color Tint Settings (when enabled)
- **Tint Strength**: How much to brighten
  - 0.2-0.4 = Subtle brighten
  - 0.5-0.7 = Medium brighten
  - 0.8-1.0 = Strong brighten

### Outline Settings (when enabled)
- **Outline Width**: Thickness of the outline
  - 0.02-0.03 = Thin outline
  - 0.04-0.06 = Medium outline
  - 0.07-0.1 = Thick outline
- **Outline Color**: Color of the outline edge

### Animation
- **Flash Curve**: Animation curve for intensity over time
- **Use Fade Curve**: Enable/disable smooth animation

---

## How Each Effect Works Technically

### Emission
- Uses `_EmissionColor` shader property with HDR values
- Renders as bright overlay on top of textures
- Can glow very bright (values > 1.0)

### Color Tint
- Multiplies `_Color` shader property to brighten texture
- Preserves texture details and patterns
- Works as a multiplicative tint on the base texture

### Outline
- Uses custom shader (`Custom/SimpleOutline`)
- Expands mesh along vertex normals
- Renders back faces with front culling for clean edges
- Added as extra material pass on existing renderers

---

## Tips & Tricks

**For Different Damage Types:**
- Physical: Tint only or Tint + subtle Emission
- Fire: Emission + Tint with red/orange color
- Ice: Emission + Outline with cyan/blue color
- Electric: Emission only with bright yellow/white
- Poison: Tint with green color

**For Different Game Styles:**
- Realistic: Color Tint only (0.3-0.4)
- Stylized: Outline + Color Tint
- Arcade: All effects combined
- Minimalist: Outline only

**Performance Notes:**
- Outline adds one extra material pass (minimal overhead)
- Emission and Tint have near-zero performance cost
- All effects use material instances (no shared material modification)

---

## Troubleshooting

**Q: Outline looks jagged**
A: Increase Outline Width slightly (try 0.05)

**Q: Can't see tint effect**
A: Increase Tint Strength to 0.6+ or use brighter Flash Color

**Q: Effects too intense/harsh**
A: Lower Emission Intensity and Tint Strength, or disable Emission entirely

**Q: Outline shader not found error**
A: Make sure `OutlineShader.shader` exists in `Assets/Shaders/`

**Q: Want different colors for different effects**
A: Flash Color applies to all effects. For different colors, create multiple HitFlash components with different triggers

---

**Version:** 3.0 (Checkbox System)
**Created:** 2024
**Last Updated:** 2024 (Checkbox system added)
