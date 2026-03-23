# Global Project Context

## Overview
This is a game development project built with Unity using a service-oriented architecture with dependency injection. The codebase follows clean architecture principles with clear separation between model (controllers), view, and service layers.

## Architecture Overview

### Service Locator Pattern
The project uses a **Service Locator** pattern for dependency management through the `ServicesLocator` singleton class.

**Key characteristics:**
- Centralized service discovery and initialization
- Automatic dependency ordering based on `GetDependencies()` declarations
- Services implement the `IService` interface
- Circular dependency detection
- All services are discoverable via reflection at runtime

**Service Lifecycle:**
1. `ServicesLocator.Awake()` triggers service discovery and initialization
2. Services are auto-discovered from all assemblies that implement `IService`
3. Dependencies are ordered topologically before initialization
4. `OnAllServicesInitialized` event fires when all services are ready
5. Services are reset in reverse order on shutdown

**Service Interface (IService):**
```csharp
public interface IService
{
    UniTask<bool> Initialize();      // Return true on success
    Type[] GetDependencies();         // Return dependent service types or null
    UniTask Reset();                  // Clean up resources
}
```

**Service with Feature Interface Pattern:**
Services can implement a feature-specific interface that extends `IService`:
```csharp
public interface IGameEventService : IService
{
    event Action OnHeroRestarted;
    void TriggerHeroRestart();
}

public class GameEventService : IGameEventService
{
    public Type[] GetDependencies() => null;
    
    public UniTask<bool> Initialize() => UniTask.FromResult(true);
    public UniTask Reset() => UniTask.CompletedTask;
    
    public event Action OnHeroRestarted;
    
    public void TriggerHeroRestart()
    {
        OnHeroRestarted?.Invoke();
    }
}
```

This pattern allows:
- Feature interfaces to extend `IService` (contract inheritance)
- Services to implement feature interfaces directly
- Auto-discovery via ServicesLocator (since feature interface extends IService)
- Type-safe access to feature-specific functionality

**Usage:**
```csharp
// Get a service
var service = ServicesLocator.Instance.GetService<MyService>();

// Subscribe to initialization event
ServicesLocator.Instance.OnAllServicesInitialized += MyInitHandler;
```

### Core Services

#### GameEventService
**Location:** `Core/ServicesManager/Scripts/GameEventService.cs`  
**Type:** Service implementing `IService`  
**Purpose:** Broadcast global game events that multiple features need to respond to without direct dependencies

**Features:**
- `OnHeroRestarted` event: Triggered when the hero dies and restarts
- Used by features (Weapons, Entities, etc.) to reset state/cleanup

**Example Usage:**
```csharp
// In WeaponsService or any feature that needs hero restart notification
GameEventService gameEventService = ServicesLocator.Instance.GetService<GameEventService>();
if (gameEventService != null)
{
    gameEventService.OnHeroRestarted += () =>
    {
        // Cleanup or reset logic
    };
}
```

**Rationale:**
- Enables cross-feature communication without creating dependencies
- Features can listen to global events without knowing about each other
- Central place for game-wide events that affect multiple systems

### Entities Service Pattern
The **EntitiesService** is the central orchestrator for game entity controllers (Hero, Enemies, etc.).

**Responsibilities:**
- Aggregate and expose entity controllers (HeroController, EnemiesController)
- Manage initialization order and dependencies between controllers
- Act as a single access point for all entity-related logic
- Declare dependencies on lower-level services (JoystickInputService, WeaponsService)

**Example structure:**
```csharp
public class EntitiesService : IService
{
    public Type[] GetDependencies() => new[] { typeof(JoystickInputService), typeof(WeaponsService) };
    
    public HeroController HeroController { get; private set; }
    public EnemiesController EnemiesController { get; private set; }
    
    public async UniTask<bool> Initialize()
    {
        // Resolve dependencies
        var joystickService = ServicesLocator.Instance.GetService<JoystickInputService>();
        var weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
        
        // Create and initialize controllers
        HeroController = new HeroController();
        EnemiesController = new EnemiesController();
        
        await HeroController.Initialize(EnemiesController, joystickService, weaponsService);
        await EnemiesController.Initialize(HeroController, weaponsService);
        
        return true;
    }
}
```

## Code Organization

