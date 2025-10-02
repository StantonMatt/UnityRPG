using UnityEngine;
using RPG.Movement;
using RPG.Core;
using RPG.Stats;

namespace RPG.Combat
{
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(ActionScheduler))]
    public class Fighter : MonoBehaviour, IAction
    {
        [Header("Weapon Configuration")]
        [Tooltip("Weapon data (damage, range, sounds, VFX, etc.)")]
        [SerializeField] private WeaponConfig weaponConfig;

        private CombatTarget target;
        private Health targetHealth; // Cache for damage dealing
        private Transform targetTransform; // Cache for performance
        private Collider targetCollider; // Cache for accurate distance calculation
        private float timeSinceLastAttack = Mathf.Infinity;
        private Mover mover;
        private ActionScheduler actionScheduler;
        private Animator animator;
        private Health health; // Cache our own health component
        private bool isAttacking = false; // Track if currently in attack animation

        private void Start()
        {
            mover = GetComponent<Mover>();
            actionScheduler = GetComponent<ActionScheduler>();
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            timeSinceLastAttack += Time.deltaTime;

            // Don't do anything if we're dead
            if (health != null && health.IsDead()) return;

            if (target == null) return;
            if (targetHealth != null && targetHealth.IsDead()) return; // Stop attacking dead targets

            // If currently in attack animation, stay rooted (don't move even if out of range)
            if (isAttacking)
            {
                AttackBehavior(); // Continue attack behavior (rotation, etc.)
                return;
            }

            if (!GetIsInRange())
            {
                // Move to target without registering as action (Fighter already registered)
                mover.MoveTo(targetTransform.position, registerAction: false);
            }
            else
            {
                AttackBehavior();
            }
        }

        private void AttackBehavior()
        {
            // Stop movement - both animation and actual NavMeshAgent movement
            mover.Cancel(); // Root in place during attack - prevents moonwalking

            // Stop movement animation while attacking
            if (animator != null)
            {
                animator.SetFloat("forwardSpeed", 0f);
            }

            // Face the target (only rotate on Y-axis to prevent tilting)
            Vector3 lookDirection = targetTransform.position - transform.position;
            lookDirection.y = 0; // Remove vertical component to prevent tilting
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            if (timeSinceLastAttack >= (weaponConfig?.attackCooldown ?? 1f))
            {
                // Trigger attack animation - this will call Hit() via Animation Event
                TriggerAttack();
                timeSinceLastAttack = 0;
            }
        }

        private void TriggerAttack()
        {
            if (weaponConfig == null)
            {
                GameDebug.LogError($"{gameObject.name} has no WeaponConfig assigned!", this);
                return;
            }

            // Fire attack started event (for swing sounds, weapon trails)
            CombatEvents.RaiseAttackStarted(new CombatEvents.AttackStartedEvent(
                gameObject,
                weaponConfig
            ));

            if (animator != null)
            {
                animator.ResetTrigger("stopAttack"); // Clear any stop triggers first
                animator.ResetTrigger("attack"); // Clear any pending attack triggers
                animator.SetTrigger("attack"); // Trigger attack animation
            }

            isAttacking = true; // Lock in place during attack animation

            // Unlock after attack ANIMATION completes (not cooldown)
            Invoke(nameof(StopAttacking), weaponConfig.animationDuration);
        }

        /// <summary>
        /// Called after attack animation completes to allow movement again.
        /// </summary>
        private void StopAttacking()
        {
            isAttacking = false; // Allow movement again
        }

        /// <summary>
        /// Called by Animation Event at the point in the attack animation where damage should be dealt.
        /// Add this as an Animation Event in your attack animation clip.
        /// </summary>
        private void Hit()
        {
            // If target was cleared (action cancelled), don't deal damage
            if (target == null) return;
            if (targetHealth == null) return;
            if (weaponConfig == null) return;

            // Calculate hit point (where VFX should spawn)
            Vector3 hitPoint = targetCollider != null
                ? targetCollider.ClosestPoint(transform.position)
                : targetTransform.position;

            // Calculate hit normal (direction for VFX orientation)
            Vector3 hitNormal = (transform.position - hitPoint).normalized;

            // Deal damage to the target with hit data
            targetHealth.TakeDamage(weaponConfig.damage, gameObject, hitPoint, hitNormal);

            // Fire attack hit event (for impact sounds, VFX, hitstop, camera shake)
            CombatEvents.RaiseAttackHit(new CombatEvents.AttackHitEvent(
                gameObject,
                target.gameObject,
                weaponConfig,
                hitPoint,
                hitNormal
            ));
        }

        private bool GetIsInRange()
        {
            if (weaponConfig == null) return false;

            if (targetCollider != null)
            {
                // Calculate distance from player position to closest point on target's collider
                Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
                float distanceToSurface = Vector3.Distance(transform.position, closestPoint);
                return distanceToSurface < weaponConfig.range;
            }

            // Fallback to center-to-center if no collider
            return Vector3.Distance(transform.position, targetTransform.position) < weaponConfig.range;
        }

        public bool CanAttack(GameObject combatTarget)
        {
            if (combatTarget == null) return false;
            if (combatTarget == gameObject) return false; // Can't attack yourself!

            CombatTarget targetToTest = combatTarget.GetComponent<CombatTarget>();
            return targetToTest != null;
        }

        public void Attack(GameObject combatTarget)
        {
            // Validate target before attacking
            if (!CanAttack(combatTarget))
            {
                GameDebug.LogWarning($"{gameObject.name} tried to attack invalid target {(combatTarget != null ? combatTarget.name : "NULL")}",
                    config => config.logFighterState, this);
                return;
            }

            actionScheduler.StartAction(this); // Register Fighter as the active action
            target = combatTarget.GetComponent<CombatTarget>();
            targetHealth = combatTarget.GetComponent<Health>(); // Cache health for damage dealing
            targetTransform = combatTarget.transform; // Cache transform
            targetCollider = combatTarget.GetComponent<Collider>(); // Cache collider for range calculation
        }

        public void Cancel()
        {
            // Cancel any pending StopAttacking calls
            CancelInvoke(nameof(StopAttacking));

            // Don't trigger stopAttack if we're dead (death animation takes priority)
            if (health != null && health.IsDead())
            {
                // Already dead, just clear targets without triggering animations
                target = null;
                targetHealth = null;
                targetTransform = null;
                targetCollider = null;
                isAttacking = false;
                return;
            }

            // Stop attack animation only if we have a target (actually attacking)
            if (target != null && animator != null)
            {
                animator.ResetTrigger("attack"); // Clear any queued attack triggers
                animator.SetTrigger("stopAttack"); // Transition out of attack
            }

            target = null;
            targetHealth = null;
            targetTransform = null;
            targetCollider = null;
            isAttacking = false; // Clear attacking flag when action is cancelled
        }

        /// <summary>
        /// Check if this fighter currently has a target.
        /// </summary>
        public bool HasTarget()
        {
            return target != null;
        }
    }
}