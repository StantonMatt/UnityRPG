using UnityEngine;
using UnityEngine.AI;
using RPG.Core;

namespace RPG.Movement
{
    /// <summary>
    /// Handles NavMesh-based movement execution for any character (player or AI).
    /// This component only knows HOW to move - it doesn't care WHO is telling it to move.
    /// Can be controlled by PlayerController (for players) or AIController (for enemies).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ActionScheduler))]
    public class Mover : MonoBehaviour, IAction
    {
    public enum MovementStyle
    {
        Snappy,     // Instant acceleration, stops, and turns
        Smooth      // Natural movement using NavMeshAgent physics
    }

    [Header("Movement Settings")]
    [Tooltip("Snappy: Instant acceleration/stops/turns\nSmooth: Natural movement using NavMeshAgent settings")]
    [SerializeField] private MovementStyle movementStyle = MovementStyle.Snappy;

    [Header("Path Settings")]
    [Range(10f, 100f)]
    [Tooltip("Maximum path distance the character will navigate. Prevents extremely long paths around obstacles.")]
    [SerializeField] private float maxPathDistance = 30f;

    [Range(1f, 10f)]
    [Tooltip("Maximum radius to search for NavMesh when target is outside valid areas.")]
    [SerializeField] private float maxSearchRadius = 5f;

    [Header("Movement Behavior")]
    [SerializeField] private bool instantReversalsForPathfinding = true; // Zero velocity on direction reversal

    // Component references
    private NavMeshAgent agent;
    private Animator animator;
    private ActionScheduler actionScheduler;

    // Movement state
    private enum MovementMode { None, Pathfinding, DirectMovement }
    private MovementMode currentMode = MovementMode.None;
    private Vector3 lastPosition;
    private float manualVelocity = 0f;

    // For smooth direct movement
    private float currentSpeed = 0f;
    private Vector3 currentMoveDirection = Vector3.zero;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        actionScheduler = GetComponent<ActionScheduler>();
        lastPosition = transform.position;

        if (agent == null)
        {
            GameDebug.LogError("NavMeshAgent component is missing!", this);
        }

        ApplyMovementStyle();
    }

    private void Update()
    {
        UpdateAnimator();
    }

    /// <summary>
    /// Move to a specific world position using pathfinding.
    /// Used for point-and-click movement or AI patrol points.
    /// </summary>
    /// <param name="destination">World position to move to</param>
    /// <param name="registerAction">Whether to register this as the current action (default true)</param>
    public void MoveTo(Vector3 destination, bool registerAction = true)
    {
        if (agent == null || !agent.enabled) return;

        if (registerAction)
        {
            actionScheduler.StartAction(this); // Register this action with the scheduler
        }

        // Find nearest valid point on NavMesh
        NavMeshHit navHit;
        Vector3 targetPosition = destination;

        if (NavMesh.SamplePosition(destination, out navHit, maxSearchRadius, NavMesh.AllAreas))
        {
            targetPosition = navHit.position;
        }
        else
        {
            // No valid NavMesh position found
            return;
        }

        // Calculate path
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(targetPosition, path);

        if (path.status == NavMeshPathStatus.PathInvalid)
        {
            return;
        }

        // Check path distance
        float pathDistance = GetPathDistance(path);
        if (pathDistance > maxPathDistance)
        {
            // Path is too long, don't move
            return;
        }

        // Switch to pathfinding mode
        if (currentMode != MovementMode.Pathfinding)
        {
            SwitchToPathfindingMode();
        }

        // Set the path
        agent.SetPath(path);

        // Handle rotation for snappy mode
        if (movementStyle == MovementStyle.Snappy)
        {
            // Get direction to first corner
            Vector3 targetDir = path.corners.Length > 1 ? path.corners[1] - transform.position :
                               path.corners.Length > 0 ? path.corners[0] - transform.position :
                               targetPosition - transform.position;

            targetDir.y = 0f;
            if (targetDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(targetDir.normalized);
            }

            // Handle instant reversals
            if (instantReversalsForPathfinding && agent.velocity.magnitude > 0.1f)
            {
                Vector3 velocityDir = agent.velocity.normalized;
                velocityDir.y = 0f;
                float dot = Vector3.Dot(velocityDir, targetDir.normalized);
                if (dot < 0f) // Opposite directions
                {
                    agent.velocity = Vector3.zero;
                }
            }
        }
    }

