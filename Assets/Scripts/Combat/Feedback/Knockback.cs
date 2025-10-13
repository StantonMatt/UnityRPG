using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using RPG.Core;
using RPG.Movement;

namespace RPG.Combat.Feedback
{
    /// <summary>
    /// Applies knockback force when this character is hit.
    /// Pushes target away from attacker for dramatic impact feel.
    /// Works with NavMeshAgent or CharacterController movement.
    /// </summary>
    [RequireComponent(typeof(ActionScheduler))]
    public class Knockback : MonoBehaviour, IAction
    {
        [Header("Knockback Settings")]
        [Tooltip("Enable knockback globally for this character")]
        [SerializeField] private bool enableKnockback = true;

        [Tooltip("Multiplier for knockback force (1 = use weapon config value)")]
        [Range(0f, 3f)]
        [SerializeField] private float knockbackMultiplier = 1f;

        [Tooltip("Duration of knockback movement")]
        [Range(0.1f, 1f)]
        [SerializeField] private float knockbackDuration = 0.3f;

        [Tooltip("If true, character cannot act during knockback")]
        [SerializeField] private bool disableControlDuringKnockback = true;

        [Tooltip("Additional time to wait after knockback movement (for animation to finish)")]
        [Range(0f, 1f)]
        [SerializeField] private float recoveryTime = 0.2f;

        [Header("Physics")]
        [Tooltip("Use NavMeshAgent for knockback (recommended)")]
        [SerializeField] private bool useNavMeshAgent = true;

        [Header("Animation")]
        [Tooltip("Animation trigger name for knockback (leave empty to disable)")]
        [SerializeField] private string knockbackTrigger = "GetHit";

        [Tooltip("Bool parameter to force exit from knockback animation (set to true during, false when done)")]
        [SerializeField] private string knockbackBoolParameter = "isKnockedBack";

        [Tooltip("Auto-calculate recovery time from animation length (recommended)")]
        [SerializeField] private bool autoCalculateRecovery = true;

        [Tooltip("Override: Manually set total incapacitation time (knockback + recovery). Animation transitions early.")]
        [Range(0f, 2f)]
        [SerializeField] private float totalIncapacitationTime = 0f;

        // Component references
        private NavMeshAgent agent;
        private Animator animator;
        private ActionScheduler actionScheduler;
        private bool isBeingKnockedBack = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            actionScheduler = GetComponent<ActionScheduler>();

            if (agent == null && useNavMeshAgent)
            {
                GameDebug.LogWarning($"[Knockback] {gameObject.name} has no NavMeshAgent component but useNavMeshAgent is true!",
                    config => config.logKnockback, this);
            }
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
            // Only apply knockback if WE are the target (not the attacker)
            if (e.Target != gameObject) return;
            if (!enableKnockback) return;

            // Check if weapon has knockback enabled
            if (e.Weapon == null || !e.Weapon.enableKnockback) return;

            // Calculate knockback direction (away from attacker)
            Vector3 knockbackDirection = (transform.position - e.Attacker.transform.position).normalized;
            knockbackDirection.y = 0f; // Keep on ground plane

            // Apply knockback using weapon config
            float force = e.Weapon.knockbackForce * knockbackMultiplier;
            ApplyKnockback(knockbackDirection, force);

            GameDebug.Log($"[Knockback] {gameObject.name} knocked back by {e.Attacker.name}, force: {force}, direction: {knockbackDirection}",
                config => config.logKnockback, this);
        }

        /// <summary>
        /// Apply knockback force in a direction.
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (isBeingKnockedBack) return; // Already being knocked back