### Directory Structure
```
Assets/
├── Core/
│   └── ServicesManager/
│       └── Scripts/
│           ├── IService.cs
│           └── ServicesLocator.cs
├── Features/
│   ├── Entities/
│   │   └── Scripts/
│   │       ├── Controllers/      (Model/Business Logic)
│   │       ├── Models/           (Data structures, state)
│   │       ├── View/             (MonoBehaviour views)
│   │       └── EntitiesService.cs
│   ├── Weapons/
│   │   └── Scripts/
│   ├── JoystickInput/
│   │   └── Scripts/
│   └── [Other Features]/
└── Docs/
    └── Global.md (this file)
```

### Layer Responsibilities

#### 1. **Controller Layer (Model/Business Logic)**
- Location: `Features/[Feature]/Scripts/Controllers/`
- Pattern: Non-MonoBehaviour classes
- Responsibilities:
  - Maintain state via immutable state structs (e.g., `HeroState`, `EnemyState`)
  - Execute business logic and state transitions
  - Publish events when state changes
  - Use UniTask for async operations
  - No direct dependency on Unity.UI or rendering

**Example:**
```csharp
public class HeroController
{
    // Events for state changes
    public event Action<HeroState> OnStateChanged;
    public event Action<int> OnHeroDamaged;
    
    // Internal immutable state
    private HeroState _currentState;
    
    // Public read-only access
    public HeroState CurrentState => _currentState;
    
    // State transitions create new state instances
    private void UpdatePosition()
    {
        Vector3 newPosition = /* calculate */;
        _currentState = new HeroState(newPosition, /* other fields */);
        OnStateChanged?.Invoke(_currentState);
    }
}
```

#### 2. **Model Layer (State/Data)**
- Location: `Features/[Feature]/Scripts/Models/`
- Pattern: Immutable structs with properties
- Responsibilities:
  - Represent runtime state and configurations
  - No behavior or side effects
  - Used as event payloads and data containers

**Example:**
```csharp
public struct HeroState
{
    public Vector3 Position { get; }
    public int Health { get; }
    public float LastAttackTime { get; }
    public Vector3 AttackToPosition { get; }
    
    public bool IsDead => Health <= 0;
    
    public HeroState(Vector3 position, int health, float lastAttackTime, Vector3 attackToPosition)
    {
        Position = position;
        Health = health;
        LastAttackTime = lastAttackTime;
        AttackToPosition = attackToPosition;
    }
}
```

#### 3. **View Layer (UI/Rendering)**
- Location: `Features/[Feature]/Scripts/View/`
- Pattern: MonoBehaviour classes
- Responsibilities:
  - Subscribe to controller events
  - Update visual representation based on state
  - Handle input (if applicable)
  - No business logic or state ownership
  - Use serialized fields for configuration

**Example:**
```csharp
public class HeroView : MonoBehaviour
{
    private HeroController _heroController;
    private Animator _animator;
    
    private void OnServicesInitialized()
    {
        _heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
        _heroController.OnStateChanged += OnHeroStateChanged;
    }
    
    private void OnHeroStateChanged(HeroState heroState)
    {
        // Update visuals based on state
        transform.position = heroState.Position;
        // ... update animator, effects, etc
    }
    
    private void OnDestroy()
    {
        if (_heroController != null)
            _heroController.OnStateChanged -= OnHeroStateChanged;
    }
}
```

#### 4. **Service Layer**
- Location: `Features/[Feature]/Scripts/[ServiceName]Service.cs`
- Pattern: Implements `IService`
- Responsibilities:
  - Provide reusable functionality across features
  - Manage global state or resources
  - Expose public APIs for other services/controllers

**Example:**
```csharp
public class WeaponsService : IService
{
    public Type[] GetDependencies() => null;  // No dependencies
    
    public WeaponController CurrentWeapon { get; private set; }
    public event Action<WeaponController> OnWeaponChanged;
    
    public UniTask<bool> Initialize()
    {
        // Initialize default weapon
        return UniTask.FromResult(true);
    }
    
    public UniTask Reset()
    {
        CurrentWeapon = null;
        return UniTask.CompletedTask;
    }
    
    public bool SwitchWeapon(string weaponId)
    {
        // Switch logic and event firing
    }
}
```

## Architectural Rules

