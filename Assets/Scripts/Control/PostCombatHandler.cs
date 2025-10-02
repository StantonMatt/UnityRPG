using UnityEngine;
using UnityEngine.AI;
using RPG.Movement;
using RPG.Combat;

namespace RPG.Control
{
    public enum PostCombatState
    {
        None,
        Decelerating,
        Dwelling,
        Returning
    }

    public enum PatrolReturnBehavior
    {
        AggroPosition,  // Return to exact position where aggro was lost
        ClosestPoint    // Return to closest waypoint on patrol path
    }

    public enum DecelerationDirection
    {
        CurrentDirection, // Continue in direction enemy was moving
        TowardPlayer      // Decelerate while moving toward player
    }

    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(Fighter))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class PostCombatHandler : MonoBehaviour
    {
        // Dependencies
        private AIController aiController;
        private PatrolController patrolController;
        private Mover mover;
        private Fighter fighter;
        private NavMeshAgent navMeshAgent;

        // State
        private PostCombatState state = PostCombatState.None;
        private float postCombatTimer = 0f;

        // Saved data for post-combat behavior
        private Vector3 aggroPosition;
        private int savedWaypointIndex;
        private GameObject player;
        private Vector3 decelerationMoveDirection;

        // Public properties
        public bool IsInPostCombat => state != PostCombatState.None;
        public PostCombatState CurrentState => state;

        private void Awake()
        {
            aiController = GetComponent<AIController>();
            patrolController = GetComponent<PatrolController>();
            mover = GetComponent<Mover>();
            fighter = GetComponent<Fighter>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public void StartPostCombat(Vector3 aggroPos, int waypointIndex, GameObject playerObject = null)
        {
            state = PostCombatState.Decelerating;
            postCombatTimer = 0f;
            aggroPosition = aggroPos;
            savedWaypointIndex = waypointIndex;
            player = playerObject;

            // Save the current movement direction for deceleration
            if (navMeshAgent != null && navMeshAgent.velocity.sqrMagnitude > 0.01f)
            {
                decelerationMoveDirection = navMeshAgent.velocity.normalized;
            }
            else
            {
                decelerationMoveDirection = transform.forward;
            }

            // Cancel any active combat
            fighter.Cancel();
        }

        public void CancelPostCombat()
        {
            state = PostCombatState.None;
            postCombatTimer = 0f;
        }

        public void UpdatePostCombat()
        {
            if (state == PostCombatState.None) return;

            postCombatTimer += Time.deltaTime;

            switch (state)
            {
                case PostCombatState.Decelerating:
                    HandleDeceleration();
                    break;

                case PostCombatState.Dwelling:
                    HandleDwelling();
                    break;

                case PostCombatState.Returning:
                    HandleReturning();
                    break;
            }
        }

        private void HandleDeceleration()
        {
            if (postCombatTimer < aiController.DecelerationTime)
            {
                // Gradually reduce speed to zero
                float progress = postCombatTimer / aiController.DecelerationTime;
                float currentSpeed = Mathf.Lerp(aiController.ChaseSpeed, 0f, progress);

                if (navMeshAgent != null)
                {
                    Vector3 moveDirection;

                    if (aiController.DecelDirection == DecelerationDirection.TowardPlayer && player != null)
                    {
                        // Face the player and move toward them while decelerating
                        Vector3 lookDirection = player.transform.position - transform.position;
                        lookDirection.y = 0;

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
                        // Continue in saved direction
                        moveDirection = decelerationMoveDirection;
                    }

                    navMeshAgent.velocity = moveDirection * currentSpeed;
                }
            }
            else
            {
                // Deceleration complete - save position and transition to dwelling
                if (aiController.ReturnBehavior == PatrolReturnBehavior.AggroPosition)
                {
                    aggroPosition = transform.position;
                }

                if (navMeshAgent != null)
                {
                    navMeshAgent.velocity = Vector3.zero;
                }

                state = PostCombatState.Dwelling;
                postCombatTimer = 0f;
            }
        }

        private void HandleDwelling()
        {
            // Stay still during dwell time
            if (navMeshAgent != null)
            {
                navMeshAgent.velocity = Vector3.zero;
            }

            if (postCombatTimer >= aiController.DwellTime)
            {
                // Dwell complete - transition to returning
                state = PostCombatState.Returning;
                ReturnToPatrol();
            }
        }

        private void HandleReturning()
        {
            // PatrolController handles the actual movement back to patrol
            // This state just ensures we don't interrupt the return
            state = PostCombatState.None;
        }

        private void ReturnToPatrol()
        {
            if (aiController.ReturnBehavior == PatrolReturnBehavior.AggroPosition)
            {
                patrolController.ResumePatrolFromPosition(aggroPosition, savedWaypointIndex);
            }
            else // ClosestPoint
            {
                patrolController.ResumePatrolFromClosestPoint();
            }
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (state == PostCombatState.None) return;

            // Draw deceleration direction
            if (state == PostCombatState.Decelerating)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, decelerationMoveDirection * 2f);
            }

            // Draw saved aggro position
            if (state != PostCombatState.None && aiController.ReturnBehavior == PatrolReturnBehavior.AggroPosition)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(aggroPosition, 0.5f);
                Gizmos.DrawLine(transform.position, aggroPosition);
            }
        }
    }
}
