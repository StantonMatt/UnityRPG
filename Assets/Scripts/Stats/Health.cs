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
        public void TakeDamage(float damage, GameObject instigator = null)
        {
            if (isDead) return; // Already dead, can't take more damage

            currentHealth = Mathf.Max(currentHealth - damage, 0f);

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}", this);
            Debug.Log($"{gameObject.name} was attacked by {(instigator != null ? instigator.name : "UNKNOWN - NO INSTIGATOR!")}", this);
            #endif

            // Respond to being attacked (fight back, flee, etc.)
            if (instigator != null)
            {
                AggravateNearbyEnemies(instigator);
            }

            if (currentHealth <= 0)
            {
                Die();
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
                #if UNITY_EDITOR
                Debug.Log($"{gameObject.name} was attacked but AI behavior prevents retaliation.", this);
                #endif
                return; // Neutral or Coward AI - don't fight back
            }

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} is aggroing! Looking for Fighter component...", this);
            #endif

            // Make this character attack back (AI only)
            Fighter fighter = GetComponent<Fighter>();

            #if UNITY_EDITOR
            if (fighter != null)
            {
                Debug.Log($"{gameObject.name} has Fighter! Attacking {instigator.name}!", this);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} does NOT have Fighter component - cannot fight back!", this);
            }
            #endif

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

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}", this);
            #endif
        }

        /// <summary>
        /// Handle death.
        /// </summary>
        private void Die()
        {
            if (isDead) return; // Already dead

            isDead = true;

            #if UNITY_EDITOR
            Debug.Log($"{gameObject.name} has died!", this);
            #endif

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
