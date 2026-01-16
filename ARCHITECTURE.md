# Space Factory - Technical Architecture

## Overview

This document describes the technical architecture for Space Factory, built in Godot 4.5 using **C#**.

> **Note:** The codebase was converted from GDScript to C# for performance reasons. All scripts are now in `scripts/csharp/`.

## Implementation Status (Phase 2 Complete)

### Implemented Files - Core (C#)
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/GameManager.cs` | Complete | Game state, ticks, pause |
| `scripts/csharp/GridManager.cs` | Complete | Grid coords, foundation tracking |
| `scripts/csharp/SpriteGenerator.cs` | Complete | All procedural sprite generation |
| `scripts/csharp/Enums.cs` | Complete | All game enumerations |
| `scripts/csharp/Constants.cs` | Complete | Game constants and colors |
| `scripts/csharp/ItemStack.cs` | Complete | Stack data class |

### Implemented Files - Systems (Autoload Singletons)
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/InventoryManager.cs` | Complete | Full inventory with item registry |
| `scripts/csharp/DebrisManager.cs` | Complete | Spawning, drifting, collection |
| `scripts/csharp/CraftingManager.cs` | Complete | Recipe registry, hand-craft queue |
| `scripts/csharp/ResearchManager.cs` | Complete | Tech tree, research progress |
| `scripts/csharp/PowerManager.cs` | Complete | Power network simulation |
| `scripts/csharp/BuildingManager.cs` | Complete | Building registry, placement, removal |

### Implemented Files - Entities
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/BuildingEntity.cs` | Complete | Base class for all buildings |
| `scripts/csharp/DebrisEntity.cs` | Complete | Debris with drift movement |
| `scripts/csharp/StoneFurnace.cs` | Complete | 2x2 furnace with fuel/input/output |
| `scripts/csharp/SmallChest.cs` | Complete | 1x1 storage with 16 slots |
| `scripts/csharp/ConveyorBelt.cs` | Complete | 1x1 belt with item transport |
| `scripts/csharp/Inserter.cs` | Complete | 1x1 item transfer with swing |

### Implemented Files - UI
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/HUD.cs` | Complete | Hotbar, resource display |
| `scripts/csharp/InventoryUI.cs` | Complete | 40-slot inventory grid |
| `scripts/csharp/BuildMenuUI.cs` | Complete | Building selection by category |
| `scripts/csharp/BuildingUI.cs` | Complete | Building interaction panel |

### Implemented Files - Resources
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/ItemResource.cs` | Complete | Item resource class |
| `scripts/csharp/RecipeResource.cs` | Complete | Recipe resource class |
| `scripts/csharp/BuildingResource.cs` | Complete | Building resource class |
| `scripts/csharp/TechnologyResource.cs` | Complete | Technology resource class |

### Implemented Files - Game
| File | Status | Notes |
|------|--------|-------|
| `scripts/csharp/Main.cs` | Complete | Main scene controller |
| `scenes/game/Main.tscn` | Complete | Main game scene |

### Not Yet Implemented (Phase 3+)
| File | Phase | Notes |
|------|-------|-------|
| `scripts/csharp/SaveManager.cs` | Phase 5 | Save/load system |
| `scripts/csharp/CraftingUI.cs` | Deferred | Hand-craft UI panel |
| `scripts/csharp/ResearchUI.cs` | Phase 3 | Tech tree UI |
| `scripts/csharp/Assembler.cs` | Phase 3 | Multi-ingredient crafting |
| `scripts/csharp/Lab.cs` | Phase 3 | Science pack consumer |
| `scripts/csharp/DebrisCollector.cs` | Phase 3 | Auto debris collection |

---

## Architecture Principles

1. **Data-Driven Design**: Items, recipes, and buildings defined as Resources
2. **Component-Based Entities**: Buildings composed of reusable components
3. **Signal-Based Communication**: Loose coupling via C# delegates/events
4. **Singleton Managers**: Global systems accessible via autoload with static Instance
5. **Separation of Concerns**: Logic, data, and presentation separated

---

## Core Systems

### 1. Game Manager (`GameManager.cs`)
**Autoload Singleton**

Responsibilities:
- Game state management (playing, paused, menu)
- Save/load coordination
- Global game settings
- Time management (game tick rate at 60 ticks/sec)

```csharp
// Signals (C# delegates)
[Signal] public delegate void GameTickEventHandler(int tick);
[Signal] public delegate void GameStateChangedEventHandler(Enums.GameState newState, Enums.GameState oldState);
[Signal] public delegate void GamePausedEventHandler();
[Signal] public delegate void GameResumedEventHandler();

