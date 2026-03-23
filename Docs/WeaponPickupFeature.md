# Weapon Pickup Feature - Implementation Plan

## Overview
Implement a weapon pickup system where enemies have a chance to drop weapon pickups on death. Players can collect pickups to change their current weapon. Pickups are managed centrally via a controller, use object pooling per prefab, and implement an `IPickupable` interface for extensibility.

## Architecture & Design Decisions

### Core Principles
- **Model-View Separation**: All logic in controllers/models, views update based on events
- **Pooling Strategy**: One pool per weapon pickup prefab (via Unity's ObjectPool)
- **Distance-Based Collection**: No physics/triggers, controller-based distance checks
- **Interface-Driven**: IPickupable interface for future pickup types
- **Event-Driven**: Events for "something happened" moments (not per-frame)

### Dependencies
- **EnemiesController**: Decides when to spawn pickups (on enemy death)
- **WeaponsService**: Owns and manages WeaponPickupController
- **GameEventService**: Broadcasts hero restart event for cleanup
- **HeroController**: Provides position updates via state changes (through EntitiesService)

---

## Data Flow & Architecture

### 1. Entity Models

#### WeaponPickupState
```csharp
// Location: Features/Weapons/Scripts/Models/WeaponPickupState.cs
public struct WeaponPickupState
{
    public int Id { get; }
    public Vector3 Position { get; }
    public WeaponConfig WeaponConfig { get; }
    public float SpawnTime { get; }  // Time.time when spawned
    
    public bool IsExpired(float currentTime, float lifetime) 
        => currentTime - SpawnTime >= lifetime;
    
    public WeaponPickupState(int id, Vector3 position, WeaponConfig weaponConfig, float spawnTime)
    {
        Id = id;
        Position = position;
        WeaponConfig = weaponConfig;
        SpawnTime = spawnTime;
    }
}
```

#### IPickupable Interface
```csharp
// Location: Features/Weapons/Scripts/Models/IPickupable.cs
public interface IPickupable
{
    /// <summary>
    /// Called when the pickup is collected by the player.
    /// Allows different pickup types to handle collection differently.
    /// </summary>
    void OnPickedUp();
}
```

**Note**: This interface is implemented by the **model state**, not the view. It allows the controller to handle different pickup types uniformly.

### 2. Controllers

#### WeaponPickupController
```csharp
// Location: Features/Weapons/Scripts/Controllers/WeaponPickupController.cs
// Responsibilities:
// - Manage all active weapon pickups
// - Track pickup state (position, weapon, lifetime)
// - Distance-based collection detection
// - Spawn/despawn pickups based on timer
// - Publish events when pickups appear/disappear/are collected
// - Handle cleanup on scene reset
```

**Key APIs:**
- `SpawnPickup(Vector3 position, WeaponConfig weaponConfig)` → int pickupId
- `SetHeroPosition(Vector3 position)` (called each frame by EntitiesService to provide hero position)
- `RemovePickup(int pickupId)` (despawn due to timeout)
- `ClearAllPickups()` (on hero death)

**Events:**
- `event Action<int, Vector3, WeaponConfig> OnPickupSpawned` (pickupId, position, weaponConfig)
- `event Action<int> OnPickupCollected` (pickupId)
- `event Action<int> OnPickupDespawned` (pickupId)

**Update Loop:**
- Check for expired pickups and despawn them
- Check distance between last known hero position and all pickups for collection
- Fire events as state changes
- **Note**: WeaponPickupController receives hero position from EntitiesService (does not directly reference HeroController)

### 3. View Layer

#### WeaponPickupContainerView
```csharp
// Location: Features/Weapons/Scripts/View/WeaponPickupContainerView.cs
// Responsibilities:
// - Subscribe to WeaponPickupController events
// - Manage pools for each weapon pickup prefab
// - Spawn/return visual instances from pools
// - Handle visual cleanup on despawn
// - Listen to hero death and clear all pickups
```

**Key Responsibilities:**
- Subscribe to `OnPickupSpawned` → instantiate from appropriate pool
- Subscribe to `OnPickupDespawned` → return to pool
- Subscribe to `OnPickupCollected` → return to pool AND trigger collection visuals/effects
- Listen to hero controller death and clear all visuals

#### WeaponPickupView
```csharp
// Location: Features/Weapons/Scripts/View/WeaponPickupView.cs
// Responsibilities:
// - Represent a single pickup visually
// - Handle rotating animation
// - Handle collection feedback/effects
// - Clean up on despawn (before returning to pool)
```

**Key APIs:**
- `Initialize(WeaponPickupState state)` - set position and show weapon model
- `OnPickedUp()` - trigger collection effect/feedback
- `Despawn()` - cleanup before returning to pool

### 4. Configuration

#### WeaponPickupConfig
```csharp
// Location: Features/Weapons/Scripts/Config/WeaponPickupConfig.cs
// Inherits: ScriptableObjectSingleton<WeaponPickupConfig>
//
// Configuration:
// - float PickupLifetime (seconds before despawn, e.g., 30)
// - float PickupCollectionDistance (how close to pick up, e.g., 2)
// - int PoolSizePerPrefab (objects per pool, e.g., 5)
// - Vector3 PickupPositionOffset (visual positioning adjustment, e.g., (0, 1, 0))
// - float PickupScale (visual scale multiplier, e.g., 1.5)
```

#### EnemiesConfig (extended)
```csharp
// Add to existing EnemiesConfig:
// - float WeaponDropChance (e.g., 0.3 for 30% chance)
// - List<WeaponPickupPrefabConfig> AvailablePickupPrefabs
//   (prefab reference, weapon config reference, or pool directly)
```

---

## Implementation Responsibilities

### Code Changes (Copilot - Will be created/modified)
- `Features/Weapons/Scripts/Models/WeaponPickupState.cs` (create)
- `Features/Weapons/Scripts/Models/IPickupable.cs` (create)
- `Features/Weapons/Scripts/Config/WeaponPickupConfig.cs` (create)
- `Features/Weapons/Scripts/Controllers/WeaponPickupController.cs` (create)
- `Features/Weapons/Scripts/View/WeaponPickupContainerView.cs` (create)
- `Features/Weapons/Scripts/View/WeaponPickupView.cs` (create)
- `Features/Entities/Scripts/Controllers/EnemiesController.cs` (modify)
- `Features/Entities/Scripts/Config/EnemiesConfig.cs` (modify)
- `Features/Weapons/Scripts/WeaponsService.cs` (modify)
- `Features/Entities/Scripts/EntitiesService.cs` (modify)

### Manual Setup (You - Unity Editor)

1. **Create WeaponPickupConfig Asset**
   - Create `Assets/Features/Weapons/Assets/Config/WeaponPickupConfig.asset`
   - In inspector, set values (will have reasonable defaults):
     - Pickup Lifetime: 30 seconds
     - Pickup Collection Distance: 2 units
     - Pool Size Per Prefab: 5

2. **Update EnemiesConfig Asset**
   - Open existing `EnemiesConfig.asset`
   - Set new property: Weapon Drop Chance = 0.3 (for 30%)

3. **Create Weapon Pickup Prefabs**
   - Create prefabs in `Assets/Features/Weapons/Prefabs/Pickups/`
   - Each prefab should have:
     - Root (Pivot at 0,0,0)
       - VisualModel (mesh/model, will rotate)
       - (Optional) EffectMarker (for future effects)
   - Add `WeaponPickupView` component to root
   - Disable any colliders (distance-based system only)
   - Do NOT assign weapon config to view (controller will pass it dynamically)

4. **Scene Setup**
   - Create empty GameObject named `WeaponPickupsContainer`
   - Add `WeaponPickupContainerView` component to it
   - In inspector, assign all weapon pickup prefabs you created to the prefabs list
   - This view will manage pooling and visual instances

---

## Event Summary

### WeaponPickupController Events
- `OnPickupSpawned(pickupId, position, weaponConfig)` - New pickup appeared
- `OnPickupCollected(pickupId)` - Pickup was picked up by player
- `OnPickupDespawned(pickupId)` - Pickup expired/was destroyed

### EnemiesController Event
- `OnWeaponDropped(position, weaponConfig)` - Enemy died and dropped a weapon

### Integration Points
- **EnemiesController** → **EntitiesService**: Fires `OnWeaponDropped` event
- **EntitiesService** → **WeaponsService**: Calls `SpawnPickup()` on WeaponPickupController and provides hero position updates
- **WeaponPickupController** → **WeaponPickupContainerView**: Events for spawn/collect/despawn
- **WeaponPickupController** → **WeaponsService**: On collect, calls `EquipWeapon()`
- **GameEventService** → **WeaponPickupController**: Broadcasts hero restart → clears pickups
- **EntitiesService** → **GameEventService**: Triggers hero restart event

---

## Pooling Strategy

### ObjectPool Setup
```csharp
// In WeaponPickupContainerView.OnServicesInitialized():
foreach (var pickupPrefab in pickupPrefabs)
{
    var pool = new ObjectPool<WeaponPickupView>(
        createFunc: () => Instantiate(pickupPrefab, transform),
        actionOnGet: view => view.gameObject.SetActive(true),
        actionOnRelease: view => view.gameObject.SetActive(false),
        actionOnDestroy: view => Destroy(view.gameObject),
        collectionCheck: false,
        defaultCapacity: 1,  // Start with small capacity
        maxSize: WeaponPickupConfig.Instance.PoolSizePerPrefab
    );
    _pickupPools[pickupPrefab] = pool;
}
```

### Pool Usage
- On `OnPickupSpawned`: Get from pool, initialize, show
- On `OnPickupDespawned`: Return to pool, hide
- On `OnPickupCollected`: Return to pool, hide

---

## Extensibility Notes

### IPickupable Interface
The `IPickupable` interface is designed for future extensibility:
- Health pickups
- Power-up pickups
- Ammunition pickups
- Any collectible that has distance-based pickup behavior

### Future Enhancements
1. **Weapon Rarity/Weighting**: Modify `SelectWeaponToDrop()` to support weighted random
2. **Enemy-Specific Drops**: Map enemy types to weapon drop tables
3. **Pickup Particle Effects**: Call setup in `WeaponPickupView.Initialize()`
4. **Collection Feedback**: Add sounds/visuals in `WeaponPickupView.PickUp()`
5. **Glow Intensity Timer**: Fade glow as despawn approaches (visual timer in view)
6. **Inventory System**: Replace immediate swap with inventory management

---

## Testing Checklist

- [ ] Pickups spawn with correct position and weapon config
- [ ] Pickups despawn after lifetime expires
- [ ] Pickups are collected when hero is within distance
- [ ] Weapon changes on collection (model swaps correctly)
- [ ] Old weapon model is destroyed (no memory leak)
- [ ] Pickups clear on hero death
- [ ] **Weapon resets to default on hero death** (edge case)
- [ ] Multiple pickups can exist simultaneously (up to pool size)
- [ ] Pool reuse works correctly (objects reactivate properly)
- [ ] Events fire at correct times
- [ ] No physics/trigger conflicts (distance-based only)
- [ ] Random weapon selection excludes current weapon
- [ ] Pickup position offset is applied correctly
- [ ] Pickup scale is applied correctly

---

## Edge Cases & Special Handling

### Hero Death & Weapon Reset
When the hero dies and restarts:
1. All pickups are cleared from the world
2. Current weapon is reset to the default weapon (first in WeaponsConfig)
3. Weapon change event is fired to update views
4. This prevents the player from keeping an unintended weapon after restart

**Implementation:** WeaponsService subscribes to `GameEventService.OnHeroRestarted` and resets `CurrentWeapon` to default.

### Pickup Visual Adjustment
Pickups can be visually adjusted via WeaponPickupConfig:
- **PickupPositionOffset**: Applied to spawn position to adjust height/depth (e.g., lift pickups off ground)
- **PickupScale**: Scales pickup visuals uniformly (e.g., make small weapons more visible)

**Implementation:** WeaponPickupView applies these in `Initialize()` method.

### Scripts to Create
- [ ] `Features/Weapons/Scripts/Models/WeaponPickupState.cs`
- [ ] `Features/Weapons/Scripts/Models/IPickupable.cs`
- [ ] `Features/Weapons/Scripts/Config/WeaponPickupConfig.cs`
- [ ] `Features/Weapons/Scripts/Controllers/WeaponPickupController.cs`
- [ ] `Features/Weapons/Scripts/View/WeaponPickupContainerView.cs`
- [ ] `Features/Weapons/Scripts/View/WeaponPickupView.cs`

### Prefabs to Create
- [ ] `Assets/Features/Weapons/Prefabs/Pickups/[WeaponName]Pickup.prefab` (one per weapon, or generic)

### ScriptableObject Assets to Create
- [ ] `Assets/Features/Weapons/Assets/Config/WeaponPickupConfig.asset`

### Scripts to Modify
- [ ] `Features/Entities/Scripts/Controllers/EnemiesController.cs` (add weapon drop logic & event)
- [ ] `Features/Entities/Scripts/Config/EnemiesConfig.cs` (add weapon drop chance)
- [ ] `Features/Weapons/Scripts/WeaponsService.cs` (add `GetAvailableWeaponsExcept()` method)
- [ ] `Features/Entities/Scripts/EntitiesService.cs` (initialize WeaponPickupController, optional)

---

## Expected Behavior Summary

1. **Enemy Death**: EnemiesController checks drop chance, randomly selects non-current weapon, fires `OnWeaponDropped`
2. **Pickup Spawn**: EntitiesService receives `OnWeaponDropped` → calls `WeaponsService.WeaponPickupController.SpawnPickup()`
3. **Pickup Visuals**: WeaponPickupContainerView instantiates from pool, rotates model
4. **Hero Position Update**: EntitiesService provides hero position via `HeroController.OnStateChanged` → calls `SetHeroPosition()`
5. **Pickup Lifetime**: Despawns after timer expires, fires `OnPickupDespawned`
6. **Collection**: WeaponPickupController detects distance, fires `OnPickupCollected`
7. **Weapon Switch**: WeaponPickupController calls `WeaponsService.EquipWeapon()`, HeroView spawns new model
8. **Cleanup**: Hero restart → EntitiesService triggers `GameEventService.TriggerHeroRestart()` → WeaponsService clears pickups

---

**Document Version**: 1.0  
**Status**: Ready for Implementation
