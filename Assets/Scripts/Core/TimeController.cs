using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
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

        [Tooltip("Use slow motion instead of freeze")]
        [SerializeField] private bool useSlowMotion = false;

        [Tooltip("Slow motion speed (0.1 = 10% speed, 0.5 = 50% speed)")]
        [Range(0.1f, 0.5f)]
        [SerializeField] private float slowMotionSpeed = 0.3f;

        [Space(10)]
        [Header("Who Gets Affected")]
        [Tooltip("Only freeze/slow the attacker and target characters (others keep moving normally)")]
        [SerializeField] private bool affectOnlyInvolvedCharacters = false;

        [Tooltip("Only freeze/slow the TARGET (not the attacker). Makes player attacks feel more responsive.")]
        [SerializeField] private bool freezeTargetOnly = true;

        [Space(10)]
        [Header("Conditions")]
        [Tooltip("When should hitstop trigger involving the player?")]
        [SerializeField] private PlayerHitStopMode playerHitStopMode = PlayerHitStopMode.PlayerAttacksOnly;

        public enum PlayerHitStopMode
        {
            Always,              // Hitstop for all hits (even enemy vs enemy)
            PlayerAttacksOnly,   // Only when player attacks
            PlayerHitOnly,       // Only when player gets hit
            PlayerInvolved       // When player attacks OR gets hit
        }

        [Space(10)]
        [Tooltip("Multiplier for hitstop duration (1 = use weapon config value)")]
        [Range(0f, 3f)]
        [SerializeField] private float hitStopMultiplier = 1f;

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

            GameDebug.Log($"[TimeController] Initialized. HitStop: {enableHitStop}, SlowMo: {useSlowMotion}, InvolvedOnly: {affectOnlyInvolvedCharacters}, TargetOnly: {freezeTargetOnly}, Mode: {playerHitStopMode}",
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

            // Check player involvement based on mode
            bool playerIsAttacker = IsPlayer(e.Attacker);
            bool playerIsTarget = IsPlayer(e.Target);

            bool shouldTrigger = playerHitStopMode switch
            {
                PlayerHitStopMode.Always => true,
                PlayerHitStopMode.PlayerAttacksOnly => playerIsAttacker,
                PlayerHitStopMode.PlayerHitOnly => playerIsTarget,
                PlayerHitStopMode.PlayerInvolved => playerIsAttacker || playerIsTarget,
                _ => false
            };

            if (!shouldTrigger)
            {
                GameDebug.Log($"[TimeController] Skipping hitstop - condition not met (Mode: {playerHitStopMode}, PlayerAttacker: {playerIsAttacker}, PlayerTarget: {playerIsTarget})",
                    config => config.logTimeController);
                return;
            }

            // Apply hitstop based on settings
            float duration = weapon.hitStopDuration * hitStopMultiplier;

            if (affectOnlyInvolvedCharacters)
            {
                if (freezeTargetOnly)
                {
                    // Only affect target - no Time.timeScale manipulation needed
                    if (useSlowMotion)
                        ApplyTargetOnlySlowMotion(e.Target, slowMotionSpeed, duration);
                    else
                        ApplyTargetOnlyFreeze(e.Target, duration);
                }
                else
                {
                    // Freeze/slow both attacker and target using Time.timeScale + selective animator disabling
                    if (useSlowMotion)
                        ApplySelectiveSlowMotion(e.Attacker, e.Target, slowMotionSpeed, duration);
                    else
                        ApplySelectiveFreeze(e.Attacker, e.Target, duration);
                }
            }
            else
            {
                // Global hitstop affects everything (camera still works with unscaled time)
                if (useSlowMotion)
                    ApplySlowMotion(slowMotionSpeed, duration);
                else
                    ApplyGlobalFreeze(duration);
            }

            string mode = affectOnlyInvolvedCharacters ?
                (freezeTargetOnly ? "Target Only" : "Attacker + Target") : "Global";
            string effect = useSlowMotion ? $"SlowMo ({slowMotionSpeed}x)" : "Freeze";
            GameDebug.Log($"[TimeController] Applying {effect} ({mode}): {duration}s",
                config => config.logTimeController);
        }

        /// <summary>
        /// Apply global freeze (entire game pauses).
        /// Camera can still move if using Time.unscaledDeltaTime (FollowCamera does by default).
        /// </summary>
        public void ApplyGlobalFreeze(float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(GlobalFreezeRoutine(duration));
        }

        /// <summary>
        /// Apply slow motion effect (global Time.timeScale).
        /// Camera can still move if using Time.unscaledDeltaTime (FollowCamera does by default).
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
        /// Freeze only the target character (doesn't use Time.timeScale - no side effects).
        /// </summary>
        public void ApplyTargetOnlyFreeze(GameObject target, float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(TargetOnlyFreezeRoutine(target, duration));
        }

        /// <summary>
        /// Slow motion only the target character (doesn't use Time.timeScale - no side effects).
        /// </summary>
        public void ApplyTargetOnlySlowMotion(GameObject target, float speed, float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(TargetOnlySlowMotionRoutine(target, speed, duration));
        }

        /// <summary>
        /// Freeze only attacker and target (others keep moving).
        /// Uses Time.timeScale but disables other animators temporarily.
        /// </summary>
        public void ApplySelectiveFreeze(GameObject attacker, GameObject target, float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(SelectiveFreezeRoutine(attacker, target, duration));
        }

        /// <summary>
        /// Slow motion only for attacker and target (others keep moving).
        /// Uses Time.timeScale but disables other animators temporarily.
        /// </summary>
        public void ApplySelectiveSlowMotion(GameObject attacker, GameObject target, float speed, float duration)
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(SelectiveSlowMotionRoutine(attacker, target, speed, duration));
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

        private IEnumerator GlobalFreezeRoutine(float duration)
        {
            // Freeze entire game
            Time.timeScale = 0f;

            // Wait using real time (unaffected by timeScale)
            yield return new WaitForSecondsRealtime(duration);

            // Restore normal time
            Time.timeScale = originalTimeScale;

            GameDebug.Log($"[TimeController] Global freeze complete",
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

            GameDebug.Log($"[TimeController] Slow motion complete",
                config => config.logTimeController);
        }

        private IEnumerator SelectiveFreezeRoutine(GameObject attacker, GameObject target, float duration)
        {
            // Get all animators and agents in scene
            Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            NavMeshAgent[] allAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

            // Find attacker and target components
            Animator attackerAnim = attacker?.GetComponent<Animator>();
            Animator targetAnim = target?.GetComponent<Animator>();
            NavMeshAgent attackerAgent = attacker?.GetComponent<NavMeshAgent>();
            NavMeshAgent targetAgent = target?.GetComponent<NavMeshAgent>();

            // Store uninvolved animators and set to unscaled time
            List<Animator> unaffectedAnimators = new List<Animator>();
            foreach (Animator anim in allAnimators)
            {
                // Skip target animator (will be frozen)
                if (anim == targetAnim) continue;

                // If freezeTargetOnly, also make attacker unaffected
                bool shouldBeUnaffected = anim != targetAnim && anim.enabled;
                if (freezeTargetOnly)
                {
                    shouldBeUnaffected = anim != targetAnim && anim.enabled;
                }
                else
                {
                    shouldBeUnaffected = anim != attackerAnim && anim != targetAnim && anim.enabled;
                }

                if (shouldBeUnaffected)
                {
                    anim.updateMode = AnimatorUpdateMode.UnscaledTime; // Ignore timeScale
                    unaffectedAnimators.Add(anim);
                }
            }

            // Store uninvolved agents and their speeds
            List<NavMeshAgent> unaffectedAgents = new List<NavMeshAgent>();
            List<float> savedSpeeds = new List<float>();
            foreach (NavMeshAgent agent in allAgents)
            {
                // Skip target agent (will be frozen)
                if (agent == targetAgent) continue;

                // If freezeTargetOnly, also make attacker unaffected
                bool shouldBeUnaffected;
                if (freezeTargetOnly)
                {
                    shouldBeUnaffected = agent != targetAgent && agent.enabled;
                }
                else
                {
                    shouldBeUnaffected = agent != attackerAgent && agent != targetAgent && agent.enabled;
                }

                if (shouldBeUnaffected)
                {
                    unaffectedAgents.Add(agent);
                    savedSpeeds.Add(agent.speed);
                }
            }

            // Freeze time
            Time.timeScale = 0f;

            // Manually update unaffected agents using unscaled time
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float unscaledDelta = Time.unscaledDeltaTime;

                // Manually move unaffected agents
                for (int i = 0; i < unaffectedAgents.Count; i++)
                {
                    NavMeshAgent agent = unaffectedAgents[i];
                    if (agent != null && agent.enabled && agent.hasPath)
                    {
                        // Calculate movement manually
                        Vector3 direction = (agent.steeringTarget - agent.transform.position).normalized;
                        agent.transform.position += direction * savedSpeeds[i] * unscaledDelta;
                    }
                }

                elapsed += unscaledDelta;
                yield return null;
            }

            // Restore time
            Time.timeScale = originalTimeScale;

            // Restore animators
            foreach (Animator anim in unaffectedAnimators)
            {
                if (anim != null)
                {
                    anim.updateMode = AnimatorUpdateMode.Normal;
                }
            }

            GameDebug.Log($"[TimeController] Selective freeze complete: {unaffectedAnimators.Count} animators, {unaffectedAgents.Count} agents unaffected",
                config => config.logTimeController);
        }

        private IEnumerator SelectiveSlowMotionRoutine(GameObject attacker, GameObject target, float speed, float duration)
        {
            // Get all animators and agents in scene
            Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
            NavMeshAgent[] allAgents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

            // Find attacker and target components
            Animator attackerAnim = attacker?.GetComponent<Animator>();
            Animator targetAnim = target?.GetComponent<Animator>();
            NavMeshAgent attackerAgent = attacker?.GetComponent<NavMeshAgent>();
            NavMeshAgent targetAgent = target?.GetComponent<NavMeshAgent>();

            // Store uninvolved animators and set to unscaled time
            List<Animator> unaffectedAnimators = new List<Animator>();
            foreach (Animator anim in allAnimators)
            {
                // Skip target animator (will be slowed)
                if (anim == targetAnim) continue;

                // If freezeTargetOnly, also make attacker unaffected
                bool shouldBeUnaffected;
                if (freezeTargetOnly)
                {
                    shouldBeUnaffected = anim != targetAnim && anim.enabled;
                }
                else
                {
                    shouldBeUnaffected = anim != attackerAnim && anim != targetAnim && anim.enabled;
                }

                if (shouldBeUnaffected)
                {
                    anim.updateMode = AnimatorUpdateMode.UnscaledTime; // Ignore timeScale
                    unaffectedAnimators.Add(anim);
                }
            }

            // Store uninvolved agents and their speeds
            List<NavMeshAgent> unaffectedAgents = new List<NavMeshAgent>();
            List<float> savedSpeeds = new List<float>();
            foreach (NavMeshAgent agent in allAgents)
            {
                // Skip target agent (will be slowed)
                if (agent == targetAgent) continue;

                // If freezeTargetOnly, also make attacker unaffected
                bool shouldBeUnaffected;
                if (freezeTargetOnly)
                {
                    shouldBeUnaffected = agent != targetAgent && agent.enabled;
                }
                else
                {
                    shouldBeUnaffected = agent != attackerAgent && agent != targetAgent && agent.enabled;
                }

                if (shouldBeUnaffected)
                {
                    unaffectedAgents.Add(agent);
                    savedSpeeds.Add(agent.speed);
                }
            }

            // Set slow motion
            Time.timeScale = speed;

            // Manually update unaffected agents at normal speed using unscaled time
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float unscaledDelta = Time.unscaledDeltaTime;

                // Manually move unaffected agents at normal speed
                for (int i = 0; i < unaffectedAgents.Count; i++)
                {
                    NavMeshAgent agent = unaffectedAgents[i];
                    if (agent != null && agent.enabled && agent.hasPath)
                    {
                        // Calculate movement manually at normal speed
                        Vector3 direction = (agent.steeringTarget - agent.transform.position).normalized;
                        agent.transform.position += direction * savedSpeeds[i] * unscaledDelta;
                    }
                }

                elapsed += unscaledDelta;
                yield return null;
            }

            // Restore time
            Time.timeScale = originalTimeScale;

            // Restore animators
            foreach (Animator anim in unaffectedAnimators)
            {
                if (anim != null)
                {
                    anim.updateMode = AnimatorUpdateMode.Normal;
                }
            }

            GameDebug.Log($"[TimeController] Selective slow motion complete: {unaffectedAnimators.Count} animators, {unaffectedAgents.Count} agents unaffected",
                config => config.logTimeController);
        }

        private IEnumerator TargetOnlyFreezeRoutine(GameObject target, float duration)
        {
            if (target == null) yield break;

            // Get target components
            Animator targetAnim = target.GetComponent<Animator>();
            NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();

            // DEBUG: Check if components exist
            GameDebug.Log($"[TimeController] Target components - Animator: {targetAnim != null}, NavMeshAgent: {targetAgent != null}",
                config => config.logTimeController);

            if (targetAnim != null)
            {
                GameDebug.Log($"[TimeController] BEFORE - Animator.speed: {targetAnim.speed}, updateMode: {targetAnim.updateMode}",
                    config => config.logTimeController);
            }
            if (targetAgent != null)
            {
                GameDebug.Log($"[TimeController] BEFORE - Agent.speed: {targetAgent.speed}, isStopped: {targetAgent.isStopped}, enabled: {targetAgent.enabled}",
                    config => config.logTimeController);
            }

            // Store original values
            float originalAnimSpeed = targetAnim != null ? targetAnim.speed : 1f;
            float originalAgentSpeed = targetAgent != null ? targetAgent.speed : 0f;
            bool wasAgentStopped = targetAgent != null && targetAgent.isStopped;

            // Freeze target only (no Time.timeScale manipulation!)
            if (targetAnim != null)
            {
                targetAnim.speed = 0f;
                GameDebug.Log($"[TimeController] Set Animator.speed to 0, now: {targetAnim.speed}",
                    config => config.logTimeController);
            }
            if (targetAgent != null && targetAgent.enabled)
            {
                targetAgent.isStopped = true;
                targetAgent.velocity = Vector3.zero;
                GameDebug.Log($"[TimeController] Set agent.isStopped to true, agent.velocity to zero",
                    config => config.logTimeController);
            }

            GameDebug.Log($"[TimeController] Target-only freeze: {target.name} for {duration}s (no Time.timeScale used)",
                config => config.logTimeController);

            // Wait in real time
            yield return new WaitForSeconds(duration);

            // Restore target
            if (targetAnim != null)
            {
                targetAnim.speed = originalAnimSpeed;
                GameDebug.Log($"[TimeController] RESTORED - Animator.speed to: {targetAnim.speed}",
                    config => config.logTimeController);
            }
            if (targetAgent != null && targetAgent.enabled)
            {
                targetAgent.isStopped = wasAgentStopped;
                targetAgent.speed = originalAgentSpeed;
                GameDebug.Log($"[TimeController] RESTORED - Agent.speed to: {targetAgent.speed}, isStopped: {targetAgent.isStopped}",
                    config => config.logTimeController);
            }

            GameDebug.Log($"[TimeController] Target-only freeze complete",
                config => config.logTimeController);
        }

        private IEnumerator TargetOnlySlowMotionRoutine(GameObject target, float speed, float duration)
        {
            if (target == null) yield break;

            // Get target components
            Animator targetAnim = target.GetComponent<Animator>();
            NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();

            // Store original values
            float originalAnimSpeed = targetAnim != null ? targetAnim.speed : 1f;
            float originalAgentSpeed = targetAgent != null ? targetAgent.speed : 0f;

            // Slow down target only (no Time.timeScale manipulation!)
            if (targetAnim != null) targetAnim.speed = speed;
            if (targetAgent != null && targetAgent.enabled)
            {
                targetAgent.speed = originalAgentSpeed * speed;
            }

            GameDebug.Log($"[TimeController] Target-only slow motion: {target.name} to {speed}x for {duration}s (no Time.timeScale used)",
                config => config.logTimeController);

            // Wait in real time
            yield return new WaitForSeconds(duration);

            // Restore target
            if (targetAnim != null) targetAnim.speed = originalAnimSpeed;
            if (targetAgent != null && targetAgent.enabled)
            {
                targetAgent.speed = originalAgentSpeed;
            }

            GameDebug.Log($"[TimeController] Target-only slow motion complete",
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