### 1. Minimize Reflection Usage
- **Avoid reflection** except for service discovery in `ServicesLocator`
- Reflection is expensive at runtime and reduces code clarity
- Use direct dependencies and interfaces instead
- Only use reflection when absolutely necessary and document why
- Exception: Service discovery via `IService` is the intended use of reflection

**Example - Avoid:**
```csharp
// DON'T DO THIS
var field = typeof(MyClass).GetField("_myField", BindingFlags.NonPublic | BindingFlags.Instance);
field.SetValue(instance, value);
```

**Example - Correct:**
```csharp
// DO THIS - Use public API instead
myInstance.SetValue(value);
```

### 2. Strict MVC/MVP Pattern Adherence

#### Controller Responsibilities
- Maintain immutable state via structs
- Execute business logic and state transitions
- Publish events on state changes
- Coordinate with services
- **No visual/UI concerns**

#### View Responsibilities
- Subscribe to controller state changes
- Update visual representation based on state
- Handle user input and forward to controller
- **No business logic, only presentation logic**
- Examples of valid view logic: animations, visual effects timing, UI layout calculations

#### Model Responsibilities
- Store data as immutable structures
- Provide **pure helper methods** that process data without side effects
- Examples: `IsDead` (returns true if health ≤ 0), `IsInRange()`, `CanAttack()`
- **No logic that changes state or affects game behavior**

**Example - Correct Pattern:**
```csharp
// Model - Data + Pure Helpers
public struct EnemyState
{
    public int Health { get; }
    public bool IsDead => Health <= 0;  // Pure helper method
    
    public EnemyState(int health, /* ... */)
    {
        Health = health;
    }
}

// Controller - Business Logic
public class EnemyController
{
    private EnemyState _state;
    
    public void TakeDamage(int damage)
    {
        int newHealth = Mathf.Max(0, _state.Health - damage);
        _state = new EnemyState(newHealth, /* ... */);
        OnStateChanged?.Invoke(_state);  // Business decision: publish event
    }
}

// View - Presentation Only
public class EnemyView : MonoBehaviour
{
    private void OnEnemyStateChanged(EnemyState state)
    {
        // Presentation logic only
        if (state.IsDead)
        {
            _animator.SetTrigger("Death");
            _deathEffect.Play();
        }
    }
}
```

**Anti-pattern - Avoid:**
```csharp
// DON'T - Logic in View
public class EnemyView : MonoBehaviour
{
    private void TakeDamage(int damage)  // Business logic in view!
    {
        health -= damage;
        if (health <= 0) Die();
    }
}

// DON'T - Complex processing in Model
public struct EnemyState
{
    public void TakeDamage(int damage)  // State mutation in model!
    {
        Health -= damage;
    }
}
```

### 3. Domain Layers and Dependency Rules

The project follows a strict **layered architecture** with unidirectional dependencies:

#### Dependency Chain (top can depend on bottom, but not vice versa):
```
Features (Entities, Weapons, JoystickInput, etc.)
    ↓ (knows about)
    ↓ (can depend on)
Core (ServicesManager, IService)

Features do NOT know about each other (only through Core/Services)
Core does NOT know about any Feature
```

#### Feature-Specific Rules

**Entities Feature:**
- Can depend on: Core, Weapons, JoystickInput
- Cannot depend on: World, UI, or any other feature (except through services)
- Exposes: EntitiesService, HeroController, EnemiesController

**Weapons Feature:**
- Can depend on: Core
- Cannot depend on: Entities, JoystickInput, or any feature
- Exposes: WeaponsService, WeaponController, WeaponPickupController
- Subscribes to: Core GameEventService for cross-feature events

**JoystickInput Feature:**
- Can depend on: Core
- Cannot depend on: Entities, Weapons, or any feature
- Exposes: JoystickInputService

#### Communication Between Features
Use **Services and Events**, never direct imports:
```csharp
// CORRECT - Through Service Locator
var weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
var currentWeapon = weaponsService.CurrentWeapon;

// INCORRECT - Direct dependency
using Game.Weapons;  // Bad if in a feature that shouldn't know Weapons

public class MyController
{
    private WeaponsService _weaponsService;  // Direct coupling
}
```

