using UnityEngine;
using System.Collections;
using RPG.Stats;
using RPG.Core;

namespace RPG.Combat
{
    /// <summary>
    /// Makes all character renderers flash when taking damage.
    /// Works with 3D models by using emission, color tinting, and outlines.
    /// Effects can be combined for custom feedback.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [Header("Flash Effects (Mix and match!)")]
        [Tooltip("Enable emission glow effect (bright overlay)")]
        [SerializeField] private bool useEmission = true;

        [Tooltip("Enable color tint effect (brightens texture while preserving details)")]
        [SerializeField] private bool useColorTint = true;

        [Tooltip("Enable outline effect (colored edge around character)")]
        [SerializeField] private bool useOutline = false;

        [Header("Flash Settings")]
        [Tooltip("Color of the flash effect")]
        [SerializeField] private Color flashColor = Color.white;

        [Tooltip("How long the flash lasts in seconds")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Emission Settings (for Emission/EmissionAndTint modes)")]
        [Tooltip("Brightness of glow effect. Lower = subtle, Higher = intense\n• 0.5-1.5 = Subtle glow\n• 2-4 = Medium glow\n• 5-10 = Bright flash")]
        [Range(0f, 10f)]
        [SerializeField] private float emissionIntensity = 2f;

        [Header("Color Tint Settings (for ColorTint/EmissionAndTint modes)")]
        [Tooltip("Brightness increase for texture. Higher = brighter character\n• 0.2-0.4 = Subtle brighten\n• 0.5-0.7 = Medium brighten\n• 0.8-1.0 = Strong brighten")]
        [Range(0f, 1f)]
        [SerializeField] private float tintStrength = 0.5f;

        [Header("Outline Settings (for Outline/OutlineAndTint modes)")]
        [Tooltip("Thickness of the outline effect")]
        [Range(0.01f, 0.1f)]
        [SerializeField] private float outlineWidth = 0.03f;

        [Tooltip("Color of the outline")]
        [SerializeField] private Color outlineColor = Color.white;

        [Header("Animation")]
        [Tooltip("Use animation curve for fade in/out (leave empty for instant flash)")]
        [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Tooltip("Enable smooth fade in/out using the curve")]
        [SerializeField] private bool useFadeCurve = true;

        // Component references
        private Health health;
        private Renderer[] renderers;
        private Material[][] originalMaterials; // Store original materials for each renderer
        private Material[][] flashMaterials; // Store flash material instances
        private Color[][] originalColors; // Store original base colors for color tint mode

        // Outline effect components
        private Material outlineShaderMaterial; // Outline shader material
        private Material[][] outlineMaterials; // Materials with outline shader applied
        private bool outlineInitialized = false;

        private void Awake()
        {
            health = GetComponent<Health>();

            // Find all renderers in children (including this GameObject)
            renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                GameDebug.LogWarning($"HitFlash on {gameObject.name} found no renderers!",
                    config => config.logHitFlash, this);
                return;
            }

            // Create material instances for each renderer
            originalMaterials = new Material[renderers.Length][];
            flashMaterials = new Material[renderers.Length][];
            originalColors = new Color[renderers.Length][];

            for (int i = 0; i < renderers.Length; i++)
            {
                // Store original materials
                originalMaterials[i] = renderers[i].materials;

                // Create flash material instances and store original colors
                flashMaterials[i] = new Material[originalMaterials[i].Length];
                originalColors[i] = new Color[originalMaterials[i].Length];

                for (int j = 0; j < originalMaterials[i].Length; j++)
                {
                    flashMaterials[i][j] = new Material(originalMaterials[i][j]);

                    // Store original color (try both _Color and _BaseColor for different shader types)
                    if (flashMaterials[i][j].HasProperty("_Color"))
                        originalColors[i][j] = flashMaterials[i][j].GetColor("_Color");
                    else if (flashMaterials[i][j].HasProperty("_BaseColor"))
                        originalColors[i][j] = flashMaterials[i][j].GetColor("_BaseColor");
                    else
                        originalColors[i][j] = Color.white;
                }
            }

            GameDebug.Log($"HitFlash initialized on {gameObject.name} with {renderers.Length} renderer(s)",
                config => config.logHitFlash, this);
        }

        private void InitializeOutline()
        {
            if (outlineInitialized) return;

            // Load outline shader
            Shader outlineShader = Shader.Find("Custom/SimpleOutline");
            if (outlineShader == null)
            {
                GameDebug.LogWarning("Custom/SimpleOutline shader not found! Make sure OutlineShader.shader is in Assets/Shaders/",
                    config => config.logHitFlash, this);
                return;
            }

            // Create outline shader material
            outlineShaderMaterial = new Material(outlineShader);
            outlineShaderMaterial.SetColor("_OutlineColor", outlineColor);
            outlineShaderMaterial.SetFloat("_OutlineWidth", outlineWidth);

            // Create material arrays with outline shader
            // Use FLASH materials (which support emission/tint) + outline shader
            outlineMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] flashMats = flashMaterials[i];
                outlineMaterials[i] = new Material[flashMats.Length + 1]; // +1 for outline

