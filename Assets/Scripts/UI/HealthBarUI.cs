using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using RPG.Stats;
using RPG.Combat;
using RPG.Core;

namespace RPG.UI
{
    /// <summary>
    /// Displays an animated health bar above a character.
    /// Listens to damage events and smoothly animates the fill amount.
    /// Supports world-space canvas that billboards toward camera.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Health Bar Setup")]
        [Tooltip("Canvas containing the health bar UI (can be world-space or child canvas)")]
        [SerializeField] private Canvas healthBarCanvas;

        [Tooltip("Image component that fills based on health (typically with Fill Amount image type)")]
        [SerializeField] private Image fillImage;

        [Tooltip("Delayed damage bar (shows recent damage before fading) - optional")]
        [SerializeField] private Image delayedDamageImage;

        [Tooltip("Background image (optional - for visual contrast)")]
        [SerializeField] private Image backgroundImage;

        [Header("Positioning")]
        [Tooltip("Offset from character position (in world units)")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);

        [Tooltip("Enable billboard effect (always face camera)")]
        [SerializeField] private bool billboardToCamera = true;

        [Header("Animation")]
        [Tooltip("Duration of health bar fill animation in seconds (instant drop to new health)")]
        [Range(0f, 0.2f)]
        [SerializeField] private float animationDuration = 0.05f;

        [Tooltip("Duration for delayed damage bar to drain (the 'recent damage' effect)")]
        [Range(0.3f, 2f)]
        [SerializeField] private float delayedDamageDuration = 0.8f;

        [Tooltip("Animation curve for fill amount (ease in/out recommended)")]
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Visibility")]
        [Tooltip("Hide health bar when at full health")]
        [SerializeField] private bool hideWhenFull = true;

        [Tooltip("Hide health bar when dead")]
        [SerializeField] private bool hideWhenDead = true;

        [Tooltip("Delay before hiding bar after reaching full health (seconds)")]
        [SerializeField] private float hideDelay = 2f;

        [Header("Colors")]
        [Tooltip("Color when health is high (> 60%)")]
        [SerializeField] private Color highHealthColor = Color.green;

        [Tooltip("Color when health is medium (30-60%)")]
        [SerializeField] private Color mediumHealthColor = Color.yellow;

        [Tooltip("Color when health is low (< 30%)")]
        [SerializeField] private Color lowHealthColor = Color.red;

        [Tooltip("Enable color gradient based on health percentage")]
        [SerializeField] private bool useColorGradient = true;

        [Header("Delayed Damage Bar")]
        [Tooltip("Color for the delayed damage bar (darker version of health color recommended)")]
        [SerializeField] private Color delayedDamageColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Component references
        private Health health;
        private Camera mainCamera;
        private Coroutine animationCoroutine;
        private Coroutine delayedDamageCoroutine;
        private Coroutine hideCoroutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            mainCamera = Camera.main;

            // Validate setup
            if (healthBarCanvas == null)
            {
                GameDebug.LogWarning($"[HealthBarUI] {gameObject.name} has no health bar canvas assigned!",
                    config => config.logHealthSystem, this);
            }

            if (fillImage == null)
            {
                GameDebug.LogWarning($"[HealthBarUI] {gameObject.name} has no fill image assigned!",
                    config => config.logHealthSystem, this);
            }