**Cross-Feature Integration Pattern:**
When a lower-layer feature (Weapons) needs data from a higher-layer feature (Entities):
1. **Higher-layer feature** (Entities) initializes and owns the lower-layer component
2. **Higher-layer calls methods** on lower-layer to provide data (e.g., `SetHeroPosition()`)
3. **Lower-layer only knows about** Services, not about specific Entity controllers
4. **Example**: WeaponPickupController receives hero position via method call, not direct HeroController reference

```csharp
// In EntitiesService (higher layer)
WeaponPickupController = new WeaponPickupController();
await WeaponPickupController.Initialize(weaponsService);

// Somewhere in update loop or event
WeaponPickupController.SetHeroPosition(HeroController.CurrentState.Position);
```

#### Assembly Structure Reflection
```
Core/
├── ServicesManager/         (Knows nothing about features)
│   └── IService, ServicesLocator

Features/
├── Entities/                (Knows Core, Weapons, JoystickInput)
│   ├── Controllers/
│   ├── Models/
│   ├── View/
│   └── EntitiesService.cs

├── Weapons/                 (Knows Core only)
│   ├── Scripts/
│   └── WeaponsService.cs

├── JoystickInput/           (Knows Core only)
│   ├── Scripts/
│   └── JoystickInputService.cs

└── [Other Features]/
    └── Same pattern
```

**Rationale:**
- **Core is stable**: Changes to ServicesLocator don't require rebuilding features
- **Features are loosely coupled**: Can replace Weapons with different implementation without touching Entities
- **Easy to test**: Mock services for feature testing without dependencies on other features
- **Clear responsibility**: Each layer knows exactly what it depends on

### 4. Configuration Management with ScriptableObjects

#### ScriptableObject Usage Rules
- **All configuration data** must be stored in ScriptableObjects
- **Never modify** ScriptableObjects from code (write operations forbidden)
- **Configuration changes** are made exclusively in the Unity Editor
- **Read-only access** via public properties with private serializable backing fields
- **Singleton instances** for easy runtime access via `Instance` property

#### ScriptableObject Structure Pattern

**Example - Correct Pattern:**
```csharp
// Location: Features/[Feature]/Scripts/Config/[FeatureName]Config.cs
using UnityEngine;
using Core.ScriptableObjectSingleton;

namespace Game.GamePlay.Heroes
{
    [CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/Hero Config")]
    public class HeroConfig : ScriptableObjectSingleton<HeroConfig>
    {
        // Private serializable fields - configured in Unity Editor only
        [SerializeField] private int _initialHealth = 100;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private int _startingDamage = 10;

        // Public read-only accessors
        public int InitialHealth => _initialHealth;
        public float MoveSpeed => _moveSpeed;
        public int StartingDamage => _startingDamage;
    }
}
```

**Key points:**
- Inherit from `ScriptableObjectSingleton<T>` instead of `ScriptableObject`
- The singleton instance is automatically managed by the base class
- Access via `HeroConfig.Instance` (inherited from base class)
- No need to implement `OnEnable()` or manual Instance assignment

#### Using Configuration in Controllers
```csharp
// In HeroController or any class needing config
public class HeroController
{
    public async UniTask<bool> Initialize(/* ... */)
    {
        // READ configuration - allowed
        int initialHealth = HeroConfig.Instance.InitialHealth;
        float moveSpeed = HeroConfig.Instance.MoveSpeed;
        
        // Initialize with config values
        _currentState = new HeroState(Vector3.zero, initialHealth, 0f, Vector3.zero);
        
        return true;
    }

    // DON'T DO THIS - writing to config at runtime
    public void ChangeSpeed(float newSpeed)
    {
        // HeroConfig.Instance._moveSpeed = newSpeed;  // FORBIDDEN
        // Use local state or a runtime settings service instead
    }
}
```

#### Runtime Configuration Changes
If runtime configuration changes are needed:
1. **Use a separate Runtime Settings Service** (not ScriptableObject)
2. **Store configuration defaults** in ScriptableObjects
3. **Apply overrides** in the Runtime Settings service

**Example:**
```csharp
// For runtime settings that can change
public class GameplaySettingsService : IService
{
    private float _currentMoveSpeedMultiplier = 1f;
    
    public float GetEffectiveMoveSpeed()
    {
        // Combine config (read-only) with runtime multipliers
        return HeroConfig.Instance.MoveSpeed * _currentMoveSpeedMultiplier;
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        _currentMoveSpeedMultiplier = multiplier;
    }
}
```

