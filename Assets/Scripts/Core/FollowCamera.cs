using UnityEngine;
using UnityEngine.InputSystem;

namespace RPG.Core
{
    /// <summary>
    /// Ragnarok Online-style camera controller.
    /// Isometric view locked on the player with right-click drag rotation.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
    [Header("Target")]
    [SerializeField] private Transform target; // The player to follow
    [SerializeField] private bool autoFindPlayer = true; // Auto-find player if not set
    
    [Header("Camera Settings")]
    [Range(5f, 30f)]
    [SerializeField] private float distance = 15f; // Default distance from player
    [Range(0f, 3f)]
    [SerializeField] private float lookAtOffset = 1f; // Height offset for look-at point on player
    
    [Header("Zoom Settings")]
    [Range(2f, 10f)]
    [SerializeField] private float minDistance = 5f; // Minimum zoom distance
    [Range(15f, 50f)]
    [SerializeField] private float maxDistance = 30f; // Maximum zoom distance
    [Range(1f, 100f)]
    [SerializeField] private float zoomSpeed = 60f; // Speed of zoom
    [SerializeField] private bool smoothZoom = false; // Enable smooth zoom transitions
    [Range(1f, 15f)]
    [Tooltip("How smooth the zoom feels. Higher = smoother/slower, Lower = snappier/faster")]
    [SerializeField] private float zoomSmoothing = 5f; // Zoom smoothing amount
    
    [Header("Rotation Settings")]
    [Range(50f, 300f)]
    [SerializeField] private float rotationSpeed = 100f; // Speed of camera rotation
    [Range(5f, 45f)]
    [SerializeField] private float minVerticalAngle = 10f; // Minimum vertical angle (prevents looking too horizontal)
    [Range(45f, 85f)]
    [SerializeField] private float maxVerticalAngle = 80f; // Maximum vertical angle (prevents looking straight down)
    [SerializeField] private bool invertHorizontalRotation = false; // Reverse horizontal rotation direction
    [SerializeField] private bool invertVerticalRotation = false; // Reverse vertical rotation direction
    
    [Header("Smoothing")]
    [SerializeField] private bool smoothRotation = false; // Smooth camera rotation when rotating with mouse
    [Range(1f, 20f)]
    [Tooltip("How smooth the rotation feels. Higher = smoother/slower, Lower = snappier/faster")]
    [SerializeField] private float rotationSmoothing = 8f; // Rotation smoothing amount
    
    [Header("Collision Detection")]
    [SerializeField] private bool enableCollisionDetection = true; // Toggle collision detection
    [SerializeField] private LayerMask collisionLayers = -1; // Layers to check for collisions
    [Range(0.1f, 2f)]
    [SerializeField] private float collisionRadius = 0.5f; // Radius of collision sphere
    [Range(0.5f, 2f)]
    [SerializeField] private float collisionBuffer = 1f; // Extra space from collision point
    
    // Private variables
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 45f; // Default isometric angle
    private float targetHorizontalAngle = 0f; // Target for smooth rotation
    private float targetVerticalAngle = 45f; // Target for smooth rotation
    private float currentDistance; // Current zoom distance
    private float targetDistance; // Target zoom distance
    private bool isRotating = false;
    private Vector2 lastMousePosition;
    