// Properties
public static GameManager Instance { get; private set; }
public float GameSpeed { get; set; } = 1.0f;
public bool IsPaused { get; private set; } = false;
public int CurrentTick { get; private set; } = 0;
```

### 2. Grid System (`GridManager.cs`)
**Autoload Singleton**

Responsibilities:
- Track station tiles (foundation positions)
- Building placement validation
- Building position lookup
- Grid coordinate conversion

```csharp
// Signals
[Signal] public delegate void FoundationAddedEventHandler(Vector2I pos);
[Signal] public delegate void FoundationRemovedEventHandler(Vector2I pos);
[Signal] public delegate void BuildingPlacedEventHandler(Vector2I pos, Node2D building);
[Signal] public delegate void BuildingRemovedEventHandler(Vector2I pos);

// Methods
public bool CanPlaceBuilding(Vector2I pos, BuildingResource building)
public bool HasFoundation(Vector2I pos)
public bool HasBuilding(Vector2I pos)
public Node2D GetBuilding(Vector2I pos)
public Vector2I WorldToGrid(Vector2 worldPos)
public Vector2 GridToWorld(Vector2I gridPos)
```

### 3. Inventory System (`InventoryManager.cs`)
**Autoload Singleton**

Responsibilities:
- Player inventory management
- Item registry
- Stack handling
- Item transfer between inventories

```csharp
// Signals
[Signal] public delegate void InventoryChangedEventHandler();
[Signal] public delegate void HotbarChangedEventHandler();
[Signal] public delegate void ItemAddedEventHandler(ItemResource item, int count, int slotIndex);
[Signal] public delegate void ItemRemovedEventHandler(ItemResource item, int count, int slotIndex);

// Properties
public Array<ItemStack> Inventory { get; private set; }
public Array<ItemStack> Hotbar { get; private set; }

// Methods
public int AddItem(ItemResource item, int count = 1)  // Returns overflow
public bool RemoveItem(ItemResource item, int count = 1)
public bool HasItem(ItemResource item, int count = 1)
public int GetItemCount(ItemResource item)
public ItemResource GetItem(string itemId)
```

### 4. Crafting System (`CraftingManager.cs`)
**Autoload Singleton**

Responsibilities:
- Recipe validation
- Crafting execution
- Queue management

```csharp
// Signals
[Signal] public delegate void CraftStartedEventHandler(RecipeResource recipe);
[Signal] public delegate void CraftProgressChangedEventHandler(RecipeResource recipe, float progress);
[Signal] public delegate void CraftCompletedEventHandler(RecipeResource recipe);
[Signal] public delegate void QueueChangedEventHandler();

// Methods
public bool CanCraft(RecipeResource recipe)
public bool QueueCraft(RecipeResource recipe, int count = 1)
public RecipeResource GetRecipe(string recipeId)
public Array<RecipeResource> GetAllRecipes()
```

### 5. Research System (`ResearchManager.cs`)
**Autoload Singleton**

Responsibilities:
- Tech tree state
- Research progress tracking
- Unlocks management

```csharp
// Signals
[Signal] public delegate void ResearchStartedEventHandler(TechnologyResource tech);
[Signal] public delegate void ResearchProgressEventHandler(TechnologyResource tech, float progress);
[Signal] public delegate void ResearchCompletedEventHandler(TechnologyResource tech);
[Signal] public delegate void TechnologyUnlockedEventHandler(TechnologyResource tech);

// Properties
public TechnologyResource CurrentResearch { get; private set; }
public float ResearchProgress { get; private set; }

// Methods
public bool StartResearch(TechnologyResource tech)
public bool IsTechnologyUnlocked(string techId)
public Array<TechnologyResource> GetAvailableResearch()
```

### 6. Debris System (`DebrisManager.cs`)
**Autoload Singleton**

Responsibilities:
- Spawn debris at edges of screen
- Manage debris movement
- Handle debris collection
- Despawn off-screen debris

```csharp
// Signals
[Signal] public delegate void DebrisSpawnedEventHandler(DebrisEntity debris);
[Signal] public delegate void DebrisCollectedEventHandler(DebrisEntity debris);

// Methods
public void SetDebrisContainer(Node2D container)
public void SpawnDebris()
```

### 7. Power System (`PowerManager.cs`)
**Autoload Singleton**

Responsibilities:
- Track power production/consumption
- Manage power network
- Handle brownouts

```csharp
// Signals
[Signal] public delegate void PowerChangedEventHandler(float production, float consumption);
[Signal] public delegate void BrownoutStartedEventHandler();
[Signal] public delegate void BrownoutEndedEventHandler();

// Properties
public float TotalProduction { get; private set; }
public float TotalConsumption { get; private set; }
public float Satisfaction { get; private set; }  // 0.0 to 1.0

