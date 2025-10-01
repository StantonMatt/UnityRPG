# Unity C# Coding Standards and Best Practices (Unity 6 Edition)

## C# Version & Unity Compatibility

**Unity 6 (Latest Stable - Released October 2024) uses:**

- **C# Language Version:** 9.0
- **Compiler:** Roslyn
- **.NET API:** .NET Standard 2.1
- **Garbage Collector:** Boehm-Demers-Weiser

**Available C# 9 Features:**

- Init-only properties
- Enhanced pattern matching (relational patterns, logical patterns)
- Target-typed new expressions
- Records (requires IsExternalInit workaround - see below)
- Top-level statements (limited use in Unity)

**Not Yet Available (Coming in Future Versions):**

- C# 10+ features (file-scoped namespaces, global usings, string interpolation improvements)
- Native .NET 5+ runtime
- Note: Unity announced .NET modernization and CoreCLR integration for next-generation releases (Unity 7+), expected in open beta 2025+

---

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

### 3. Cache Transform References (When Necessary)

❌ **BAD:**

```csharp
private void Update()
{
    transform.position += velocity * Time.deltaTime; // Multiple property accesses
}
```

✅ **GOOD (Performance-Critical Code):**

```csharp
private Transform cachedTransform;

private void Awake()
{
    cachedTransform = transform; // Cache in extremely hot paths
}

private void Update()
{
    cachedTransform.position += velocity * Time.deltaTime;
}
```

**Note:** Transform caching is micro-optimization. Only cache in performance-critical paths (e.g., updating hundreds of objects per frame). For typical gameplay code, direct `transform` access is fine.

### 4. Use readonly for Immutable References

✅ **GOOD:**

```csharp
private readonly RaycastHit[] raycastHits = new RaycastHit[10]; // Can't accidentally reassign
private readonly StringBuilder sb = new StringBuilder(100); // Pre-sized for expected use
private readonly List<Enemy> enemyPool = new(); // C# 9 target-typed new
```

### 5. String Operations Optimization

❌ **BAD:**

```csharp
private void Update()
{
    textMesh.text = "Health: " + currentHealth + "/" + maxHealth; // Allocates every frame!
    string status = $"Level {level} - {playerName}"; // String interpolation allocates
}
```

✅ **GOOD:**

```csharp
private readonly StringBuilder sb = new StringBuilder(50);

private void UpdateHealthText()
{
    sb.Clear();
    sb.Append("Health: ").Append(currentHealth).Append("/").Append(maxHealth);
    textMesh.text = sb.ToString();
}

// Or cache if values change rarely
private string cachedHealthText;
private int lastHealth = -1;

private void Update()
{
    if (currentHealth != lastHealth)
    {
        lastHealth = currentHealth;
        cachedHealthText = $"Health: {currentHealth}/{maxHealth}";
        textMesh.text = cachedHealthText;
    }
}
```

**Note:** Future C# versions will have interpolated string handlers that improve performance, but for Unity 6, stick with StringBuilder in hot paths.

---

## Modern C# 9 Features

### 6. Use Init-only Properties for Immutable Configuration

✅ **GOOD:**

```csharp
// Perfect for configuration that shouldn't change after initialization
public class WeaponConfig
{
    public string Name { get; init; }
    public float Damage { get; init; }
    public float Range { get; init; }
    public int AmmoCapacity { get; init; }
}

// Usage
var rifle = new WeaponConfig
{
    Name = "Assault Rifle",
    Damage = 25f,
    Range = 100f,
    AmmoCapacity = 30
};
// rifle.Damage = 50f; // Compile error - init-only!

// Also great for component configuration
public class EnemySpawner : MonoBehaviour
{
    public int MaxEnemies { get; init; } = 10;
    public float SpawnInterval { get; init; } = 2f;
}
```

### 7. Enhanced Pattern Matching for Cleaner Logic

✅ **GOOD:**

