# Unity C# Coding Standards and Best Practices

## Memory Management & Performance

### 1. Avoid Allocations in Update Loops
❌ **BAD:**
```csharp
private void Update()
{
    RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance); // Allocates new array every frame
    Enemy enemy = hit.transform.GetComponent<Enemy>(); // Allocates even if null
}
```

✅ **GOOD:**
```csharp
private readonly RaycastHit[] raycastHits = new RaycastHit[10]; // Pre-allocated, reusable

private void Update()
{
    int numHits = Physics.RaycastNonAlloc(ray, raycastHits, maxDistance);
    if (!hit.transform.TryGetComponent<Enemy>(out Enemy enemy)) return; // No allocation on failure
}
```

### 2. Cache Component References
❌ **BAD:**
```csharp
private void Update()
{
    GetComponent<Animator>().SetFloat("Speed", velocity); // GetComponent every frame!
}
```

✅ **GOOD:**
```csharp
private Animator animator;

private void Start()
{
    animator = GetComponent<Animator>(); // Cache once
}

private void Update()
{
    animator.SetFloat("Speed", velocity); // Use cached reference
}
```

### 3. Use readonly for Immutable References
✅ **GOOD:**
```csharp
private readonly RaycastHit[] raycastHits = new RaycastHit[10]; // Can't accidentally reassign
```

## Architecture & Dependencies

### 4. Use RequireComponent for Dependencies
✅ **GOOD:**
```csharp
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Mover))]
public class Fighter : MonoBehaviour
{
    // Guarantees these components exist
}
```

### 5. Use Namespaces to Organize Code
✅ **GOOD:**
```csharp
namespace RPG.Movement { }  // Low-level systems
namespace RPG.Combat { }    // Combat systems
namespace RPG.Control { }   // High-level control
namespace RPG.Core { }      // Shared utilities
```

### 6. Follow Dependency Rules
- **High-level → Low-level** (never reverse)
- **Control → Movement** ✅
- **Movement → Control** ❌ (creates circular dependency)
- Keep Core independent of everything

### 7. Separate Concerns
- **Controller** (PlayerController, AIController) - Decides WHAT to do
- **Executor** (Mover, Fighter) - Knows HOW to do it
- Controllers can be swapped without changing executors

## Unity-Specific Best Practices

### 8. Avoid Debug.Log in Production Code
❌ **BAD:**
```csharp
private void Update()
{
    Debug.Log($"Speed: {velocity}"); // Performance killer in Update!
}
```

✅ **GOOD:**
```csharp
#if UNITY_EDITOR
    Debug.Log($"Speed: {velocity}"); // Only in editor
#endif
```

### 9. Use Range Attributes for Inspector Values
✅ **GOOD:**
```csharp
[Range(0.1f, 10f)]
[SerializeField] private float attackRange = 2f;

[Range(0f, 1f)]
[SerializeField] private float damageModifier = 1f;
```

### 10. Use Tooltip Attributes for Documentation
✅ **GOOD:**
```csharp
[Tooltip("Maximum distance the character will pathfind. Prevents long paths around obstacles.")]
[SerializeField] private float maxPathDistance = 30f;
```

### 11. Header Attributes for Organization
✅ **GOOD:**
```csharp
[Header("Movement Settings")]
[SerializeField] private float speed = 5f;

[Header("Combat Settings")]
[SerializeField] private float attackRange = 2f;
```

## Code Quality

### 12. Use TryGetComponent for Null-Safe Checks
✅ **GOOD:**
```csharp
if (target.TryGetComponent<Enemy>(out Enemy enemy))
{
    enemy.TakeDamage(damage);
}
```

### 13. Prefer Single Responsibility
- One class = one job
- Mover only moves
- Fighter only fights
- PlayerController only handles input

### 14. Use Descriptive Variable Names
❌ **BAD:** `float t = 1f;`
✅ **GOOD:** `float timeBetweenAttacks = 1f;`

### 15. Extract Magic Numbers to Named Variables
❌ **BAD:**
```csharp
if (distance < 2f) // What is 2?
```

✅ **GOOD:**
```csharp
[SerializeField] private float attackRange = 2f;
if (distance < attackRange)
```

## Performance Patterns

### 16. Pre-allocate Arrays for Repeated Operations
```csharp
private readonly Collider[] overlapResults = new Collider[20];
private readonly RaycastHit[] raycastHits = new RaycastHit[10];
```

### 17. Use Object Pooling for Frequently Created Objects
- Projectiles
- Particle effects
- UI elements

### 18. Avoid Instantiate/Destroy in Loops
❌ **BAD:** Creating/destroying objects every frame
✅ **GOOD:** Pool and reuse objects

## Input & Movement

### 19. Separate Input Modes Clearly
- Direct movement (WASD/Controller) - immediate response
- Pathfinding (Mouse click) - continues after release
- Don't mix behaviors

### 20. Layer Masks for Selective Raycasting
```csharp
[SerializeField] private LayerMask groundLayerMask = -1; // What can be clicked
[SerializeField] private LayerMask enemyLayerMask;      // What can be attacked
```

## Summary Checklist for New Scripts
- [ ] Add namespace
- [ ] Use RequireComponent for dependencies
- [ ] Cache all components in Start/Awake
- [ ] Pre-allocate arrays for Update operations
- [ ] Use TryGetComponent instead of GetComponent
- [ ] Add [Range] and [Tooltip] to SerializeFields
- [ ] Use readonly for immutable references
- [ ] No Debug.Log in Update
- [ ] Follow dependency hierarchy (high → low)
- [ ] Single responsibility per class