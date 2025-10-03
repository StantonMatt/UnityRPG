using UnityEngine;
using System.Collections;
using RPG.Combat;
using RPG.Core;

namespace RPG.CameraControl
{
    /// <summary>
    /// Shakes the camera on impact for dramatic effect.
    /// Attach to Main Camera or camera rig.
    /// Listens to combat events to determine when to shake.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Enable camera shake globally")]
        [SerializeField] private bool enableShake = true;

        [Tooltip("Multiplier for shake intensity (1 = use weapon config value)")]
        [Range(0f, 3f)]
        [SerializeField] private float shakeMultiplier = 1f;

        [Tooltip("Only shake camera if player is involved (attacker or target)")]
        [SerializeField] private bool playerOnlyShake = true;

        [Tooltip("Default shake duration if weapon doesn't specify")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float defaultShakeDuration = 0.2f;

        [Header("Shake Pattern")]
        [Tooltip("How shake intensity decreases over time")]
        [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // State
        private Vector3 originalLocalPosition;
        private Coroutine shakeCoroutine;

        private void Start()
        {
            originalLocalPosition = transform.localPosition;

            GameDebug.Log($"[CameraShake] Initialized. Shake enabled: {enableShake}, original position: {originalLocalPosition}",
                config => config.logCameraShake, this);
        }

        private void OnEnable()
        {
            CombatEvents.OnAttackHit += HandleAttackHit;
        }

        private void OnDisable()
        {
            CombatEvents.OnAttackHit -= HandleAttackHit;
        }

        private void HandleAttackHit(CombatEvents.AttackHitEvent e)
        {
            if (!enableShake) return;

            WeaponConfig weapon = e.Weapon;
            if (weapon == null || !weapon.enableCameraShake) return;

            // Check if player is involved
            if (playerOnlyShake)
            {
                bool playerInvolved = IsPlayer(e.Attacker) || IsPlayer(e.Target);
                if (!playerInvolved)
                {
                    GameDebug.Log($"[CameraShake] Skipping shake - player not involved",
                        config => config.logCameraShake, this);
                    return;
                }
            }

            // Apply shake
            float intensity = weapon.cameraShakeIntensity * shakeMultiplier;
            float duration = weapon.cameraShakeDuration > 0 ? weapon.cameraShakeDuration : defaultShakeDuration;

            Shake(intensity, duration);

            GameDebug.Log($"[CameraShake] Shaking camera: intensity {intensity}, duration {duration}s",
                config => config.logCameraShake, this);
        }

        /// <summary>
        /// Manually trigger camera shake.
        /// </summary>
        public void Shake(float intensity, float duration)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Get shake strength from curve
                float curveValue = shakeCurve.Evaluate(t);
                float currentIntensity = intensity * curveValue;

                // Random offset
                float x = Random.Range(-1f, 1f) * currentIntensity;
                float y = Random.Range(-1f, 1f) * currentIntensity;

                // Apply shake
                transform.localPosition = originalLocalPosition + new Vector3(x, y, 0f);

                yield return null;
            }

            // Restore original position
            transform.localPosition = originalLocalPosition;

            GameDebug.Log($"[CameraShake] Shake complete, restored position to {originalLocalPosition}",
                config => config.logCameraShake, this);
        }

        /// <summary>
        /// Instantly stop shake and restore position.
        /// </summary>
        public void StopShake()
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }

            transform.localPosition = originalLocalPosition;
        }

        private bool IsPlayer(GameObject obj)
        {
            return obj != null && obj.CompareTag("Player");
        }

        // Reset position if disabled mid-shake
        private void OnDestroy()
        {
            StopShake();
        }
    }
}
