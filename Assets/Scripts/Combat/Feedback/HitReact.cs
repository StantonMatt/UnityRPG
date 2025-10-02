using UnityEngine;
using RPG.Stats;
using RPG.Core;

namespace RPG.Combat.Feedback
{
    /// <summary>
    /// Triggers hit reaction animation when damaged.
    /// Listens to CombatEvents and triggers animator parameter.
    /// Add this component to any character that should play a flinch animation when hit.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitReact : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Animator component (if not assigned, will search this GameObject)")]
        [SerializeField] private Animator animator;

        [Tooltip("Name of the trigger parameter in the Animator")]
        [SerializeField] private string hitReactTrigger = "hitReact";

        [Tooltip("Minimum time between hit reactions (prevents spam)")]
        [SerializeField] private float minTimeBetweenReactions = 0.3f;

        private Health health;
        private float lastReactionTime = -999f;

        private void Awake()
        {
            health = GetComponent<Health>();

            // Find animator if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                GameDebug.LogWarning($"[HitReact] {gameObject.name} has no Animator! Hit react will not work.",
                    config => config.logHitReact, this);
            }
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageTaken += HandleDamageTaken;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageTaken -= HandleDamageTaken;
        }

        private void HandleDamageTaken(CombatEvents.DamageTakenEvent e)
        {
            // Only react if WE are the target
            if (e.Target != gameObject) return;

            // Don't react if dead
            if (health != null && health.IsDead())
            {
                GameDebug.Log($"[HitReact] {gameObject.name} is dead, skipping hit react",
                    config => config.logHitReact, this);
                return;
            }

            // Don't react if we just reacted recently (prevent animation spam)
            if (Time.time - lastReactionTime < minTimeBetweenReactions)
            {
                GameDebug.Log($"[HitReact] {gameObject.name} hit react on cooldown",
                    config => config.logHitReact, this);
                return;
            }

            // Trigger hit react animation
            if (animator != null)
            {
                animator.SetTrigger(hitReactTrigger);
                lastReactionTime = Time.time;

                GameDebug.Log($"[HitReact] {gameObject.name} triggered '{hitReactTrigger}'",
                    config => config.logHitReact, this);
            }
        }
    }
}