#### ScriptableObject Asset Folder Structure
```
Assets/
├── Features/
│   ├── Entities/
│   │   ├── Scripts/
│   │   │   ├── Config/
│   │   │   │   ├── HeroConfig.cs
│   │   │   │   └── EnemiesConfig.cs
│   │   │   └── ...
│   │   └── Assets/
│   │       └── Config/
│   │           ├── HeroConfig.asset
│   │           └── EnemiesConfig.asset
│   │
│   ├── Weapons/
│   │   ├── Scripts/
│   │   │   ├── Config/
│   │   │   │   ├── WeaponsConfig.cs
│   │   │   │   └── WeaponConfig.cs
│   │   │   └── ...
│   │   └── Assets/
│   │       └── Config/
│   │           ├── WeaponsConfig.asset
│   │           └── [individual weapon configs]/
```

#### Best Practices for ScriptableObjects
- **Never null-check without logging**: If config is null at runtime, it's a setup error
- **Use `[ReadOnly]`** attribute in inspector for fields that shouldn't be edited at runtime
- **Validate data** in editor scripts or custom inspectors if needed
- **Document ranges** with `[Range]` or `[Min]` attributes for clarity
- **Group related settings** using `[System.Serializable]` nested structs when appropriate

**Example with validation and ranges:**
```csharp
[CreateAssetMenu(fileName = "EnemiesConfig", menuName = "Game/Enemies Config")]
public class EnemiesConfig : ScriptableObjectSingleton<EnemiesConfig>
{
    [SerializeField] [Range(0.1f, 10f)] private float _spawnInterval = 2f;
    [SerializeField] [Min(1)] private int _maxEnemies = 10;
    [SerializeField] [Min(1f)] private float _spawnRadius = 10f;

    public float SpawnInterval => _spawnInterval;
    public int MaxEnemies => _maxEnemies;
    public float SpawnRadius => _spawnRadius;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Validation in editor only
        if (_spawnInterval < 0.1f) _spawnInterval = 0.1f;
        if (_maxEnemies < 1) _maxEnemies = 1;
    }
#endif
}
```

## Coding Standards

### Naming Conventions
- **Private fields**: `_camelCase` (with underscore prefix)
- **Properties**: `PascalCase`
- **Methods**: `PascalCase`
- **Local variables**: `camelCase`
- **Constants**: `UPPER_SNAKE_CASE` (if used)
- **Events**: `OnAction` (e.g., `OnStateChanged`, `OnEnemySpawned`)

### Event Patterns
- Events follow the naming convention `On[Action]`
- Events are declared at the top of the class after fields
- Unsubscribe in `OnDestroy()` to prevent memory leaks
- Use `?.Invoke()` pattern for safe event invocation

**Example:**
```csharp
// Declaration
public event Action<HeroState> OnStateChanged;
public event Action<int, int> OnEnemyDamaged;

// Invocation
OnStateChanged?.Invoke(_currentState);
OnEnemyDamaged?.Invoke(enemyId, damage);

// Subscription in view
_controller.OnStateChanged += OnStateChanged;

// Unsubscription
_controller.OnStateChanged -= OnStateChanged;
```

### Async Patterns
- Use `UniTask` from the Cysharp library (not Task or Coroutines)
- Use `async UniTask` for async methods that need to return
- Use `async UniTaskVoid` for fire-and-forget async operations
- Always use `.Forget()` when calling `UniTaskVoid` methods
- Properly handle `CancellationToken` for long-running loops

**Example:**
```csharp
// In Initialize
UpdateLoop(_cancellationTokenSource.Token).Forget();

// Update loop
private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        // Update logic
        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
    }
}

// Clean reset
public async UniTask Reset()
{
    _cancellationTokenSource?.Cancel();
    _cancellationTokenSource?.Dispose();
    return UniTask.CompletedTask;
}
```

