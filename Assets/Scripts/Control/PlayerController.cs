using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using RPG.Movement; // For Mover component
using RPG.Combat;   // For Fighter component
using RPG.Core;
using RPG.Stats;

namespace RPG.Control
{
    /// <summary>
    /// Handles all player input and controls various components (Mover, Combat, etc.)
    /// This is the brain that decides WHAT to do based on player input.
    /// The actual execution (HOW to do it) is delegated to specialized components.
    /// </summary>
    [RequireComponent(typeof(Mover))]
    public class PlayerController : MonoBehaviour
    {
    [Header("Input Settings")]
    [SerializeField] private bool mouseMovementEnabled = true;
    [SerializeField] private bool keyboardMovementEnabled = true;
    [SerializeField] private bool controllerMovementEnabled = true;

    [Header("Mouse Settings")]
    [SerializeField] private LayerMask groundLayerMask = -1; // Layers for raycastable ground
    [SerializeField] private float maxClickDistance = 100f; // Maximum raycast distance

    [Header("Controller Settings")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float controllerDeadzone = 0.2f;

    public enum InputPriority
    {
        Additive,    // Opposing inputs cancel out (A+D = no movement)
        LastPressed  // Last pressed key takes priority (A then D = move right)
    }

    [Header("WASD Settings")]
    [SerializeField] private InputPriority inputPriority = InputPriority.Additive;

    // Component references
    private Mover mover;
    private Fighter fighter;
    private Camera mainCamera;
    private RPG.Stats.Health health;

    // Input tracking for LastPressed mode
    private float lastHorizontalInput = 0f;
    private float lastVerticalInput = 0f;
    private bool wasPressingA = false;
    private bool wasPressingD = false;
    private bool wasPressingW = false;
    private bool wasPressingS = false;

    // State tracking
    private bool wasUsingDirectInput = false;

    // Cursor affordance
    private enum CursorType
    {
        None,
        Movement,
        Combat,
        UI,
        Interact
    }
    private CursorType currentCursorType = CursorType.None;
    private GameObject currentHoverTarget = null;

    // Raycast optimization - pre-allocated array to avoid garbage collection
    private readonly RaycastHit[] raycastHits = new RaycastHit[10]; // Adjust size based on expected max hits

    private void Start()
    {
        mover = GetComponent<Mover>();
        fighter = GetComponent<Fighter>();
        health = GetComponent<RPG.Stats.Health>();
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            GameDebug.LogError("Main Camera not found! Ensure a camera is tagged 'MainCamera'.", this);
        }
    }

    private void Update()
    {
        // Don't process input if dead
        if (health != null && health.IsDead())
        {
            return;
        }

        // Update cursor affordance (what the cursor is hovering over)
        UpdateCursorAffordance();

        // Process inputs in priority order
        // Direct movement (WASD/Controller) takes priority over point-and-click

        bool directInputActive = false;

        // Check for WASD input
        if (keyboardMovementEnabled)
        {
            Vector2 wasdInput = GetWASDInput();
            if (wasdInput != Vector2.zero)
            {
                HandleDirectMovement(wasdInput);
                directInputActive = true;
                wasUsingDirectInput = true;
            }
        }

        // Check for controller input
        if (!directInputActive && controllerMovementEnabled)
        {
            Vector2 controllerInput = GetControllerInput();
            if (controllerInput != Vector2.zero)
            {
                HandleDirectMovement(controllerInput);
                directInputActive = true;
                wasUsingDirectInput = true;
            }
        }

        // If no direct input, check for mouse input
        if (!directInputActive && mouseMovementEnabled)
        {
            ProcessMouseInput();
        }

        // Stop movement only when releasing direct input (WASD/Controller)
        // Never stop pathfinding automatically
        if (!directInputActive && wasUsingDirectInput)
        {
            mover.Cancel();
            wasUsingDirectInput = false;
        }
    }

    /// <summary>
    /// Updates cursor affordance based on what the mouse is hovering over
    /// </summary>
    private void UpdateCursorAffordance()
    {
        if (Mouse.current == null || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        CursorType newCursorType = CursorType.None;
        GameObject newHoverTarget = null;

        // Check for combat targets first (higher priority)
        int numHits = Physics.RaycastNonAlloc(ray, raycastHits, maxClickDistance);
        for (int i = 0; i < numHits; i++)
        {
            // Check for combat target
            if (raycastHits[i].transform.TryGetComponent<CombatTarget>(out CombatTarget combatTarget))
            {
                newCursorType = CursorType.Combat;
                newHoverTarget = raycastHits[i].transform.gameObject;
                break; // Combat has priority, stop checking
            }

            // In future, check for other interactables here
            // e.g., NPCs, Chests, Doors, etc.
        }

        // If no interactables found, check for movement
        if (newCursorType == CursorType.None)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, groundLayerMask))
            {
                newCursorType = CursorType.Movement;
                newHoverTarget = hit.transform.gameObject;
            }
        }

