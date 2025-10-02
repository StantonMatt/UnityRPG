using UnityEngine;
using UnityEngine.AI;
using RPG.Movement;

namespace RPG.Control
{
    [RequireComponent(typeof(Mover))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class PatrolController : MonoBehaviour
    {
        // Dependencies
        private AIController aiController;
        private Mover mover;
        private NavMeshAgent navMeshAgent;

        // Patrol state
        private PatrolPath patrolPath;
        private int currentWaypointIndex = 0;
        private float timeSinceArrivedAtWaypoint = Mathf.Infinity;
        private bool isPaused = false;
        private bool movingForward = true; // For linear patrol

        // Saved state for returning to patrol
        private Vector3 savedPatrolPosition;
        private int savedWaypointIndex;

        // Public properties
        public bool IsPatrolling => !isPaused && patrolPath != null;
        public int CurrentWaypointIndex => currentWaypointIndex;
        public Vector3 CurrentPosition => transform.position;

        private void Awake()
        {
            aiController = GetComponent<AIController>();
            mover = GetComponent<Mover>();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(PatrolPath path)
        {
            patrolPath = path;
            currentWaypointIndex = 0;
            timeSinceArrivedAtWaypoint = Mathf.Infinity;
            movingForward = true;
        }

        public void UpdatePatrol()
        {
            if (isPaused || patrolPath == null) return;

            if (AtWaypoint())
            {
                timeSinceArrivedAtWaypoint += Time.deltaTime;
                if (timeSinceArrivedAtWaypoint >= aiController.WaypointDwellTime)
                {
                    CycleWaypoint();
                }
            }
            else
            {
                MoveToCurrentWaypoint();
            }
        }

        public void StartPatrol()
        {
            if (patrolPath == null) return;
            isPaused = false;
        }

        public void PausePatrol()
        {
            isPaused = true;
            savedPatrolPosition = transform.position;
            savedWaypointIndex = currentWaypointIndex;
        }

        public void ResumePatrolFromClosestPoint()
        {
            if (patrolPath == null) return;

            // Find closest waypoint
            int closestWaypoint = GetClosestWaypointIndex();
            currentWaypointIndex = closestWaypoint;
            timeSinceArrivedAtWaypoint = 0f;

            isPaused = false;
        }

        public void ResumePatrolFromPosition(Vector3 position, int waypointIndex)
        {
            if (patrolPath == null) return;

            // Move back to saved position first
            mover.MoveTo(position, registerAction: false);
            currentWaypointIndex = waypointIndex;
            timeSinceArrivedAtWaypoint = 0f;

            isPaused = false;
        }

        public Vector3 GetSavedPatrolPosition()
        {
            return savedPatrolPosition;
        }

        public int GetSavedWaypointIndex()
        {
            return savedWaypointIndex;
        }

        private void MoveToCurrentWaypoint()
        {
            Vector3 destination = GetCurrentWaypoint();
            mover.MoveTo(destination, registerAction: false);
        }

        private Vector3 GetCurrentWaypoint()
        {
            if (patrolPath == null) return transform.position;
            return patrolPath.GetWaypoint(currentWaypointIndex);
        }

        private void CycleWaypoint()
        {
            currentWaypointIndex = GetNextWaypointIndex();
            timeSinceArrivedAtWaypoint = 0f;
        }

        private int GetNextWaypointIndex()
        {
            if (patrolPath == null) return 0;

            if (patrolPath.Type == PatrolType.Circular)
            {
                return patrolPath.GetNextIndex(currentWaypointIndex);
            }
            else // Linear
            {
                int waypointCount = patrolPath.GetWaypointCount();

                // Reverse direction at ends
                if (currentWaypointIndex >= waypointCount - 1)
                {
                    movingForward = false;
                }
                else if (currentWaypointIndex <= 0)
                {
                    movingForward = true;
                }

                return movingForward ? currentWaypointIndex + 1 : currentWaypointIndex - 1;
            }
        }

        private bool AtWaypoint()
        {
            if (patrolPath == null) return false;

            float distance = Vector3.Distance(transform.position, GetCurrentWaypoint());
            return distance < aiController.WaypointTolerance;
        }

        private int GetClosestWaypointIndex()
        {
            if (patrolPath == null) return 0;

            int closestIndex = 0;
            float closestDistance = Mathf.Infinity;

            for (int i = 0; i < patrolPath.GetWaypointCount(); i++)
            {
                float distance = Vector3.Distance(transform.position, patrolPath.GetWaypoint(i));
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        // Debug visualization
        private void OnDrawGizmosSelected()
        {
            if (patrolPath == null) return;
            if (!IsPatrolling) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetCurrentWaypoint(), aiController.WaypointTolerance);
        }
    }
}