### State Immutability
- Use immutable structs for state representation
- Create new state instances on changes (don't mutate existing ones)
- Pass state changes via events, not property mutations
- Read-only properties provide access to state

**Anti-pattern:**
```csharp
// DON'T DO THIS
_currentState.Health -= damage;  // Mutation
```

**Correct pattern:**
```csharp
// DO THIS
int newHealth = Mathf.Max(0, _currentState.Health - damage);
_currentState = new HeroState(_currentState.Position, newHealth, _currentState.LastAttackTime, _currentState.AttackToPosition);
OnStateChanged?.Invoke(_currentState);
```

### Null Checks and Guards
- Use early returns and guard clauses
- Prefer explicit null checks over null-coalescing in guards
- Use `?.` operator for safe member access in non-critical paths

**Example:**
```csharp
public void TakeHit(int damage)
{
    if (_currentState.IsDead) return;  // Guard clause
    
    int newHealth = Mathf.Max(0, _currentState.Health - damage);
    // ... continue logic
}
```

### Comments
- Use comments to explain **why**, not **what**
- Avoid obvious comments
- Document complex algorithms or non-obvious design decisions
- Use `//` for single-line comments and implementation details
- Use `/* */` for block comments sparingly

**Example:**
```csharp
// Skip very recently spawned enemies to prevent attacking before their view spawns
if (Time.time - enemy.LastAttackTime < spawnGrace) continue;
```

## Integration Patterns

### How to Add a New Service

1. **Create the service class** in `Features/[Feature]/Scripts/`
2. **Implement `IService`** interface
3. **Declare dependencies** via `GetDependencies()`
4. **Implement `Initialize()`** to set up the service
5. **Implement `Reset()`** to clean up resources
6. **Access via `ServicesLocator`**:
   ```csharp
   var myService = ServicesLocator.Instance.GetService<MyService>();
   ```

The service will be automatically discovered and initialized in dependency order.

### How to Add Features to Project Context

When adding new classes or systems to this project, update this document with:

1. **Class/System Name**: Brief name of what you're adding
2. **Location**: Directory path where it lives
3. **Type**: Controller, Service, Model, View, etc.
4. **Dependencies**: What services/controllers it depends on
5. **Key APIs**: Public methods, events, and properties
6. **Example Usage**: How it's instantiated or used
7. **Notes**: Any special considerations or patterns it follows

**Prompt for adding to context:**
```
Add [ClassName] to the project context.
Location: [Path]
Type: [Controller/Service/Model/View]
Dependencies: [List dependencies]
Key APIs: [Key public interface]
Brief description of responsibility
```

## Testing and Debugging

### Event Subscription Checklist
- [ ] Subscribed in initialization/OnServicesInitialized
- [ ] Unsubscribed in OnDestroy
- [ ] Event names follow `On[Action]` convention
- [ ] Safe invocation with `?.Invoke()`

### State Management Checklist
- [ ] State is immutable struct
- [ ] New instances created on changes
- [ ] Events fire with state payload
- [ ] No direct property mutations
- [ ] Read-only public access to state

### Service Integration Checklist
- [ ] Implements IService interface
- [ ] Dependencies declared in GetDependencies()
- [ ] Initialize() returns success/failure
- [ ] Reset() cleans up resources
- [ ] Discovered and initialized automatically

## Common Patterns

### Controller Update Loop
```csharp
private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        if (!_shouldUpdate) 
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            continue;
        }
        
        // Update logic here
        
        await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
    }
}
```

### View State Subscription
```csharp
private void OnServicesInitialized()
{
    _controller = ServicesLocator.Instance.GetService<EntitiesService>().SomeController;
    _controller.OnStateChanged += OnStateChanged;
}

private void OnStateChanged(StateType state)
{
    // Update visuals
}

private void OnDestroy()
{
    if (_controller != null)
        _controller.OnStateChanged -= OnStateChanged;
}
```

## Performance Considerations
- Avoid creating new state instances unnecessarily
- Use object pooling for frequently spawned/destroyed objects
- Prefer early returns to reduce branching depth
- Use `IReadOnlyDictionary` for read-only collection exposure
- Cache frequently accessed service references

## References
- **UniTask Documentation**: [GitHub Cysharp](https://github.com/Cysharp/UniTask)
- **Service Locator Pattern**: Wikipedia and Gang of Four patterns
- **Dependency Injection**: Best practices and anti-patterns

---

**Document Version**: 1.0  
**Last Updated**: [Current Date]  
**Maintained by**: Project Team