    /// <summary>
    /// Move in a specific direction relative to world space.
    /// Used for WASD/Controller input or AI steering behaviors.
    /// </summary>
    public void MoveInDirection(Vector3 direction)
    {
        if (agent == null || !agent.enabled) return;
        if (direction == Vector3.zero) return;

        actionScheduler.StartAction(this); // Register this action with the scheduler

        // Switch to direct movement mode
        if (currentMode != MovementMode.DirectMovement)
        {
            SwitchToDirectMovementMode();
        }

        if (movementStyle == MovementStyle.Snappy)
        {
            // Instant movement and rotation
            Vector3 moveDelta = direction * agent.speed * Time.deltaTime;
            agent.Move(moveDelta);

            // Instant rotation
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else // Smooth
        {
            // Smooth acceleration
            float targetSpeed = agent.speed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, agent.acceleration * Time.deltaTime);
            currentMoveDirection = direction;

            // Apply movement
            Vector3 moveDelta = currentMoveDirection * currentSpeed * Time.deltaTime;
            agent.Move(moveDelta);

            // Smooth rotation
            if (agent.angularSpeed > 1000f)
            {
                // Very high angular speed = instant rotation
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    agent.angularSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Stop all movement immediately.
    /// </summary>
    public void Cancel()
    {
        if (agent == null) return;

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        currentSpeed = 0f;
        currentMode = MovementMode.None;
    }

    /// <summary>
    /// Check if the agent has reached its destination.
    /// </summary>
    public bool HasReachedDestination()
    {
        if (agent == null || currentMode != MovementMode.Pathfinding) return true;

        return !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance;
    }

    /// <summary>
    /// Get the current movement style.
    /// </summary>
    public MovementStyle GetMovementStyle()
    {
        return movementStyle;
    }

    /// <summary>
    /// Set the movement style at runtime.
    /// </summary>
    public void SetMovementStyle(MovementStyle style)
    {
        movementStyle = style;
        ApplyMovementStyle();
    }

    /// <summary>
    /// Toggle between movement styles.
    /// </summary>
    public void ToggleMovementStyle()
    {
        movementStyle = movementStyle == MovementStyle.Snappy ? MovementStyle.Smooth : MovementStyle.Snappy;
        ApplyMovementStyle();
    }

    private void SwitchToPathfindingMode()
    {
        currentMode = MovementMode.Pathfinding;
        agent.updatePosition = true;
        agent.updateRotation = (movementStyle == MovementStyle.Smooth);
        agent.isStopped = false;
    }

    private void SwitchToDirectMovementMode()
    {
        currentMode = MovementMode.DirectMovement;
        agent.ResetPath();
        agent.updatePosition = true;
        agent.updateRotation = false; // We handle rotation manually for direct movement
        agent.isStopped = false;
    }

    private void ApplyMovementStyle()
    {
        if (agent == null) return;

        if (movementStyle == MovementStyle.Snappy)
        {
            // For snappy mode, we handle rotation manually
            agent.updateRotation = false;
        }
        else if (currentMode == MovementMode.Pathfinding)
        {
            // For smooth pathfinding, let agent handle rotation
            agent.updateRotation = true;
        }
    }

    private float GetPathDistance(NavMeshPath path)
    {
        if (path.corners.Length < 2) return 0f;

        float distance = 0f;
        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }
        return distance;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Calculate velocity manually based on position changes
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - lastPosition;
        deltaPosition.y = 0;
        float deltaDistance = deltaPosition.magnitude;
        manualVelocity = deltaDistance / Time.deltaTime;
        lastPosition = currentPosition;

        // Use agent velocity if available, otherwise use manual calculation
        float velocityToUse = agent.velocity.magnitude;
        if (velocityToUse < 0.1f && manualVelocity > 0.1f)
        {
            velocityToUse = manualVelocity;
        }

        // Set animator parameter (blend trees typically use 0=idle, ~2=walk, ~5.5=run)
        animator.SetFloat("forwardSpeed", velocityToUse);
    }
    }
}