            StartCoroutine(KnockbackRoutine(direction.normalized, force));
        }

        private IEnumerator KnockbackRoutine(Vector3 direction, float force)
        {
            isBeingKnockedBack = true;

            // Register knockback as the current action (prevents AI from acting)
            if (disableControlDuringKnockback && actionScheduler != null)
            {
                actionScheduler.StartAction(this);
            }

            // Calculate recovery time
            float calculatedRecoveryTime = recoveryTime;

            if (autoCalculateRecovery && animator != null && !string.IsNullOrEmpty(knockbackTrigger))
            {
                // Get animation clip length
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    // Find the knockback animation clip
                    foreach (var clip in clipInfo)
                    {
                        if (clip.clip.name.Contains(knockbackTrigger) ||
                            animator.GetCurrentAnimatorStateInfo(0).IsName(knockbackTrigger))
                        {
                            float animLength = clip.clip.length;

                            // If override is set, use it; otherwise use full animation length
                            if (totalIncapacitationTime > 0)
                            {
                                calculatedRecoveryTime = totalIncapacitationTime - knockbackDuration;
                            }
                            else
                            {
                                calculatedRecoveryTime = Mathf.Max(0, animLength - knockbackDuration);
                            }

                            GameDebug.Log($"[Knockback] Auto-calculated recovery: {calculatedRecoveryTime}s (anim: {animLength}s, override: {totalIncapacitationTime}s)",
                                config => config.logKnockback, this);
                            break;
                        }
                    }
                }
            }
            else if (totalIncapacitationTime > 0)
            {
                // Manual override without animation
                calculatedRecoveryTime = totalIncapacitationTime - knockbackDuration;
            }

            // Set knockback bool parameter (allows animator to stay in knockback state)
            if (animator != null && !string.IsNullOrEmpty(knockbackBoolParameter))
            {
                animator.SetBool(knockbackBoolParameter, true);
            }

            // Trigger knockback animation
            if (animator != null && !string.IsNullOrEmpty(knockbackTrigger))
            {
                animator.SetTrigger(knockbackTrigger);
                GameDebug.Log($"[Knockback] Triggered animation: {knockbackTrigger}",
                    config => config.logKnockback, this);
            }

            if (useNavMeshAgent && agent != null && agent.enabled)
            {
                // Completely stop NavMeshAgent pathfinding
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true; // CRITICAL: Prevents NavMeshAgent from moving at all

                // Apply knockback by directly moving transform
                float elapsed = 0f;
                Vector3 startPosition = transform.position;
                Vector3 targetPosition = startPosition + (direction * force);

                while (elapsed < knockbackDuration)
                {
                    float delta = Time.deltaTime;
                    elapsed += delta;
                    float t = elapsed / knockbackDuration;

                    // Ease out curve (starts fast, slows down)
                    float easeValue = 1f - (1f - t) * (1f - t);

                    // Move transform directly (bypasses NavMeshAgent)
                    transform.position = Vector3.Lerp(startPosition, targetPosition, easeValue);

                    yield return null;
                }

                // Re-enable NavMeshAgent
                agent.isStopped = false;
            }
            else
            {
                // Fallback: Direct transform movement (not NavMesh-aware)
                float elapsed = 0f;
                Vector3 startPosition = transform.position;
                Vector3 endPosition = startPosition + (direction * force);

                while (elapsed < knockbackDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / knockbackDuration;

                    // Ease out
                    float easeValue = 1f - (1f - t) * (1f - t);

                    transform.position = Vector3.Lerp(startPosition, endPosition, easeValue);

                    yield return null;
                }
            }

            // Wait for animation to finish (recovery time)
            if (calculatedRecoveryTime > 0)
            {
                GameDebug.Log($"[Knockback] {gameObject.name} waiting {calculatedRecoveryTime}s for animation recovery",
                    config => config.logKnockback, this);
                yield return new WaitForSeconds(calculatedRecoveryTime);
            }

            // Clear knockback bool parameter (forces animator to transition out)
            if (animator != null && !string.IsNullOrEmpty(knockbackBoolParameter))
            {
                animator.SetBool(knockbackBoolParameter, false);
                GameDebug.Log($"[Knockback] Cleared {knockbackBoolParameter} - animator should transition out",
                    config => config.logKnockback, this);
            }

            isBeingKnockedBack = false;

            GameDebug.Log($"[Knockback] {gameObject.name} knockback complete",
                config => config.logKnockback, this);
        }

        /// <summary>
        /// Check if currently being knocked back.
        /// </summary>
        public bool IsKnockedBack()
        {
            return isBeingKnockedBack;
        }

        /// <summary>
        /// IAction implementation - called when another action takes priority.
        /// We don't allow canceling knockback (it must complete).
        /// </summary>
        public void Cancel()
        {
            // Knockback cannot be canceled - must complete
            GameDebug.Log($"[Knockback] Attempted to cancel knockback on {gameObject.name} - ignored",
                config => config.logKnockback, this);
        }
    }
}
