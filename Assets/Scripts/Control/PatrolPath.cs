using UnityEngine;

namespace RPG.Control
{
    public enum PatrolType
    {
        Circular, // Loop back to start (0→1→2→0...)
        Linear    // Reverse at ends (0→1→2→1→0...)
    }

    /// <summary>
    /// Defines a patrol path with multiple waypoints.
    /// Place this component on an empty GameObject with child transforms as waypoints.
    /// </summary>
    public class PatrolPath : MonoBehaviour
    {
        [Tooltip("Circular: loops back to start. Linear: reverses at ends")]
        [SerializeField] private PatrolType patrolType = PatrolType.Circular;

        public PatrolType Type => patrolType;

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


        #if UNITY_EDITOR
        /// <summary>
        /// Draw the patrol path in the editor.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (transform.childCount == 0) return;

            Gizmos.color = Color.cyan;

            // Draw spheres at each waypoint
            for (int i = 0; i < transform.childCount; i++)
            {
                Vector3 waypointPos = GetWaypoint(i);
                Gizmos.DrawSphere(waypointPos, 0.3f);

                // Draw lines based on patrol type
                if (patrolType == PatrolType.Circular)
                {
                    // Draw line to next waypoint (loops back to start)
                    int nextIndex = GetNextIndex(i);
                    Vector3 nextWaypointPos = GetWaypoint(nextIndex);
                    Gizmos.DrawLine(waypointPos, nextWaypointPos);
                }
                else // Linear
                {
                    // Draw line to next waypoint (no line from last back to first)
                    if (i < transform.childCount - 1)
                    {
                        Vector3 nextWaypointPos = GetWaypoint(i + 1);
                        Gizmos.DrawLine(waypointPos, nextWaypointPos);
                    }
                }
            }
        }
        #endif
    }
}