```csharp
// Relational patterns with logical combinators
public void ProcessEnemy(Enemy enemy)
{
    // Property patterns with and/or/not
    if (enemy is { Health: > 0 and < 50, IsAggressive: true })
    {
        enemy.Retreat();
    }

    // Null checking with pattern
    if (target is not null && target.Health > 0)
    {
        Attack(target);
    }

    // Type pattern with property pattern
    if (collider.gameObject.GetComponent<IInteractable>() is { IsEnabled: true } interactable)
    {
        interactable.Interact();
    }
}

// Switch expressions with relational patterns
public string GetThreatLevel(Enemy enemy) => enemy switch
{
    { Health: > 80, Damage: > 50 } => "Critical",
    { Health: > 50, Damage: > 25 } => "High",
    { Health: > 25 } => "Moderate",
    { Health: > 0 } => "Low",
    _ => "Neutralized"
};

// Nested property patterns
public bool IsValidTarget(GameObject target) => target switch
{
    { layer: var layer } when layer == enemyLayer => target.TryGetComponent<Health>(out var health) && health.IsAlive,
    _ => false
};

// Pattern matching in switch expressions
public float CalculateDamageModifier(Character character) => character switch
{
    Warrior { Level: > 10, HasBuff: true } => 1.5f,
    Mage { Mana: > 50 } => 1.3f,
    Rogue { IsStealth: true } => 2.0f,
    _ => 1.0f
};
```

### 8. Target-typed New Expressions

✅ **GOOD:**

```csharp
// Cleaner initialization with target-typed new (C# 9)
private List<Transform> targets = new(); // Type inferred from declaration
private Dictionary<int, Enemy> enemies = new();
private Queue<GameObject> objectPool = new(50); // With capacity

// Works with fields, properties, and return types
public List<Item> Inventory { get; } = new();

private Dictionary<string, int> CreateScoreTable() => new()
{
    ["Player1"] = 100,
    ["Player2"] = 85
};

// Especially useful with longer generic types
private Dictionary<string, List<WeaponData>> weaponsByCategory = new();
```

### 9. Records for Data Transfer Objects (Use with Caution)

⚠️ **IMPORTANT:** Records require a workaround and have limitations in Unity:

```csharp
// First, add this ONCE to your project (in any file)
namespace System.Runtime.CompilerServices
{
    internal class IsExternalInit { }
}

// Now you can use records for immutable data structures
public record WeaponData(string Name, float Damage, float Range);
public record PlayerStats(int Health, int Mana, float Speed);
public record GameConfig(int MaxPlayers, float TimeLimit, bool FriendlyFire);

// Usage
var weapon = new WeaponData("Sword", 50f, 2f);
var modified = weapon with { Damage = 60f }; // Creates new instance with modified value

// Records are great for:
// - Data passed between systems
// - Event data
// - Configuration snapshots
// - Value objects in domain logic

public record EnemySpawnedEvent(Vector3 Position, EnemyType Type, int Level);
public record SaveData(string PlayerName, int Level, Vector3 LastPosition);
```

❌ **DO NOT USE RECORDS FOR:**

