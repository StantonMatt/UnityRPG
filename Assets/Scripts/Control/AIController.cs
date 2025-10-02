using UnityEngine;
using RPG.Combat;
using RPG.Movement;
using RPG.Core;
using RPG.Stats;

namespace RPG.Control
{
    /// <summary>
    /// Controls AI behavior for enemies.
    /// Configurable per-enemy through Inspector.
    /// Delegates patrol and post-combat to specialized components.
    /// </summary>
    [RequireComponent(typeof(Fighter))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(ActionScheduler))]
    [RequireComponent(typeof(PatrolController))]
    [RequireComponent(typeof(PostCombatHandler))]
    public class AIController : MonoBehaviour
    {
        [Header("Behavior Configuration")]
        [Tooltip("How this enemy reacts to the player")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Passive;

        [Header("Detection Settings")]
        [Tooltip("How close the player must be to trigger aggro (for aggressive enemies)")]
        [SerializeField] private float aggroRange = 5f;

        [Tooltip("How far the enemy will chase before giving up")]
        [SerializeField] private float maxChaseDistance = 10f;

        [Header("Patrol Settings")]
        [Tooltip("Patrol path this enemy follows when not in combat (optional)")]
        [SerializeField] private PatrolPath patrolPath = null;

        [Tooltip("How close to waypoint before considering it 'reached'")]
        [SerializeField] private float waypointTolerance = 1f;

        [Tooltip("How long to wait at each waypoint before moving to next")]
        [SerializeField] private float waypointDwellTime = 2f;

        [Tooltip("How enemy returns to patrol after combat")]
        [SerializeField] private PatrolReturnBehavior returnBehavior = PatrolReturnBehavior.ClosestPoint;

        [Header("Post-Combat Behavior")]
        [Tooltip("Time taken to decelerate from chase speed to stop when losing aggro")]
        [SerializeField] private float decelerationTime = 0.5f;

        [Tooltip("Time to stand still after stopping before returning to patrol")]
        [SerializeField] private float dwellTime = 2f;

        [Tooltip("Direction enemy moves while decelerating")]
        [SerializeField] private DecelerationDirection decelerationDirection = DecelerationDirection.CurrentDirection;

        [Header("Speed Settings")]
        [Tooltip("Movement speed when patrolling or idle")]
        [SerializeField] private float patrolSpeed = 2f;

        [Tooltip("Movement speed when chasing or in combat")]
        [SerializeField] private float chaseSpeed = 5f;

        // Components
        private Fighter fighter;
        private Health health;
        private Mover mover;
        private UnityEngine.AI.NavMeshAgent navMeshAgent;
        private PatrolController patrolController;
        private PostCombatHandler postCombatHandler;

        // State
        private GameObject player;
        private Vector3 guardPosition;
        private bool hadTargetLastFrame = false;

        // Public properties for components to access settings
        public float WaypointTolerance => waypointTolerance;
        public float WaypointDwellTime => waypointDwellTime;
        public float DecelerationTime => decelerationTime;
        public float DwellTime => dwellTime;
        public float ChaseSpeed => chaseSpeed;
        public float PatrolSpeed => patrolSpeed;
        public PatrolReturnBehavior ReturnBehavior => returnBehavior;
        public DecelerationDirection DecelDirection => decelerationDirection;

        private void Start()
        {
            fighter = GetComponent<Fighter>();
            health = GetComponent<Health>();
            mover = GetComponent<Mover>();
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            patrolController = GetComponent<PatrolController>();
            postCombatHandler = GetComponent<PostCombatHandler>();
            player = GameObject.FindGameObjectWithTag("Player");
            guardPosition = transform.position;

            // Safety check: make sure we didn't find ourselves as the player
            if (player == gameObject)
            {
                GameDebug.LogError($"AIController on {gameObject.name} found itself as Player! Check your tags - this enemy should NOT have the 'Player' tag.", this);
                player = null;
            }

            // Initialize patrol
            if (patrolPath != null)
            {
                patrolController.Initialize(patrolPath);
            }
        }

        private void Update()
        {
            // Don't do anything if dead
            if (health.IsDead()) return;

            // Update post-combat handler (runs its state machine)
            postCombatHandler.UpdatePostCombat();

            // Behavior based on type
            switch (behaviorType)
            {
                case AIBehaviorType.Aggressive:
                    AggressiveBehavior();
                    break;
                case AIBehaviorType.Passive:
                    PassiveBehavior();
                    break;
                case AIBehaviorType.Coward:
                    CowardBehavior();
                    break;
                case AIBehaviorType.Neutral:
                    NeutralBehavior();
                    break;
            }
        }

        /// <summary>
        /// Aggressive: Attacks player on sight within aggro range.
        /// </summary>
        private void AggressiveBehavior()
        {
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool inAggroRange = distanceToPlayer <= aggroRange;

            // State: Player in aggro range - attack (even if in post-combat)
            if (inAggroRange)
            {
                // Cancel post-combat if we were in it
                if (postCombatHandler.IsInPostCombat)
                {
                    postCombatHandler.CancelPostCombat();
                }

                SetSpeed(chaseSpeed);
                patrolController.PausePatrol();
                fighter.Attack(player);
            }
            // State: Just lost aggro - start post-combat sequence
            else if (!inAggroRange && !postCombatHandler.IsInPostCombat && fighter.HasTarget())
            {
                postCombatHandler.StartPostCombat(
                    transform.position,
                    patrolController.CurrentWaypointIndex,
                    player
                );
            }
            // State: Normal patrol (no combat, not in post-combat)
            else if (!inAggroRange && !postCombatHandler.IsInPostCombat)
            {
                SetSpeed(patrolSpeed);
                patrolController.UpdatePatrol();
            }
        }

        /// <summary>
        /// Passive: Only attacks when attacked (handled by Health component).
        /// Patrols or returns to guard position when not in combat.
        /// </summary>
        private void PassiveBehavior()
        {
            bool hasTarget = fighter.HasTarget();

            // State: In Combat (re-engage even if in post-combat)
            if (hasTarget)
            {
                // Cancel post-combat if we were in it
                if (postCombatHandler.IsInPostCombat)
                {
                    postCombatHandler.CancelPostCombat();
                }

                SetSpeed(chaseSpeed);
                patrolController.PausePatrol();
                hadTargetLastFrame = true;
            }
            // State: Just lost target - start post-combat sequence
            else if (!hasTarget && !postCombatHandler.IsInPostCombat && hadTargetLastFrame)
            {
                postCombatHandler.StartPostCombat(
                    transform.position,
                    patrolController.CurrentWaypointIndex,
                    null // Passive enemies don't track player
                );
                hadTargetLastFrame = false;
            }
            // State: Normal patrol (no combat, not in post-combat)
            else if (!hasTarget && !postCombatHandler.IsInPostCombat)
            {
                SetSpeed(patrolSpeed);
                patrolController.UpdatePatrol();
            }
        }

        /// <summary>
        /// Coward: Flees when attacked (future feature).
        /// </summary>
        private void CowardBehavior()
        {
            // TODO: Implement flee logic
            // For now, behave like passive
            PassiveBehavior();
        }

        /// <summary>
        /// Neutral: Never attacks, never retaliates.
        /// Still patrols if patrol path is set.
        /// </summary>
        private void NeutralBehavior()
        {
            SetSpeed(patrolSpeed);
            patrolController.UpdatePatrol();
        }

        /// <summary>
        /// Set the NavMeshAgent speed.
        /// </summary>
        private void SetSpeed(float speed)
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.speed = speed;
            }
        }

        /// <summary>
        /// Check if this enemy should auto-aggro when attacked.
        /// Called by Health component.
        /// </summary>
        public bool ShouldRetaliateWhenAttacked()
        {
            return behaviorType switch
            {
                AIBehaviorType.Aggressive => true,
                AIBehaviorType.Passive => true,
                AIBehaviorType.Coward => false, // Cowards flee, don't fight back
                AIBehaviorType.Neutral => false,
                _ => false
            };
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Visualize aggro range in editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Show aggro range for aggressive enemies
            if (behaviorType == AIBehaviorType.Aggressive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, aggroRange);
            }

            // Show max chase distance
            Gizmos.color = Color.yellow;
            Vector3 guardPos = Application.isPlaying ? guardPosition : transform.position;
            Gizmos.DrawWireSphere(guardPos, maxChaseDistance);
        }
        #endif
    }

    /// <summary>
    /// Types of AI behavior.
    /// </summary>
    public enum AIBehaviorType
    {
        Aggressive,  // Attacks player on sight
        Passive,     // Only attacks when attacked
        Coward,      // Flees when attacked (future)
        Neutral      // Never attacks
    }
}
