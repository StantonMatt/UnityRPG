using UnityEngine;
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
    public class Knockback : MonoBehaviour
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

        [Header("Physics")]
        [Tooltip("Use NavMeshAgent for knockback (recommended)")]
        [SerializeField] private bool useNavMeshAgent = true;

        [Tooltip("Apply upward force (for 'launch' effect)")]
        [Range(0f, 5f)]
        [SerializeField] private float upwardForce = 0.5f;

        // Component references
        private Mover mover;
        private ActionScheduler actionScheduler;
        private bool isBeingKnockedBack = false;

        private void Awake()
        {
            mover = GetComponent<Mover>();
            actionScheduler = GetComponent<ActionScheduler>();

            if (mover == null && useNavMeshAgent)
            {
                GameDebug.LogWarning($"[Knockback] {gameObject.name} has no Mover component but useNavMeshAgent is true!",
                    config => config.logKnockback, this);
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageDealt += HandleDamageDealt;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageDealt -= HandleDamageDealt;
        }

        private void HandleDamageDealt(CombatEvents.DamageDealtEvent e)
        {
            // Only apply knockback if WE are the target (not the attacker)
            if (e.Target != gameObject) return;
            if (!enableKnockback) return;

            // Get weapon config (future - check if knockback enabled)
            // For now, we assume all hits can cause knockback if enabled

            // Calculate knockback direction (away from attacker)
            Vector3 knockbackDirection = (transform.position - e.Attacker.transform.position).normalized;
            knockbackDirection.y = 0f; // Keep on ground plane

            // Apply knockback
            float force = 5f * knockbackMultiplier; // Default force (will come from weapon config later)
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

            // Cancel current action (stop attacking/moving)
            if (disableControlDuringKnockback && actionScheduler != null)
            {
                actionScheduler.CancelCurrentAction();
            }

            if (useNavMeshAgent && mover != null)
            {
                // Use NavMeshAgent for knockback - simplified to avoid excessive pathfinding queries
                Vector3 targetPosition = transform.position + (direction * force);

                // Move to final position once (NavMesh will path there)
                mover.MoveTo(targetPosition, false);

                // Wait for knockback duration
                yield return new WaitForSeconds(knockbackDuration);
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
    }
}