// Methods
public void RegisterProducer(BuildingEntity building)
public void UnregisterProducer(BuildingEntity building)
public void RegisterConsumer(BuildingEntity building)
public void UnregisterConsumer(BuildingEntity building)
```

### 8. Building Manager (`BuildingManager.cs`)
**Autoload Singleton**

Responsibilities:
- Building registry
- Build mode and ghost preview
- Placement validation and execution
- Building removal

```csharp
// Signals
[Signal] public delegate void BuildModeChangedEventHandler(bool enabled, BuildingResource building);
[Signal] public delegate void BuildingPlacedEventHandler(Vector2I pos, Node2D building);
[Signal] public delegate void BuildingRemovedEventHandler(Vector2I pos);

// Properties
public Node2D BuildingsLayer { get; set; }

// Methods
public void EnterBuildMode(BuildingResource building)
public void ExitBuildMode()
public bool IsInBuildMode()
public void RotatePlacementCw()
public void UpdateGhostPosition(Vector2I gridPos)
public bool TryPlaceBuilding(Vector2I gridPos)
public void RemoveBuilding(Vector2I gridPos)
public BuildingResource GetBuilding(string buildingId)
public Array<BuildingResource> GetBuildingsByCategory(Enums.BuildingCategory category)
```

---

## Resource Definitions

### ItemResource (`ItemResource.cs`)
```csharp
public partial class ItemResource : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public int StackSize { get; set; } = 100;
    [Export] public Enums.ItemCategory Category { get; set; }
    [Export] public Color SpriteColor { get; set; } = Colors.White;
    [Export] public bool IsFluid { get; set; } = false;
}
```

### RecipeResource (`RecipeResource.cs`)
```csharp
public partial class RecipeResource : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string[] IngredientIds { get; set; }
    [Export] public int[] IngredientCounts { get; set; }
    [Export] public string[] ResultIds { get; set; }
    [Export] public int[] ResultCounts { get; set; }
    [Export] public float CraftingTime { get; set; } = 1.0f;
    [Export] public Enums.CraftingType CraftingType { get; set; }
    [Export] public string RequiredTechnology { get; set; }
}
```

### BuildingResource (`BuildingResource.cs`)
```csharp
public partial class BuildingResource : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public Vector2I Size { get; set; } = Vector2I.One;
    [Export] public float PowerConsumption { get; set; } = 0.0f;
    [Export] public float PowerProduction { get; set; } = 0.0f;
    [Export] public Enums.BuildingCategory Category { get; set; }
    [Export] public string RequiredTechnology { get; set; }
    [Export] public string[] BuildCostItemIds { get; set; }
    [Export] public int[] BuildCostItemCounts { get; set; }
}
```

### TechnologyResource (`TechnologyResource.cs`)
```csharp
public partial class TechnologyResource : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string[] PrerequisiteIds { get; set; }
    [Export] public string[] SciencePackIds { get; set; }
    [Export] public int[] SciencePackCounts { get; set; }
    [Export] public string[] UnlocksRecipeIds { get; set; }
    [Export] public string[] UnlocksBuildingIds { get; set; }
}
```

---

## Entity System

### Base Classes

#### BuildingEntity (`BuildingEntity.cs`)
```csharp
public partial class BuildingEntity : Node2D
{
    public BuildingResource Definition { get; protected set; }
    public Vector2I GridPosition { get; protected set; }
    public int RotationIndex { get; protected set; } = 0;  // 0-3 for N, E, S, W
    public bool IsPowered { get; protected set; } = true;
    public Array<ItemStack> InternalInventory { get; protected set; } = new();

    // Virtual methods for subclasses
    public virtual bool CanAcceptItem(ItemResource item, Enums.Direction fromDirection)
    public virtual bool InsertItem(ItemResource item, int count, Enums.Direction fromDirection)
    public virtual ItemResource HasOutputItem(Enums.Direction toDirection)
    public virtual ItemResource ExtractItem(Enums.Direction toDirection)
    protected virtual void ProcessBuilding()  // Called each game tick
}
```

#### DebrisEntity (`DebrisEntity.cs`)
```csharp
public partial class DebrisEntity : Area2D
{
    public string DebrisType { get; private set; }
    public Array Contents { get; private set; }
    public Vector2 DriftVelocity { get; private set; }
    public bool IsCollectible { get; set; } = true;