    private void Start()
    {
        // Auto-find player if needed
        if (target == null && autoFindPlayer)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                GameDebug.LogWarning("FollowCamera: No target assigned and no GameObject with 'Player' tag found!",
                    config => config.enableDebugLogs, this);
            }
        }
        
        // Initialize camera angle and distance from current position
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentHorizontalAngle = angles.y;
            currentVerticalAngle = angles.x;

            // Clamp vertical angle to valid range
            if (currentVerticalAngle > 180f) currentVerticalAngle -= 360f;
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);

            // Initialize zoom distance
            currentDistance = distance;
            targetDistance = distance;

            // Initialize rotation targets
            targetHorizontalAngle = currentHorizontalAngle;
            targetVerticalAngle = currentVerticalAngle;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) return;
        
        HandleRotationInput();
        HandleZoomInput();
        UpdateCameraPosition();
    }
    
    private void HandleRotationInput()
    {
        // Check for right mouse button
        if (Mouse.current != null)
        {
            bool rightButtonPressed = Mouse.current.rightButton.isPressed;
            
            if (rightButtonPressed && !isRotating)
            {
                // Start rotating
                isRotating = true;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            else if (!rightButtonPressed && isRotating)
            {
                // Stop rotating
                isRotating = false;
            }
            
            if (isRotating)
            {
                // Get mouse delta
                Vector2 currentMousePosition = Mouse.current.position.ReadValue();
                Vector2 mouseDelta = currentMousePosition - lastMousePosition;
                lastMousePosition = currentMousePosition;

                // Apply rotation
                ApplyRotation(mouseDelta.x * rotationSpeed * Time.deltaTime,
                             mouseDelta.y * rotationSpeed * Time.deltaTime);
            }
        }
        
        // Gamepad support - right stick for rotation while holding a button
        if (Gamepad.current != null)
        {
            // Hold right shoulder button (R1/RB) and use right stick to rotate
            if (Gamepad.current.rightShoulder.isPressed)
            {
                Vector2 stickInput = Gamepad.current.rightStick.ReadValue();
                
                if (stickInput.magnitude > 0.1f) // Deadzone
                {
                    ApplyRotation(stickInput.x * rotationSpeed * Time.deltaTime,
                                 stickInput.y * rotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    private void HandleZoomInput()
    {
        // Mouse scroll wheel zoom
        if (Mouse.current != null && Mouse.current.scroll != null)
        {
            Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                // Adjust target distance based on scroll
                targetDistance -= scrollDelta.y * zoomSpeed * 0.01f; // Scale down scroll value
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }
        
        // Gamepad zoom with triggers or d-pad
        if (Gamepad.current != null)
        {
            // Use d-pad up/down for zoom
            float zoomInput = 0f;
            if (Gamepad.current.dpad.up.isPressed)
                zoomInput = -1f;
            else if (Gamepad.current.dpad.down.isPressed)
                zoomInput = 1f;
            
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                targetDistance += zoomInput * zoomSpeed * Time.deltaTime;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }
        }
        
        // Apply zoom with or without smoothing
        if (smoothZoom)
        {
            // Invert smoothing value so higher = smoother (more intuitive)
            float lerpSpeed = (16f - zoomSmoothing) * Time.deltaTime;
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, lerpSpeed);
        }
        else
        {
            currentDistance = targetDistance; // Instant zoom
        }
    }
    
    private void UpdateCameraPosition()
    {
        // Apply rotation smoothing if enabled
        if (smoothRotation)
        {
            // Invert smoothing value so higher = smoother (more intuitive)
            // Convert smoothing (1-20) to lerp speed where higher smoothing = slower movement
            float lerpSpeed = (21f - rotationSmoothing) * Time.deltaTime;

            // Use LerpAngle for horizontal to handle 360-0 degree wrapping properly
            currentHorizontalAngle = Mathf.LerpAngle(currentHorizontalAngle, targetHorizontalAngle, lerpSpeed);
            currentVerticalAngle = Mathf.Lerp(currentVerticalAngle, targetVerticalAngle, lerpSpeed);
        }
        else
        {
            currentHorizontalAngle = targetHorizontalAngle;
            currentVerticalAngle = targetVerticalAngle;
        }

        // Get the player's position with offset
        Vector3 playerCenter = target.position + Vector3.up * lookAtOffset;

        // Calculate camera position based on angles and current distance
        float radianH = currentHorizontalAngle * Mathf.Deg2Rad;
        float radianV = currentVerticalAngle * Mathf.Deg2Rad;

        // Calculate offset from player using current zoom distance
        float x = Mathf.Sin(radianH) * Mathf.Cos(radianV) * currentDistance;
        float y = Mathf.Sin(radianV) * currentDistance;
        float z = -Mathf.Cos(radianH) * Mathf.Cos(radianV) * currentDistance;

        // Position relative to player
        Vector3 desiredPosition = playerCenter + new Vector3(x, y, z);

        // Apply collision detection if enabled
        if (enableCollisionDetection)
        {
            desiredPosition = HandleCollision(desiredPosition, playerCenter);
        }

        // Set camera position
        transform.position = desiredPosition;

        // Look at the player
        transform.LookAt(playerCenter);
    }
    
    private Vector3 HandleCollision(Vector3 desiredPosition, Vector3 targetPoint)
    {
        Vector3 direction = desiredPosition - targetPoint;
        float desiredDistance = direction.magnitude;

        // Use SphereCast for main collision detection (combines ray and volume check)
        RaycastHit hit;
        if (Physics.SphereCast(targetPoint, collisionRadius, direction.normalized, out hit,
                               desiredDistance, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != target) // Don't collide with the player
            {
                float safeDistance = hit.distance - collisionBuffer;
                safeDistance = Mathf.Max(safeDistance, 2f); // Minimum 2 units from player
                return targetPoint + direction.normalized * safeDistance;
            }
        }

        return desiredPosition;
    }
    
    /// <summary>
    /// Applies rotation delta to camera angles with inversion and clamping.
    /// </summary>
    private void ApplyRotation(float horizontalDelta, float verticalDelta)
    {
        targetHorizontalAngle += invertHorizontalRotation ? -horizontalDelta : horizontalDelta;
        targetVerticalAngle -= invertVerticalRotation ? -verticalDelta : verticalDelta;

        // Clamp vertical angle
        targetVerticalAngle = Mathf.Clamp(targetVerticalAngle, minVerticalAngle, maxVerticalAngle);

        // Wrap horizontal angle
        if (targetHorizontalAngle > 360f) targetHorizontalAngle -= 360f;
        if (targetHorizontalAngle < 0f) targetHorizontalAngle += 360f;
    }

    /// <summary>
    /// Sets a new target for the camera to follow.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // GetTarget() removed - access target directly if needed
    
    /// <summary>
    /// Resets the camera to default isometric angle and zoom.
    /// </summary>
    public void ResetCamera()
    {
        currentHorizontalAngle = 0f;
        currentVerticalAngle = 45f;
        targetHorizontalAngle = 0f;
        targetVerticalAngle = 45f;
        targetDistance = distance;
        currentDistance = distance;
    }
    }
}