            // Initialize health bar to full
            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
                UpdateHealthColor(1f);
            }

            // Initialize delayed damage bar
            if (delayedDamageImage != null)
            {
                delayedDamageImage.fillAmount = 1f;
                delayedDamageImage.color = delayedDamageColor;
            }

            // Hide if configured
            if (hideWhenFull && healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageTaken += HandleDamageTaken;
            CombatEvents.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageTaken -= HandleDamageTaken;
            CombatEvents.OnDeath -= HandleDeath;
        }

        private void HandleDamageTaken(CombatEvents.DamageTakenEvent e)
        {
            // Only update if WE are the target
            if (e.Target != gameObject) return;

            // Show health bar if it was hidden
            if (healthBarCanvas != null && !healthBarCanvas.gameObject.activeSelf)
            {
                healthBarCanvas.gameObject.SetActive(true);
            }

            // Cancel any pending hide
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            // Calculate target fill amount
            float targetFill = e.CurrentHealth / e.MaxHealth;

            GameDebug.Log($"[HealthBarUI] {gameObject.name} updating health bar to {targetFill:P0} ({e.CurrentHealth}/{e.MaxHealth})",
                config => config.logHealthSystem, this);

            // Store old fill amount for delayed damage bar
            float oldFill = fillImage != null ? fillImage.fillAmount : targetFill;

            // Animate main bar to new value (fast/instant)
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateFill(targetFill));

            // Animate delayed damage bar (slow drain from old position)
            if (delayedDamageImage != null && targetFill < oldFill)
            {
                if (delayedDamageCoroutine != null)
                {
                    StopCoroutine(delayedDamageCoroutine);
                }
                delayedDamageCoroutine = StartCoroutine(AnimateDelayedDamage(oldFill, targetFill));
            }

            // Schedule hide if at full health
            if (hideWhenFull && targetFill >= 1f)
            {
                hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }

        private void HandleDeath(CombatEvents.DeathEvent e)
        {
            // Only react if WE died
            if (e.Target != gameObject) return;

            if (hideWhenDead && healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(false);
                GameDebug.Log($"[HealthBarUI] {gameObject.name} hiding health bar (dead)",
                    config => config.logHealthSystem, this);
            }
        }

        private IEnumerator AnimateFill(float targetFill)
        {
            if (fillImage == null) yield break;

            float startFill = fillImage.fillAmount;
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;

                // Apply animation curve
                float curveValue = animationCurve.Evaluate(t);

                // Lerp fill amount
                float currentFill = Mathf.Lerp(startFill, targetFill, curveValue);
                fillImage.fillAmount = currentFill;

                // Update color if gradient enabled
                if (useColorGradient)
                {
                    UpdateHealthColor(currentFill);
                }

                yield return null;
            }

            // Ensure exact final value
            fillImage.fillAmount = targetFill;
            if (useColorGradient)
            {
                UpdateHealthColor(targetFill);
            }
        }

        private void UpdateHealthColor(float healthPercentage)
        {
            if (fillImage == null) return;

            // Determine color based on health percentage
            Color targetColor;
            if (healthPercentage > 0.6f)
            {
                // High health: Green
                targetColor = highHealthColor;
            }
            else if (healthPercentage > 0.3f)
            {
                // Medium health: Interpolate between green and yellow
                float t = (healthPercentage - 0.3f) / 0.3f; // Map 0.3-0.6 to 0-1
                targetColor = Color.Lerp(mediumHealthColor, highHealthColor, t);
            }
            else
            {
                // Low health: Interpolate between red and yellow
                float t = healthPercentage / 0.3f; // Map 0-0.3 to 0-1
                targetColor = Color.Lerp(lowHealthColor, mediumHealthColor, t);
            }

            fillImage.color = targetColor;
        }

        private IEnumerator AnimateDelayedDamage(float startFill, float targetFill)
        {
            if (delayedDamageImage == null) yield break;

            // Start at old position
            delayedDamageImage.fillAmount = startFill;

            float elapsed = 0f;

            while (elapsed < delayedDamageDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / delayedDamageDuration;

                // Apply animation curve for smooth drain
                float curveValue = animationCurve.Evaluate(t);

                // Lerp from old fill to new fill
                float currentFill = Mathf.Lerp(startFill, targetFill, curveValue);
                delayedDamageImage.fillAmount = currentFill;

                yield return null;
            }

            // Ensure exact final value
            delayedDamageImage.fillAmount = targetFill;
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(hideDelay);

            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(false);
                GameDebug.Log($"[HealthBarUI] {gameObject.name} hiding health bar (full health)",
                    config => config.logHealthSystem, this);
            }
        }

        private void LateUpdate()
        {
            // Update position and rotation every frame
            if (healthBarCanvas == null || mainCamera == null) return;

            // Position at character + offset
            healthBarCanvas.transform.position = transform.position + worldOffset;

            // Billboard toward camera
            if (billboardToCamera)
            {
                healthBarCanvas.transform.LookAt(mainCamera.transform);
                healthBarCanvas.transform.Rotate(0, 180, 0); // Face camera (not back of canvas)
            }
        }
    }
}