    public void Initialize(string type, Array contents, int seed)
    public Array Collect()
}
```

### Building Types

- **StoneFurnace** - 2x2 smelter with fuel, input, and output slots
- **SmallChest** - 1x1 storage with 16 inventory slots
- **ConveyorBelt** - 1x1 item transport, auto-connects to adjacent belts
- **Inserter** - 1x1 item transfer with swing animation

---

## Scene Structure

### Main Scene Tree
```
Main (Node2D) - Main.cs
├── Background (ParallaxBackground)
├── GameWorld (Node2D)
│   ├── DebrisLayer (Node2D)
│   │   └── [DebrisEntity instances]
│   └── StationLayer (Node2D)
│       ├── FoundationTiles (Node2D)
│       └── Buildings (Node2D)
│           └── [BuildingEntity instances]
├── Camera2D
├── HUD (CanvasLayer) - HUD.cs
│   ├── HotbarPanel
│   ├── ResourcePanel
│   ├── CraftingProgress
│   └── Tooltip
├── InventoryUI (CanvasLayer) - InventoryUI.cs
├── BuildMenuUI (CanvasLayer) - BuildMenuUI.cs
└── BuildingUI (CanvasLayer) - BuildingUI.cs
```

---

## C# Patterns Used

### Singleton Pattern
All managers use a static `Instance` property set in `_EnterTree()`:
```csharp
public static GameManager Instance { get; private set; }

public override void _EnterTree()
{
    Instance = this;
}
```

### Node References in C#
Since C# exports with NodePath don't auto-resolve, nodes are fetched in `_Ready()`:
```csharp
public override void _Ready()
{
    // Fetch node references manually
    Camera ??= GetNodeOrNull<Camera2D>("Camera2D");
    Hud ??= GetNodeOrNull<HUD>("HUD");
    // etc.
}
```

### Signal Definitions
C# signals use delegate naming convention with `EventHandler` suffix:
```csharp
[Signal]
public delegate void GameTickEventHandler(int tick);

// Emit with:
EmitSignal(SignalName.GameTick, CurrentTick);

// Connect with:
GameManager.Instance.GameTick += OnGameTick;
```

---

## Input Handling

### Input Actions (defined in project.godot)
- `click` -> Left Mouse Button
- `right_click` -> Right Mouse Button
- `inventory` -> I
- `interact` -> E
- `rotate` -> R
- `cancel` -> Escape
- `move_up/down/left/right` -> WASD / Arrows
- `zoom_in/out` -> Mouse Wheel
- `build_menu` -> B

---

## File Structure

```
scripts/csharp/
├── Enums.cs                    ✓ Game enumerations
├── Constants.cs                ✓ Game constants
├── ItemStack.cs                ✓ Stack data class
├── ItemResource.cs             ✓ Item resource
├── RecipeResource.cs           ✓ Recipe resource
├── BuildingResource.cs         ✓ Building resource
├── TechnologyResource.cs       ✓ Technology resource
├── GameManager.cs              ✓ Game state singleton
├── GridManager.cs              ✓ Grid management singleton
├── SpriteGenerator.cs          ✓ Procedural sprites singleton
├── InventoryManager.cs         ✓ Inventory singleton
├── CraftingManager.cs          ✓ Crafting singleton
├── ResearchManager.cs          ✓ Research singleton
├── DebrisManager.cs            ✓ Debris singleton
├── PowerManager.cs             ✓ Power singleton
├── BuildingManager.cs          ✓ Building placement singleton
├── BuildingEntity.cs           ✓ Building base class
├── DebrisEntity.cs             ✓ Debris entity
├── StoneFurnace.cs             ✓ Furnace building
├── SmallChest.cs               ✓ Chest building
├── ConveyorBelt.cs             ✓ Belt building
├── Inserter.cs                 ✓ Inserter building
├── HUD.cs                      ✓ HUD UI
├── InventoryUI.cs              ✓ Inventory UI
├── BuildMenuUI.cs              ✓ Build menu UI
├── BuildingUI.cs               ✓ Building interaction UI
└── Main.cs                     ✓ Main scene controller

scenes/
└── game/
    └── Main.tscn               ✓ Main game scene

SpaceFactory.csproj             ✓ C# project file
SpaceFactory.sln                ✓ Solution file
```

## Autoload Singletons (project.godot)

The following C# singletons are configured and load automatically:
- `GameManager` - Game state and tick system
- `GridManager` - Station grid management
- `SpriteGenerator` - Procedural sprite generation
- `InventoryManager` - Player inventory
- `CraftingManager` - Recipes and crafting
- `DebrisManager` - Debris spawning/collection
- `ResearchManager` - Tech tree
- `PowerManager` - Power network
- `BuildingManager` - Building placement and registry

---

## Performance Considerations

### Optimization Strategies

1. **Object Pooling**: Reuse debris and item entities
2. **Chunk-Based Updates**: Only update visible/nearby buildings
3. **Batched Rendering**: Use tilemap for belts, batch draw calls
4. **LOD for Debris**: Simplified rendering for distant debris
5. **Tick-Based Logic**: Process buildings on game ticks (60/sec), not every frame

### Target Performance
- 60 FPS with 500+ buildings
- 100+ active debris entities
- Smooth scrolling and zooming