- Unity serialized fields (they won't serialize properly)
- MonoBehaviour or ScriptableObject classes
- Data that needs to appear in the Unity Inspector

✅ **GOOD USE CASES:**

```csharp
// Event system
public record DamageEvent(GameObject Source, GameObject Target, float Amount);

// Internal data structures
public record PathfindingRequest(Vector3 Start, Vector3 End, int Priority);

// Configuration snapshots
public record GameState(int Score, float TimeRemaining, bool IsPaused);
```

### 10. Modern Null Checking Patterns

✅ **GOOD:**

```csharp
// Pattern-based null checking (cleaner than != null)
if (component is not null)
{
    component.DoSomething();
}

// Null-coalescing with pattern matching
if (TryGetTarget() is { } target)
{
    Attack(target);
}

// Not pattern in conditions
if (enemy is not { IsAlive: true })
{
    return;
}

// Safe navigation remains useful
healthBar?.UpdateDisplay(currentHealth);
```

---

## Architecture & Dependencies

### 11. Avoid Circular Dependencies

❌ **BAD - Circular Dependency:**

```csharp
// Player.cs
public class Player : MonoBehaviour
{
    private Inventory inventory;

    public void PickupItem(Item item)
    {
        inventory.AddItem(item);
        item.SetOwner(this); // Item now references Player
    }
}

// Inventory.cs
public class Inventory : MonoBehaviour
{
    private Player player; // Inventory references Player

    public void AddItem(Item item)
    {
        player.UpdateStats(item.stats); // Circular reference!
    }
}

// Item.cs
public class Item : MonoBehaviour
{
    private Player owner; // Item also references Player

    public void Use()
    {
        owner.inventory.RemoveItem(this); // Multiple circular paths!
    }
}
```

✅ **GOOD - Dependency Inversion:**

```csharp
// Interfaces for dependency inversion
public interface IItemContainer
{
    void AddItem(ItemData item);
    void RemoveItem(ItemData item);
}

public interface IStatsProvider
{
    void ModifyStats(StatModifier modifier);
}

// Player only knows about interfaces
public class Player : MonoBehaviour, IStatsProvider
{
    private IItemContainer inventory;

    public void ModifyStats(StatModifier modifier)
    {
        // Apply stats without knowing about items
    }
}

// Inventory doesn't know about Player
public class Inventory : MonoBehaviour, IItemContainer
{
    public event Action<ItemData> OnItemAdded;

    public void AddItem(ItemData item)
    {
        items.Add(item);
        OnItemAdded?.Invoke(item); // Use events for communication
    }
}

// Item is just data, no dependencies
[System.Serializable]
public class ItemData
{
    public string id;
    public StatModifier statModifier;
    // No references to game objects
}
```

✅ **GOOD - Mediator Pattern:**

```csharp
// Central mediator handles communication
public class GameplayMediator : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Inventory inventory;
    [SerializeField] private CombatSystem combat;

    private void Start()
    {
        // Wire up events
        inventory.OnItemAdded += HandleItemAdded;
        combat.OnEnemyDefeated += HandleEnemyDefeated;
    }

    private void HandleItemAdded(ItemData item)
    {
        player.ModifyStats(item.statModifier);
    }

    private void HandleEnemyDefeated(Enemy enemy)
    {
        inventory.AddItem(enemy.lootTable.GetRandomItem());
    }
}
```

### 12. Detect and Prevent Circular Dependencies

```csharp
// Use assembly definitions to enforce boundaries
// RPG.Core.asmdef - No dependencies
// RPG.Combat.asmdef - Depends on Core only
// RPG.Inventory.asmdef - Depends on Core only
// RPG.Player.asmdef - Depends on Core, Combat, Inventory

// This prevents Combat from referencing Player, enforcing clean architecture
```

### 13. Use RequireComponent for Dependencies

✅ **GOOD:**

```csharp
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Mover))]
public class Fighter : MonoBehaviour
{
    // Guarantees these components exist
    private NavMeshAgent agent;
    private Mover mover;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = GetComponent<Mover>();
    }
}
```

### 14. Use Namespaces to Organize Code

✅ **GOOD:**

```csharp
namespace RPG.Movement { }  // Low-level systems
namespace RPG.Combat { }    // Combat systems
namespace RPG.Control { }   // High-level control
namespace RPG.Core { }      // Shared utilities
namespace RPG.UI { }        // User interface
namespace RPG.Saving { }    // Save system
namespace RPG.Stats { }     // Character stats
```

**Note:** File-scoped namespaces (C# 10+) are not yet available in Unity 6, but will reduce indentation in future versions:

```csharp
// Future C# 10+ (not yet in Unity 6)
// namespace RPG.Combat; // No braces needed
```

### 15. Follow Dependency Rules

- **High-level → Low-level** (never reverse)
- **Control → Movement** ✅
- **Movement → Control** ❌ (creates circular dependency)
- Keep Core independent of everything
- UI can reference gameplay but not vice versa

### 16. Separate Concerns

- **Controller** (PlayerController, AIController) - Decides WHAT to do
- **Executor** (Mover, Fighter) - Knows HOW to do it
- **Data** (ScriptableObjects) - Stores configuration
- Controllers can be swapped without changing executors

---

## ScriptableObjects for Data Management

### 17. Use ScriptableObjects for Shared Configuration

✅ **GOOD:**

```csharp
[CreateAssetMenu(fileName = "WeaponData", menuName = "RPG/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Base Stats")]
    [Range(1f, 100f)]
    public float damage = 10f;

    [Range(0.5f, 10f)]
    public float range = 2f;

    [Header("Visual")]
    public GameObject prefab;
    public AnimationClip attackAnimation;

    // Shared across all instances - no duplication
}

// Usage
[SerializeField] private WeaponData weaponData;
```

### 18. ScriptableObject Events for Decoupling

```csharp
[CreateAssetMenu(fileName = "GameEvent", menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private readonly List<GameEventListener> listeners = new();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised();
    }

    public void RegisterListener(GameEventListener listener) => listeners.Add(listener);
    public void UnregisterListener(GameEventListener listener) => listeners.Remove(listener);
}
```

---

## Event Systems & Decoupling

### 19. Choose the Right Event System

```csharp
// UnityEvents - For Inspector configuration
public class Health : MonoBehaviour
{
    [SerializeField] private UnityEvent<float> onHealthChanged;
    [SerializeField] private UnityEvent onDeath;
}

// C# Events - For code-only subscriptions
public class Player : MonoBehaviour
{
    public event Action<int> OnLevelUp;
    public static event Action<Player> OnPlayerSpawned; // Static for game-wide
}

// Event Bus Pattern - For complex systems
public static class EventBus
{
    private static readonly Dictionary<Type, List<object>> subscriptions = new();

    public static void Subscribe<T>(Action<T> callback) { /* ... */ }
    public static void Publish<T>(T eventData) { /* ... */ }
}

// Modern approach with records for event data
public static class GameEvents
{
    public record PlayerDied(string PlayerName, Vector3 Position);
    public record ItemCollected(string ItemId, int Amount);
    public record LevelCompleted(int LevelIndex, float CompletionTime);
}
```

---

## Unity-Specific Best Practices

### 20. Avoid Debug.Log in Production Code

❌ **BAD:**

```csharp
private void Update()
{
    Debug.Log($"Speed: {velocity}"); // Performance killer in Update!
}
```

✅ **GOOD:**

```csharp
// Conditional compilation
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log($"Speed: {velocity}");
#endif

// Or create a wrapper
public static class GameDebug
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message) => Debug.Log(message);
}
```

### 21. Use Range Attributes for Inspector Values

✅ **GOOD:**

```csharp
[Range(0.1f, 10f)]
[SerializeField] private float attackRange = 2f;

[Range(0f, 1f)]
[SerializeField] private float damageModifier = 1f;

[Min(0)]
[SerializeField] private int maxHealth = 100;
```

### 22. Use Tooltip Attributes for Documentation

✅ **GOOD:**

```csharp
[Tooltip("Maximum distance the character will pathfind. Prevents long paths around obstacles.")]
[SerializeField] private float maxPathDistance = 30f;
```

### 23. Header Attributes for Organization

✅ **GOOD:**

```csharp
[Header("Movement Settings")]
[SerializeField] private float speed = 5f;
[SerializeField] private float acceleration = 10f;

[Header("Combat Settings")]
[SerializeField] private float attackRange = 2f;
[SerializeField] private float attackCooldown = 1f;

[Space(10)]
[Header("Debug Options")]
[SerializeField] private bool showGizmos = true;
```

---

## Physics & Movement

### 24. Use FixedUpdate for Physics

❌ **BAD:**

```csharp
private void Update()
{
    rigidbody.AddForce(force); // Inconsistent physics
}
```

✅ **GOOD:**

```csharp
private void FixedUpdate()
{
    rigidbody.AddForce(force); // Consistent physics simulation
}

private void Update()
{
    // Input and non-physics updates here
}
```

### 25. Configure Layer Collision Matrix

```csharp
// Define layers clearly
public static class Layers
{
    public const int Player = 8;
    public const int Enemy = 9;
    public const int Ground = 10;
    public const int Interactable = 11;
}

// Use LayerMasks effectively
[SerializeField] private LayerMask groundLayerMask = 1 << Layers.Ground;
[SerializeField] private LayerMask enemyLayerMask = 1 << Layers.Enemy;
```

---

## Code Quality

### 26. Use TryGetComponent for Null-Safe Checks

✅ **GOOD:**

```csharp
if (target.TryGetComponent<Enemy>(out Enemy enemy))
{
    enemy.TakeDamage(damage);
}

// Combined with modern pattern matching
if (target.TryGetComponent<IInteractable>(out var interactable) && interactable is { IsEnabled: true })
{
    interactable.Interact();
}
```

### 27. Unity Null Checking Patterns

```csharp
// Unity's overloaded null check (checks if object is destroyed)
if (component != null)
{
    // Safe to use
}

// Pattern matching null check (modern C# 9)
if (component is not null)
{
    // Safe to use
}

// Actual null reference check (faster but doesn't check destroyed)
if (!ReferenceEquals(component, null))
{
    // Use when you know object can't be destroyed
}

// Safe navigation for chains
private void UpdateUI()
{
    healthText?.SetText($"HP: {currentHealth}");
}
```

### 28. Prefer Single Responsibility

- One class = one job
- Mover only moves
- Fighter only fights
- PlayerController only handles input
- Health only manages health

### 29. Use Descriptive Variable Names

❌ **BAD:** `float t = 1f;`
✅ **GOOD:** `float timeBetweenAttacks = 1f;`

### 30. Extract Magic Numbers to Named Variables

❌ **BAD:**

```csharp
if (distance < 2f) // What is 2?
```

✅ **GOOD:**

```csharp
[SerializeField] private float attackRange = 2f;
if (distance < attackRange)
```

---

## Performance Patterns

### 31. Pre-allocate Arrays for Repeated Operations

```csharp
public class CombatSystem : MonoBehaviour
{
    // Pre-allocate all reusable arrays
    private readonly Collider[] overlapResults = new Collider[20];
    private readonly RaycastHit[] raycastHits = new RaycastHit[10];
    private readonly ContactPoint[] contactPoints = new ContactPoint[10];
}
```

### 32. Use Object Pooling for Frequently Created Objects

```csharp
public class ObjectPool<T> where T : Component
{
    private readonly Queue<T> pool = new();
    private readonly T prefab;
    private readonly Transform parent;

    public T Get()
    {
        if (pool.Count == 0)
        {
            return Object.Instantiate(prefab, parent);
        }

        T item = pool.Dequeue();
        item.gameObject.SetActive(true);
        return item;
    }

    public void Return(T item)
    {
        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }
}

// Use for:
// - Projectiles
// - Particle effects
// - UI elements
// - Enemies in wave spawners
```

### 33. Avoid Instantiate/Destroy in Loops

❌ **BAD:** Creating/destroying objects every frame
✅ **GOOD:** Pool and reuse objects

---

## Async Programming

### 34. Coroutines vs Async/Await

```csharp
// Coroutines - Good for frame-based timing
private IEnumerator FadeOut()
{
    while (alpha > 0)
    {
        alpha -= Time.deltaTime;
        yield return null; // Wait one frame
    }
}

// Async/Await - Good for complex async operations
public async Task<SaveData> LoadSaveDataAsync()
{
    try
    {
        string json = await File.ReadAllTextAsync(savePath);
        return JsonUtility.FromJson<SaveData>(json);
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to load: {e.Message}");
        return null;
    }
}

// UniTask (3rd party) - Best of both worlds
public async UniTaskVoid AttackAsync()
{
    await UniTask.Delay(TimeSpan.FromSeconds(attackDelay));
    DealDamage();
}
```

**Note:** Unity 6 has improved async/await support, making native async more viable than in previous versions.

---

## Input & Movement

### 35. Separate Input Modes Clearly

```csharp
public class PlayerController : MonoBehaviour
{
    private enum ControlMode
    {
        Direct,      // WASD/Gamepad
        PointClick   // Mouse pathfinding
    }

    private ControlMode currentMode;

    private void Update()
    {
        // Modern switch expression with pattern matching
        switch (currentMode)
        {
            case ControlMode.Direct:
                HandleDirectMovement();
                break;
            case ControlMode.PointClick:
                HandlePointClickMovement();
                break;
        }
    }
}
```

### 36. Layer Masks for Selective Raycasting

```csharp
[SerializeField] private LayerMask groundLayerMask = -1; // What can be clicked
[SerializeField] private LayerMask enemyLayerMask;      // What can be attacked
[SerializeField] private LayerMask interactableMask;    // What can be interacted with
```

---

## Testing & Debugging

### 37. Make Code Testable

```csharp
// Separate logic from MonoBehaviour
public class HealthSystem // Pure C# class - testable
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    public void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(CurrentHealth);
        if (CurrentHealth == 0) OnDeath?.Invoke();
    }
}

// MonoBehaviour wrapper
public class Health : MonoBehaviour
{
    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = new HealthSystem();
        healthSystem.OnDeath += HandleDeath;
    }
}
```

### 38. Use Conditional Attributes for Debug Methods

```csharp
public class CombatDebugger : MonoBehaviour
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void DrawAttackRange()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogDamageDealt(float damage)
    {
        Debug.Log($"Dealt {damage} damage");
    }
}
```

---

## Prefab Workflow

### 39. Use Prefab Variants for Configuration

```csharp
// Base Prefab: Enemy.prefab
// Variants: Enemy_Goblin.prefab, Enemy_Orc.prefab
// Only override what changes (stats, models, etc.)
```

### 40. Nested Prefabs for Modularity

```csharp
// Character.prefab
//   ├── Model.prefab
//   ├── WeaponSlot.prefab
//   └── UI_HealthBar.prefab
// Each can be updated independently
```

---

## Version Control Best Practices

### 41. Force Text Serialization

```yaml
# ProjectSettings/EditorSettings.asset
Editor Settings:
  - Serialization Mode: Force Text
  - Line Endings: Unix (for cross-platform)
```

### 42. Meta Files and .gitignore

```gitignore
# Essential Unity entries
[Ll]ibrary/
[Tt]emp/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# Never ignore meta files
!*.meta
```

---

## Platform-Specific Optimizations

### 43. Mobile Optimization Flags

```csharp
public class QualityManager : MonoBehaviour
{
    private void Awake()
    {
        #if UNITY_IOS || UNITY_ANDROID
            Application.targetFrameRate = 30;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
        #else
            Application.targetFrameRate = 60;
        #endif
    }
}
```

### 44. Platform-Specific Input

```csharp
public class InputManager : MonoBehaviour
{
    private void Update()
    {
        #if UNITY_STANDALONE || UNITY_EDITOR
            HandleKeyboardMouse();
        #elif UNITY_IOS || UNITY_ANDROID
            HandleTouch();
        #elif UNITY_GAMECORE || UNITY_PS4 || UNITY_PS5
            HandleGamepad();
        #endif
    }
}
```

---

## Profiling Guidelines

### 45. Profile Before Optimizing

```csharp
// Use Profiler.BeginSample for custom profiling
public void ProcessEnemies()
{
    Profiler.BeginSample("ProcessEnemies");

    foreach (var enemy in enemies)
    {
        Profiler.BeginSample("Enemy.Update");
        enemy.UpdateAI();
        Profiler.EndSample();
    }

    Profiler.EndSample();
}
```

### 46. Common Performance Bottlenecks to Check

- Draw calls (combine meshes, use atlases)
- SetPass calls (share materials)
- Overdraw (optimize transparency)
- Physics calculations (simplify colliders)
- Garbage collection (check GC.Alloc in profiler)

---

## Summary Checklist for New Scripts

- [ ] Add namespace
- [ ] Use RequireComponent for dependencies
- [ ] Cache all components in Start/Awake
- [ ] Cache transform if used in performance-critical paths
- [ ] Pre-allocate arrays for Update operations
- [ ] Use TryGetComponent instead of GetComponent
- [ ] Add [Range], [Min], and [Tooltip] to SerializeFields
- [ ] Use readonly for immutable references
- [ ] No Debug.Log in Update without conditional compilation
- [ ] Follow dependency hierarchy (high → low)
- [ ] Single responsibility per class
- [ ] Consider ScriptableObjects for shared data
- [ ] Use appropriate event system for decoupling
- [ ] Physics in FixedUpdate, input in Update
- [ ] Setup LayerMasks for raycasting
- [ ] Consider object pooling for spawned objects
- [ ] Make logic testable by separating from MonoBehaviour
- [ ] Profile before optimizing performance
- [ ] Consider init-only properties for configuration
- [ ] Use pattern matching for complex conditionals
- [ ] Use target-typed new for cleaner initialization

---

## Modern C# 9 Quick Reference

```csharp
// Init-only properties
public class Config
{
    public int MaxPlayers { get; init; } = 4;
}

// Target-typed new
List<Enemy> enemies = new();
Dictionary<int, string> names = new();

// Pattern matching enhancements
if (enemy is { Health: > 0 and < 50, IsAggressive: true })
{
    // React to wounded aggressive enemy
}

string threat = enemy switch
{
    { Health: > 80 } => "High",
    { Health: > 30 } => "Medium",
    _ => "Low"
};

// Records (with IsExternalInit workaround)
public record GameEvent(string Type, float Timestamp);

// Not pattern
if (component is not null)
{
    component.Execute();
}
```

---

## Future-Proofing Notes

**Unity 7+ Expected Features (Open Beta 2025+):**

- .NET modernization with CoreCLR
- C# 10+ support (file-scoped namespaces, global usings)
- ECS integration with GameObjects
- Improved async/await performance
- Enhanced compilation pipeline

**Stay Updated:**

- Monitor Unity blog for roadmap updates
- Test beta versions for upcoming features
- Prepare for C# 10+ adoption when available

---

## Additional Resources

- [Unity Official C# Style Guide (2025)](https://unity.com/resources/c-sharp-style-guide-unity-6)
- [Microsoft C# 9 Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9)
- [Unity Manual: C# Compiler Reference](https://docs.unity3d.com/Manual/csharp-compiler.html)
- [Unity Performance Best Practices](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)

---

**Document Version:** Unity 6 Edition (2024-2025)  
**C# Version:** 9.0  
**Last Updated:** September 2025