                // Copy flash materials first (these support emission/tint)
                for (int j = 0; j < flashMats.Length; j++)
                {
                    outlineMaterials[i][j] = flashMats[j];
                }

                // Add outline material last
                outlineMaterials[i][flashMats.Length] = outlineShaderMaterial;
            }

            outlineInitialized = true;
            GameDebug.Log($"Outline initialized with shader, color: {outlineColor}, width: {outlineWidth}",
                config => config.logHitFlash, this);
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageTaken += HandleDamageTaken;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageTaken -= HandleDamageTaken;
        }

        private void OnDestroy()
        {
            // Clean up all material instances
            if (flashMaterials != null)
            {
                foreach (var materialArray in flashMaterials)
                {
                    foreach (var material in materialArray)
                    {
                        if (material != null)
                        {
                            Destroy(material);
                        }
                    }
                }
            }

            // Clean up outline shader material
            if (outlineShaderMaterial != null)
            {
                Destroy(outlineShaderMaterial);
            }
        }

        private void HandleDamageTaken(CombatEvents.DamageTakenEvent e)
        {
            if (e.Target != gameObject) return;
            if (health != null && health.IsDead()) return;

            GameDebug.Log($"HitFlash triggered on {gameObject.name}", config => config.logHitFlash, this);
            Flash();
        }

        private void Flash()
        {
            // Don't flash if no effects are enabled
            if (!useEmission && !useColorTint && !useOutline)
            {
                GameDebug.Log($"HitFlash triggered but all effects are disabled",
                    config => config.logHitFlash, this);
                return;
            }

            StopAllCoroutines();
            StartCoroutine(FlashCoroutine());
        }

        private IEnumerator FlashCoroutine()
        {
            // Initialize outline if needed
            if (useOutline && !outlineInitialized)
            {
                GameDebug.Log($"Outline not initialized, creating now...",
                    config => config.logHitFlash, this);
                InitializeOutline();
            }

            // Apply appropriate materials
            if (useOutline && outlineInitialized)
            {
                // Apply outline materials (original + outline shader)
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].materials = outlineMaterials[i];
                }
                GameDebug.Log($"Outline materials applied!",
                    config => config.logHitFlash, this);
            }
            else
            {
                // Apply flash materials (for emission/tint effects)
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].materials = flashMaterials[i];
                }
            }

            if (useFadeCurve && flashCurve != null && flashCurve.length > 0)
            {
                // Animated flash using curve
                float elapsed = 0f;
                while (elapsed < flashDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / flashDuration;
                    float curveValue = flashCurve.Evaluate(t);

                    ApplyFlashEffect(curveValue);

                    yield return null;
                }
            }
            else
            {
                // Instant flash (no fade)
                ApplyFlashEffect(1f);
                yield return new WaitForSeconds(flashDuration);
            }

            // Restore original materials (this also removes outline)
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].materials = originalMaterials[i];
            }
        }

        private void ApplyFlashEffect(float intensity)
        {
            // Update outline shader parameters if outline is enabled
            if (useOutline && outlineShaderMaterial != null)
            {
                Color color = outlineColor;
                color.a = intensity;
                outlineShaderMaterial.SetColor("_OutlineColor", color);
                outlineShaderMaterial.SetFloat("_OutlineWidth", outlineWidth * intensity);
            }

            // Apply emission and/or tint effects if enabled
            // These apply to flashMaterials which are used in both outline and non-outline modes
            if (useEmission || useColorTint)
            {
                // Get the correct materials array based on whether outline is active
                Material[][] targetMaterials = (useOutline && outlineInitialized) ? outlineMaterials : flashMaterials;

                for (int i = 0; i < renderers.Length; i++)
                {
                    // Only iterate over the flash materials, not the outline shader material
                    int matCount = flashMaterials[i].Length;
                    for (int j = 0; j < matCount; j++)
                    {
                        // Get material from currently active array
                        Material mat = targetMaterials[i][j];

                        if (useEmission)
                        {
                            ApplyEmissionOnly(mat, intensity);
                        }

                        if (useColorTint)
                        {
                            ApplyColorTintOnly(mat, originalColors[i][j], intensity);
                        }
                    }
                }
            }
        }

        private void ApplyEmissionOnly(Material mat, float intensity)
        {
            Color emissionColor = flashColor * (emissionIntensity * intensity);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }

        private void ApplyColorTintOnly(Material mat, Color originalColor, float intensity)
        {
            // For textured materials, _Color acts as a multiplier/tint on the texture
            // White (1,1,1) = normal texture, brighter values = brightened texture

            // Calculate brightness multiplier (higher tint strength = more brightening)
            float brightnessMultiplier = 1f + (tintStrength * intensity * 3f); // Range: 1.0 to 4.0

            // Brighten the original color
            Color brightenedColor = originalColor * brightnessMultiplier;

            // Blend toward flash color for tinting effect
            float tintAmount = tintStrength * intensity * 0.5f;
            Color tintedColor = Color.Lerp(brightenedColor, flashColor * brightnessMultiplier, tintAmount);

            // Keep alpha at 1
            tintedColor.a = 1f;

            // Set color on both possible properties (don't clamp - allow HDR brightening)
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tintedColor);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tintedColor);
        }
    }
}