        // Update cursor state when it changes
        if (newCursorType != currentCursorType || newHoverTarget != currentHoverTarget)
        {
            currentCursorType = newCursorType;
            currentHoverTarget = newHoverTarget;

            // TODO: Change cursor sprite based on type
            // Cursor.SetCursor(cursorTexture, hotspot, cursorMode);
        }
    }

    /// <summary>
    /// Processes mouse input for point-and-click movement and combat
    /// </summary>
    private void ProcessMouseInput()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            // Try combat first, then movement
            if (InteractWithCombat()) return;
            if (InteractWithMovement()) return;
        }
    }

    private bool InteractWithCombat()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        int numHits = Physics.RaycastNonAlloc(ray, raycastHits, maxClickDistance);

        for (int i = 0; i < numHits; i++)
        {
            // TryGetComponent doesn't allocate memory on failure
            if (!raycastHits[i].transform.TryGetComponent(out CombatTarget target))
                continue;

            if (fighter == null) continue;

            if (fighter.CanAttack(target.gameObject))
            {
                fighter.Attack(target.gameObject);
                return true;
            }
        }

        return false;
    }

    private bool InteractWithMovement()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxClickDistance, groundLayerMask))
        {
            // Tell the mover to go to this position
            mover.MoveTo(hit.point);

            // Cancel combat when clicking on ground
            if (fighter != null)
            {
                fighter.Cancel();
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles direct movement input (WASD or Controller)
    /// </summary>
    private void HandleDirectMovement(Vector2 input)
    {
        // Convert 2D input to camera-relative 3D direction
        Vector3 moveDirection = GetCameraRelativeDirection(input);

        // Tell the mover to move in this direction
        mover.MoveInDirection(moveDirection);

        // Cancel combat when using direct movement
        if (fighter != null)
        {
            fighter.Cancel();
        }
    }

    /// <summary>
    /// Converts 2D input to camera-relative 3D world direction
    /// </summary>
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (mainCamera == null) return Vector3.zero;

        // Get camera's forward and right vectors, projected to horizontal plane
        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward = cameraForward.normalized;

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight = cameraRight.normalized;

        // Combine: right * x + forward * y
        Vector3 direction = (cameraRight * input.x) + (cameraForward * input.y);
        return direction.normalized;
    }

    /// <summary>
    /// Gets WASD keyboard input
    /// </summary>
    private Vector2 GetWASDInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        if (inputPriority == InputPriority.Additive)
        {
            // Original behavior: opposing inputs cancel out
            float horizontal = 0f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.aKey.isPressed) horizontal -= 1f;

            float vertical = 0f;
            if (keyboard.wKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;

            return new Vector2(horizontal, vertical).normalized;
        }
        else // LastPressed
        {
            // Track key press changes for horizontal
            bool pressingA = keyboard.aKey.isPressed;
            bool pressingD = keyboard.dKey.isPressed;

            // Detect new presses and update horizontal
            if (pressingA && !wasPressingA)
            {
                lastHorizontalInput = -1f;
            }
            else if (pressingD && !wasPressingD)
            {
                lastHorizontalInput = 1f;
            }
            else if (!pressingA && !pressingD)
            {
                lastHorizontalInput = 0f;
            }
            // If both keys are held but one was released, use the one still held
            else if (wasPressingA && wasPressingD)
            {
                if (!pressingA && pressingD) lastHorizontalInput = 1f;
                else if (pressingA && !pressingD) lastHorizontalInput = -1f;
            }

            // Track key press changes for vertical
            bool pressingW = keyboard.wKey.isPressed;
            bool pressingS = keyboard.sKey.isPressed;

            // Detect new presses and update vertical
            if (pressingW && !wasPressingW)
            {
                lastVerticalInput = 1f;
            }
            else if (pressingS && !wasPressingS)
            {
                lastVerticalInput = -1f;
            }
            else if (!pressingW && !pressingS)
            {
                lastVerticalInput = 0f;
            }
            // If both keys are held but one was released, use the one still held
            else if (wasPressingW && wasPressingS)
            {
                if (!pressingW && pressingS) lastVerticalInput = -1f;
                else if (pressingW && !pressingS) lastVerticalInput = 1f;
            }

            // Update previous state
            wasPressingA = pressingA;
            wasPressingD = pressingD;
            wasPressingW = pressingW;
            wasPressingS = pressingS;

            return new Vector2(lastHorizontalInput, lastVerticalInput).normalized;
        }
    }

    /// <summary>
    /// Gets controller input with deadzone applied
    /// </summary>
    private Vector2 GetControllerInput()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return Vector2.zero;

        Vector2 stickInput = gamepad.leftStick.ReadValue();

        // Apply deadzone
        if (stickInput.magnitude < controllerDeadzone)
        {
            return Vector2.zero;
        }

        // Normalize if beyond 1
        if (stickInput.magnitude > 1f)
        {
            stickInput = stickInput.normalized;
        }

        return stickInput;
    }
    }
}