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
    /// </summary>
    [RequireComponent(typeof(Fighter))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(ActionScheduler))]
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

        private Fighter fighter;
        private Health health;
        private Mover mover;
        private UnityEngine.AI.NavMeshAgent navMeshAgent;
        private GameObject player;
        private Vector3 guardPosition;
        private Vector3 aggroPosition;
        private int aggroWaypointIndex;
        private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private float postCombatTimer = 0f;
        private int currentWaypointIndex = 0;
        private bool hadTargetLastFrame = false;
        private Vector3 decelerationMoveDirection; // Direction to move during deceleration

        private enum PostCombatState
        {
            None,           // Not in post-combat
            Decelerating,   // Slowing down
            Dwelling,       // Standing still
            Returning       // Moving back to patrol
        }
        private PostCombatState postCombatState = PostCombatState.None;

        private void Start()
        {
            fighter = GetComponent<Fighter>();
            health = GetComponent<Health>();
            mover = GetComponent<Mover>();
            navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            player = GameObject.FindGameObjectWithTag("Player");
            guardPosition = transform.position;
        }

        private void Update()
        {
            // Don't do anything if dead
            if (health.IsDead()) return;

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

            // State: In Combat
            if (inAggroRange && postCombatState == PostCombatState.None)
            {
                SetSpeed(chaseSpeed);
                fighter.Attack(player);
            }
            // State: Just lost aggro - start post-combat sequence
            else if (!inAggroRange && postCombatState == PostCombatState.None && fighter.HasTarget())
            {
                // Save position where we lost aggro
                aggroPosition = transform.position;
                aggroWaypointIndex = currentWaypointIndex;

                // Save the direction we should move during deceleration
                if (decelerationDirection == DecelerationDirection.TowardPlayer && player != null)
                {
                    // Direction toward player at the moment aggro is lost
                    decelerationMoveDirection = (player.transform.position - transform.position).normalized;
                    decelerationMoveDirection.y = 0; // Keep on horizontal plane
                }
                else
                {
                    // Direction we're currently facing
                    decelerationMoveDirection = transform.forward;
                }

                // Start deceleration
                postCombatState = PostCombatState.Decelerating;
                postCombatTimer = 0f;
                fighter.Cancel(); // Stop attacking
            }
            // State: Decelerating
            else if (postCombatState == PostCombatState.Decelerating)
            {
                // Player came back into range - resume combat
                if (inAggroRange)
                {
                    postCombatState = PostCombatState.None;
                    SetSpeed(chaseSpeed);
                    fighter.Attack(player);
                    return;
                }

                postCombatTimer += Time.deltaTime;

                if (postCombatTimer < decelerationTime)
                {
                    // Decelerate smoothly
                    float progress = postCombatTimer / decelerationTime;
                    float currentSpeed = Mathf.Lerp(chaseSpeed, 0f, progress);

                    if (navMeshAgent != null)
                    {
                        Vector3 moveDirection;

                        // Determine movement direction based on setting
                        if (decelerationDirection == DecelerationDirection.TowardPlayer && player != null)
                        {
                            // Face the player and move toward them
                            Vector3 lookDirection = player.transform.position - transform.position;
                            lookDirection.y = 0; // Keep on horizontal plane

                            if (lookDirection != Vector3.zero)
                            {
                                transform.rotation = Quaternion.LookRotation(lookDirection);
                                moveDirection = lookDirection.normalized;
                            }
                            else
                            {
                                moveDirection = decelerationMoveDirection;
                            }
                        }
                        else
                        {
                            // Use saved direction (momentum)
                            moveDirection = decelerationMoveDirection;
                        }

                        // Move with decreasing speed
                        navMeshAgent.velocity = moveDirection * currentSpeed;
                    }

                    #if UNITY_EDITOR
                    Debug.Log($"Decelerating: {postCombatTimer:F2}s / {decelerationTime:F2}s, Speed: {currentSpeed:F2}");
                    #endif
                }
                else
                {
                    // Deceleration complete - start dwelling
                    // Save the CURRENT position where we stopped, not where we started decelerating
                    aggroPosition = transform.position;

                    postCombatState = PostCombatState.Dwelling;
                    postCombatTimer = 0f;

                    if (navMeshAgent != null)
                    {
                        navMeshAgent.isStopped = true;
                        navMeshAgent.velocity = Vector3.zero;
                        navMeshAgent.ResetPath();
                    }

                    #if UNITY_EDITOR
                    Debug.Log("Deceleration complete, starting dwell");
                    #endif
                }
            }
            // State: Dwelling
            else if (postCombatState == PostCombatState.Dwelling)
            {
                // Player came back into range - resume combat
                if (inAggroRange)
                {
                    postCombatState = PostCombatState.None;
                    if (navMeshAgent != null) navMeshAgent.isStopped = false;
                    SetSpeed(chaseSpeed);
                    fighter.Attack(player);
                    return;
                }

                postCombatTimer += Time.deltaTime;

                #if UNITY_EDITOR
                Debug.Log($"Dwelling: {postCombatTimer:F2}s / {dwellTime:F2}s");
                #endif

                if (postCombatTimer >= dwellTime)
                {
                    // Dwell complete - start returning
                    postCombatState = PostCombatState.Returning;
                    if (navMeshAgent != null) navMeshAgent.isStopped = false;

                    #if UNITY_EDITOR
                    Debug.Log("Dwell complete, starting return to patrol");
                    #endif
                }
            }
            // State: Returning to patrol
            else if (postCombatState == PostCombatState.Returning)
            {
                // Player came back into range - resume combat
                if (inAggroRange)
                {
                    postCombatState = PostCombatState.None;
                    SetSpeed(chaseSpeed);
                    fighter.Attack(player);
                    return;
                }

                ReturnToAggroPosition();
            }
            // State: Normal patrol (no combat, not returning)
            else
            {
                SetSpeed(patrolSpeed);
                PatrolBehavior();
            }
        }

        /// <summary>
        /// Passive: Only attacks when attacked (handled by Health component).
        /// Patrols or returns to guard position when not in combat.
        /// </summary>
        private void PassiveBehavior()
        {
            bool hasTarget = fighter.HasTarget();

            // State: In Combat
            if (hasTarget && postCombatState == PostCombatState.None)
            {
                SetSpeed(chaseSpeed);
                hadTargetLastFrame = true;
            }
            // State: Just lost target - start post-combat sequence
            else if (!hasTarget && postCombatState == PostCombatState.None && hadTargetLastFrame)
            {
                // Save position where combat ended
                aggroPosition = transform.position;
                aggroWaypointIndex = currentWaypointIndex;

                // Start deceleration
                postCombatState = PostCombatState.Decelerating;
                postCombatTimer = 0f;
                hadTargetLastFrame = false;
            }
            // State: Never been in combat - just patrol
            else if (!hasTarget && postCombatState == PostCombatState.None && !hadTargetLastFrame)
            {
                SetSpeed(patrolSpeed);
                PatrolBehavior();
            }
            // State: Decelerating
            else if (postCombatState == PostCombatState.Decelerating)
            {
                // Got new target - resume combat
                if (hasTarget)
                {
                    postCombatState = PostCombatState.None;
                    SetSpeed(chaseSpeed);
                    hadTargetLastFrame = true;
                    return;
                }

                postCombatTimer += Time.deltaTime;

                if (postCombatTimer < decelerationTime)
                {
                    // Decelerate smoothly
                    float progress = postCombatTimer / decelerationTime;
                    float currentSpeed = Mathf.Lerp(chaseSpeed, 0f, progress);

                    if (navMeshAgent != null)
                    {
                        navMeshAgent.speed = currentSpeed;
                        // Keep the agent moving by maintaining destination
                        if (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.5f)
                        {
                            Vector3 targetDestination;

                            // Choose destination based on deceleration direction setting
                            switch (decelerationDirection)
                            {
                                case DecelerationDirection.CurrentDirection:
                                    // Continue in whatever direction enemy is facing
                                    targetDestination = transform.position + transform.forward * 10f;
                                    break;
                                case DecelerationDirection.TowardPlayer:
                                    // Passive enemies don't have a player reference
                                    // So just continue in current direction
                                    targetDestination = transform.position + transform.forward * 10f;
                                    break;
                                default:
                                    targetDestination = transform.position + transform.forward * 10f;
                                    break;
                            }

                            navMeshAgent.SetDestination(targetDestination);
                        }
                    }
                }
                else
                {
                    // Deceleration complete - start dwelling
                    // Save CURRENT position where we stopped
                    aggroPosition = transform.position;

                    postCombatState = PostCombatState.Dwelling;
                    postCombatTimer = 0f;

                    if (navMeshAgent != null)
                    {
                        navMeshAgent.isStopped = true;
                        navMeshAgent.velocity = Vector3.zero;
                        navMeshAgent.ResetPath();
                    }
                }
            }
            // State: Dwelling
            else if (postCombatState == PostCombatState.Dwelling)
            {
                // Got new target - resume combat
                if (hasTarget)
                {
                    postCombatState = PostCombatState.None;
                    if (navMeshAgent != null) navMeshAgent.isStopped = false;
                    SetSpeed(chaseSpeed);
                    hadTargetLastFrame = true;
                    return;
                }

                postCombatTimer += Time.deltaTime;

                if (postCombatTimer >= dwellTime)
                {
                    // Dwell complete - start returning
                    postCombatState = PostCombatState.Returning;
                    if (navMeshAgent != null) navMeshAgent.isStopped = false;
                }
            }
            // State: Returning to patrol
            else if (postCombatState == PostCombatState.Returning)
            {
                // Got new target - resume combat
                if (hasTarget)
                {
                    postCombatState = PostCombatState.None;
                    SetSpeed(chaseSpeed);
                    return;
                }

                ReturnToAggroPosition();
            }
            // State: Normal patrol
            else
            {
                SetSpeed(patrolSpeed);
                PatrolBehavior();
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
            PatrolBehavior();
        }

        /// <summary>
        /// Patrol between waypoints or return to guard position.
        /// </summary>
        private void PatrolBehavior()
        {
            // No patrol path - just guard current position
            if (patrolPath == null || patrolPath.GetWaypointCount() == 0)
            {
                GuardBehavior();
                return;
            }

            // At waypoint - wait for dwell time before moving to next
            if (AtWaypoint())
            {
                timeSinceArrivedAtWaypoint += Time.deltaTime;

                if (timeSinceArrivedAtWaypoint >= patrolPath.GetWaypointDwellTime())
                {
                    timeSinceArrivedAtWaypoint = 0f;
                    currentWaypointIndex = patrolPath.GetNextIndex(currentWaypointIndex);
                }
            }
            // Not at waypoint - move to it
            else
            {
                timeSinceArrivedAtWaypoint = 0f;
                mover.MoveTo(GetCurrentWaypoint());
            }
        }

        /// <summary>
        /// Stay at guard position (no patrol).
        /// </summary>
        private void GuardBehavior()
        {
            if (Vector3.Distance(transform.position, guardPosition) > 1f)
            {
                mover.MoveTo(guardPosition);
            }
        }

        /// <summary>
        /// Return to guard position and reset patrol.
        /// </summary>
        private void ReturnToGuardPosition()
        {
            timeSinceArrivedAtWaypoint = Mathf.Infinity;
            postCombatState = PostCombatState.None;
            mover.MoveTo(guardPosition);
        }

        /// <summary>
        /// Return to the position where aggro started, then resume patrol.
        /// </summary>
        private void ReturnToAggroPosition()
        {
            SetSpeed(patrolSpeed);

            // No patrol path - just return to aggro position
            if (patrolPath == null || patrolPath.GetWaypointCount() == 0)
            {
                if (Vector3.Distance(transform.position, aggroPosition) <= 1f)
                {
                    postCombatState = PostCombatState.None;
                }
                else
                {
                    mover.MoveTo(aggroPosition);
                }
                return;
            }

            // Handle return behavior based on configuration
            switch (returnBehavior)
            {
                case PatrolReturnBehavior.AggroPosition:
                    ReturnToAggroPositionBehavior();
                    break;
                case PatrolReturnBehavior.ClosestPoint:
                    ReturnToClosestPointOnPath();
                    break;
            }
        }

        /// <summary>
        /// Return to exact position where aggro started (original behavior).
        /// </summary>
        private void ReturnToAggroPositionBehavior()
        {
            // Arrived at aggro position - resume patrol from saved waypoint
            if (Vector3.Distance(transform.position, aggroPosition) <= 1f)
            {
                postCombatState = PostCombatState.None;
                currentWaypointIndex = aggroWaypointIndex;
                timeSinceArrivedAtWaypoint = 0f;
                PatrolBehavior();
            }
            // Still moving back to aggro position
            else
            {
                mover.MoveTo(aggroPosition);
            }
        }

        /// <summary>
        /// Return to closest point on patrol path (smoother behavior).
        /// </summary>
        private void ReturnToClosestPointOnPath()
        {
            // Find closest point on entire patrol path
            Vector3 closestPoint = Vector3.zero;
            int closestSegmentWaypoint = 0;
            float closestDistance = Mathf.Infinity;

            // Check each segment of the patrol path
            for (int i = 0; i < patrolPath.GetWaypointCount(); i++)
            {
                Vector3 waypointA = patrolPath.GetWaypoint(i);
                Vector3 waypointB = patrolPath.GetWaypoint(patrolPath.GetNextIndex(i));

                // Find closest point on this line segment
                Vector3 pointOnSegment = GetClosestPointOnLineSegment(waypointA, waypointB, transform.position);
                float distance = Vector3.Distance(transform.position, pointOnSegment);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = pointOnSegment;
                    closestSegmentWaypoint = patrolPath.GetNextIndex(i); // Target the next waypoint
                }
            }

            // Arrived at closest point on path - resume patrol
            if (closestDistance <= 1f)
            {
                postCombatState = PostCombatState.None;
                currentWaypointIndex = closestSegmentWaypoint;
                timeSinceArrivedAtWaypoint = 0f;
                PatrolBehavior();
            }
            // Still moving to closest point
            else
            {
                mover.MoveTo(closestPoint);
            }
        }

        /// <summary>
        /// Get the closest point on a line segment to a given position.
        /// </summary>
        private Vector3 GetClosestPointOnLineSegment(Vector3 lineStart, Vector3 lineEnd, Vector3 position)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();

            Vector3 lineToPosition = position - lineStart;
            float dotProduct = Vector3.Dot(lineToPosition, lineDirection);

            // Clamp to line segment (not infinite line)
            dotProduct = Mathf.Clamp(dotProduct, 0f, lineLength);

            return lineStart + lineDirection * dotProduct;
        }

        /// <summary>
        /// Check if we're at the current waypoint.
        /// </summary>
        private bool AtWaypoint()
        {
            float distanceToWaypoint = Vector3.Distance(transform.position, GetCurrentWaypoint());
            return distanceToWaypoint < patrolPath.GetWaypointTolerance();
        }

        /// <summary>
        /// Get the current waypoint position.
        /// </summary>
        private Vector3 GetCurrentWaypoint()
        {
            return patrolPath.GetWaypoint(currentWaypointIndex);
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

    /// <summary>
    /// How the AI returns to patrol after combat.
    /// </summary>
    public enum PatrolReturnBehavior
    {
        AggroPosition,  // Return to exact position where combat started
        ClosestPoint    // Return to closest point on patrol path (smoother)
    }

    /// <summary>
    /// Direction enemy moves while decelerating after losing aggro.
    /// </summary>
    public enum DecelerationDirection
    {
        CurrentDirection,  // Continue in the direction enemy is facing
        TowardPlayer       // Continue moving toward player (chase momentum)
    }
}
