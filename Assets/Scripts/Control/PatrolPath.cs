using UnityEngine;

namespace RPG.Control
{
    /// <summary>
    /// Defines a patrol path with multiple waypoints.
    /// Place this component on an empty GameObject with child transforms as waypoints.
    /// </summary>
    public class PatrolPath : MonoBehaviour
    {
        [Tooltip("Radius around waypoint to consider 'reached'")]
        [SerializeField] private float waypointTolerance = 1f;

        [Tooltip("How long to wait at each waypoint before moving to next")]
        [SerializeField] private float waypointDwellTime = 2f;

        /// <summary>
        /// Get the position of a waypoint by index.
        /// </summary>
        public Vector3 GetWaypoint(int index)
        {
            return transform.GetChild(index).position;
        }

        /// <summary>
        /// Get the next waypoint index (wraps around to start).
        /// </summary>
        public int GetNextIndex(int currentIndex)
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex >= transform.childCount)
            {
                nextIndex = 0; // Loop back to start
            }
            return nextIndex;
        }

        /// <summary>
        /// Total number of waypoints in this path.
        /// </summary>
        public int GetWaypointCount()
        {
            return transform.childCount;
        }

        /// <summary>
        /// How close the AI needs to be to consider waypoint reached.
        /// </summary>
        public float GetWaypointTolerance()
        {
            return waypointTolerance;
        }

        /// <summary>
        /// How long to wait at each waypoint.
        /// </summary>
        public float GetWaypointDwellTime()
        {
            return waypointDwellTime;
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Draw the patrol path in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;

            // Draw spheres at each waypoint
            for (int i = 0; i < transform.childCount; i++)
            {
                Vector3 waypointPos = GetWaypoint(i);
                Gizmos.DrawSphere(waypointPos, 0.3f);

                // Draw line to next waypoint
                int nextIndex = GetNextIndex(i);
                Vector3 nextWaypointPos = GetWaypoint(nextIndex);
                Gizmos.DrawLine(waypointPos, nextWaypointPos);
            }
        }
        #endif
    }
}
