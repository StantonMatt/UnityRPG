using UnityEngine;
using RPG.Core;
using RPG.Combat;
using RPG.Control;

namespace RPG.Stats
{
    /// <summary>
    /// Manages health for any character (player or enemy).
    /// Handles damage, healing, and death.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [Min(1)]
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private bool isDead = false;
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Apply damage to this character.
        /// </summary>
        /// <param name="damage">Amount of damage to deal</param>
        /// <param name="instigator">The GameObject that dealt the damage (optional)</param>
        /// <param name="hitPoint">World position where hit occurred (for VFX)</param>
        /// <param name="hitNormal">Normal direction of hit (for VFX orientation)</param>
        public void TakeDamage(float damage, GameObject instigator = null, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (isDead) return; // Already dead, can't take more damage

            currentHealth = Mathf.Max(currentHealth - damage, 0f);

            GameDebug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}",
                config => config.logHealthSystem, this);
            GameDebug.Log($"{gameObject.name} was attacked by {(instigator != null ? instigator.name : "UNKNOWN - NO INSTIGATOR!")}",
                config => config.logHealthSystem, this);

            // Fire event for feedback systems (flash, sound, VFX, etc.)
            CombatEvents.RaiseDamageTaken(new CombatEvents.DamageTakenEvent(
                gameObject,
                damage,
                currentHealth,
                maxHealth
            ));

            // Fire event from attacker's perspective
            if (instigator != null)
            {
                CombatEvents.RaiseDamageDealt(new CombatEvents.DamageDealtEvent(
                    instigator,
                    gameObject,
                    damage,
                    hitPoint,
                    hitNormal
                ));

                AggravateNearbyEnemies(instigator);
            }

            if (currentHealth <= 0)
            {
                Die(instigator);
            }
        }

        /// <summary>
        /// React to being attacked - behavior depends on AI configuration.
        /// </summary>
        private void AggravateNearbyEnemies(GameObject instigator)
        {
            // Don't auto-attack if this is the player (player attacks only on input)
            if (GetComponent<PlayerController>() != null)
            {
                return; // Player-controlled - don't auto-aggro
            }

            // Check if this AI should retaliate based on behavior type
            AIController aiController = GetComponent<AIController>();
            if (aiController != null && !aiController.ShouldRetaliateWhenAttacked())
            {
                GameDebug.Log($"{gameObject.name} was attacked but AI behavior prevents retaliation.",
                    config => config.logHealthSystem, this);
                return; // Neutral or Coward AI - don't fight back
            }

            GameDebug.Log($"{gameObject.name} is aggroing! Looking for Fighter component...",
                config => config.logHealthSystem, this);

            // Make this character attack back (AI only)
            Fighter fighter = GetComponent<Fighter>();

            if (fighter != null)
            {
                GameDebug.Log($"{gameObject.name} has Fighter! Attacking {instigator.name}!",
                    config => config.logHealthSystem, this);
            }
            else
            {
                GameDebug.LogWarning($"{gameObject.name} does NOT have Fighter component - cannot fight back!",
                    config => config.logHealthSystem, this);
            }

            if (fighter != null)
            {
                fighter.Attack(instigator);
            }

            // TODO: Future - make nearby allies also attack
            // TODO: Future - implement flee behavior for Coward AI
        }

        /// <summary>
        /// Heal this character.
        /// </summary>
        /// <param name="amount">Amount of health to restore</param>
        public void Heal(float amount)
        {
            if (isDead) return; // Can't heal the dead

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

            GameDebug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}",
                config => config.logHealthSystem, this);
        }

        /// <summary>
        /// Handle death.
        /// </summary>
        private void Die(GameObject killer)
        {
            if (isDead) return; // Already dead

            isDead = true;

            GameDebug.Log($"{gameObject.name} has died!", config => config.logHealthSystem, this);

            // Fire death event
            CombatEvents.RaiseDeath(new CombatEvents.DeathEvent(gameObject, killer));

            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("die");
            }

            // Cancel current action (stops attacking, moving, etc.)
            ActionScheduler actionScheduler = GetComponent<ActionScheduler>();
            if (actionScheduler != null)
            {
                actionScheduler.CancelCurrentAction();
            }

            // Disable collider so player can walk over corpse and click through it
            Collider bodyCollider = GetComponent<Collider>();
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            // TODO: Award XP/loot
            // TODO: Destroy corpse after time or make persistent
        }

        /// <summary>
        /// Check if this character is dead.
        /// </summary>
        public bool IsDead()
        {
            return isDead;
        }

        /// <summary>
        /// Get current health value.
        /// </summary>
        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        /// <summary>
        /// Get maximum health value.
        /// </summary>
        public float GetMaxHealth()
        {
            return maxHealth;
        }

        /// <summary>
        /// Get health as a percentage (0-1).
        /// </summary>
        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }
    }
}
