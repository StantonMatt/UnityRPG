using UnityEngine;
using System.Collections;
using RPG.Combat;

namespace RPG.Core
{
    /// <summary>
    /// Global time manipulation system for game feel effects.
    /// Handles hitstop (brief pause on impact), slow motion, and time freezes.
    /// Singleton pattern - only one instance exists.
    /// </summary>
    public class TimeController : MonoBehaviour
    {
        public static TimeController Instance { get; private set; }

        [Header("HitStop Settings")]
        [Tooltip("Enable hitstop effect globally")]
        [SerializeField] private bool enableHitStop = true;

        [Tooltip("Multiplier for hitstop duration (1 = use weapon config value)")]
        [Range(0f, 3f)]
        [SerializeField] private float hitStopMultiplier = 1f;

        [Tooltip("Only apply hitstop if player is involved (attacker or target)")]
        [SerializeField] private bool playerOnlyHitStop = true;

        // Active hitstop coroutine
        private Coroutine hitStopCoroutine;
        private float originalTimeScale = 1f;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            originalTimeScale = Time.timeScale;

            GameDebug.Log($"[TimeController] Initialized. HitStop enabled: {enableHitStop}",
                config => config.logTimeController);
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
            if (!enableHitStop) return;

            WeaponConfig weapon = e.Weapon;
            if (weapon == null || !weapon.enableHitStop) return;

            // Check if player is involved
            if (playerOnlyHitStop)
            {
                bool playerInvolved = IsPlayer(e.Attacker) || IsPlayer(e.Target);
                if (!playerInvolved)
                {
                    GameDebug.Log($"[TimeController] Skipping hitstop - player not involved",
                        config => config.logTimeController);
                    return;
                }
            }

            // Apply hitstop
            float duration = weapon.hitStopDuration * hitStopMultiplier;
            ApplyHitStop(duration);

            GameDebug.Log($"[TimeController] Applying hitstop: {duration}s (weapon: {weapon.name})",
                config => config.logTimeController);
        }

        /// <summary>
        /// Apply a brief time freeze (hitstop effect).
        /// </summary>
        public void ApplyHitStop(float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
        }

        /// <summary>
        /// Apply slow motion effect.
        /// </summary>
        public void ApplySlowMotion(float timeScale, float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(SlowMotionRoutine(timeScale, duration));
        }

        /// <summary>
        /// Instantly restore normal time.
        /// </summary>
        public void RestoreNormalTime()
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
                hitStopCoroutine = null;
            }

            Time.timeScale = originalTimeScale;
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            // Freeze time
            Time.timeScale = 0f;

            // Wait using real time (unaffected by timeScale)
            yield return new WaitForSecondsRealtime(duration);

            // Restore normal time
            Time.timeScale = originalTimeScale;

            GameDebug.Log($"[TimeController] Hitstop complete, restored timeScale to {originalTimeScale}",
                config => config.logTimeController);
        }

        private IEnumerator SlowMotionRoutine(float slowTimeScale, float duration)
        {
            // Set slow motion
            Time.timeScale = slowTimeScale;

            GameDebug.Log($"[TimeController] Slow motion started: {slowTimeScale}x for {duration}s",
                config => config.logTimeController);

            // Wait using scaled time
            yield return new WaitForSeconds(duration);

            // Restore normal time
            Time.timeScale = originalTimeScale;

            GameDebug.Log($"[TimeController] Slow motion complete, restored timeScale to {originalTimeScale}",
                config => config.logTimeController);
        }

        private bool IsPlayer(GameObject obj)
        {
            return obj != null && obj.CompareTag("Player");
        }

        private void OnDestroy()
        {
            // Ensure time is restored when destroyed
            RestoreNormalTime();
        }

#if UNITY_EDITOR
        // Editor helper - visualize current time scale
        private void OnGUI()
        {
            if (Time.timeScale < 1f)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, 10, 200, 20), $"TimeScale: {Time.timeScale:F2}");
            }
        }
#endif
    }
}
