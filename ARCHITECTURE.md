# Space Factory - Technical Architecture

## Overview

This document describes the technical architecture for Space Factory, built in Godot 4.5 using GDScript.

## Implementation Status (Phase 2 Complete)

### Implemented Files - Core
| File | Status | Notes |
|------|--------|-------|
| `scripts/core/GameManager.gd` | Complete | Game state, ticks, pause |
| `scripts/core/GridManager.gd` | Complete | Grid coords, foundation tracking |
| `scripts/core/SpriteGenerator.gd` | Complete | All procedural sprite generation |
| `scripts/data/Enums.gd` | Complete | All game enumerations |
| `scripts/data/Constants.gd` | Complete | Game constants and colors |
| `scripts/data/ItemStack.gd` | Complete | Stack data class |

### Implemented Files - Systems (Autoload Singletons)
| File | Status | Notes |
|------|--------|-------|
| `scripts/systems/InventoryManager.gd` | Complete | Full inventory with item registry |
| `scripts/systems/DebrisManager.gd` | Complete | Spawning, drifting, collection |
| `scripts/systems/CraftingManager.gd` | Complete | Recipe registry, hand-craft queue |
| `scripts/systems/ResearchManager.gd` | Complete | Tech tree, research progress |
| `scripts/systems/PowerManager.gd` | Complete | Power network simulation |
| `scripts/systems/BuildingManager.gd` | Complete | Building registry, placement, removal |

### Implemented Files - Entities
| File | Status | Notes |
|------|--------|-------|
| `scripts/entities/BuildingEntity.gd` | Complete | Base class for all buildings |
| `scripts/entities/DebrisEntity.gd` | Complete | Debris with drift movement |
| `scripts/entities/StoneFurnace.gd` | Complete | 2x2 furnace with fuel/input/output |
| `scripts/entities/SmallChest.gd` | Complete | 1x1 storage with 16 slots |
| `scripts/entities/ConveyorBelt.gd` | Complete | 1x1 belt with item transport |
| `scripts/entities/Inserter.gd` | Complete | 1x1 item transfer with swing |

### Implemented Files - UI
| File | Status | Notes |
|------|--------|-------|
| `scripts/ui/HUD.gd` | Complete | Hotbar, resource display |
| `scripts/ui/InventoryUI.gd` | Complete | 40-slot inventory grid |
| `scripts/ui/BuildMenuUI.gd` | Complete | Building selection by category |

### Implemented Files - Resources
| File | Status | Notes |
|------|--------|-------|
| `resources/items/ItemResource.gd` | Complete | Item resource class |
| `resources/recipes/RecipeResource.gd` | Complete | Recipe resource class |
| `resources/buildings/BuildingResource.gd` | Complete | Building resource class |
| `resources/research/TechnologyResource.gd` | Complete | Technology resource class |

### Implemented Files - Game
| File | Status | Notes |
|------|--------|-------|
| `scripts/game/Main.gd` | Complete | Main scene controller |
| `scenes/game/Main.tscn` | Complete | Main game scene |

### Not Yet Implemented (Phase 3+)
| File | Phase | Notes |
|------|-------|-------|
| `scripts/core/SaveManager.gd` | Phase 5 | Save/load system |
| `scripts/ui/CraftingUI.gd` | Deferred | Hand-craft UI panel |
| `scripts/ui/ResearchUI.gd` | Phase 3 | Tech tree UI |
| `scripts/entities/Assembler.gd` | Phase 3 | Multi-ingredient crafting |
| `scripts/entities/Lab.gd` | Phase 3 | Science pack consumer |
| `scripts/entities/DebrisCollector.gd` | Phase 3 | Auto debris collection |

---

## Architecture Principles

1. **Data-Driven Design**: Items, recipes, and buildings defined as Resources
2. **Component-Based Entities**: Buildings composed of reusable components
3. **Signal-Based Communication**: Loose coupling via Godot signals
4. **Singleton Managers**: Global systems accessible via autoload
5. **Separation of Concerns**: Logic, data, and presentation separated

---

## Core Systems

### 1. Game Manager (`GameManager.gd`)
**Autoload Singleton**

Responsibilities:
- Game state management (playing, paused, menu)
- Save/load coordination
- Global game settings
- Time management (game tick rate)

```gdscript
# Signals
signal game_paused
signal game_resumed
signal game_saved
signal game_loaded

# Properties
var game_speed: float = 1.0
var is_paused: bool = false
var current_tick: int = 0
```

### 2. Grid System (`GridManager.gd`)
**Autoload Singleton**

Responsibilities:
- Track station tiles (foundation positions)
- Building placement validation
- Building position lookup
- Grid coordinate conversion

```gdscript
# Constants
const TILE_SIZE = 32

# Data
var foundation_tiles: Dictionary = {}  # Vector2i -> bool
var buildings: Dictionary = {}  # Vector2i -> BuildingEntity

# Methods
func can_place_building(pos: Vector2i, building_def: BuildingResource) -> bool
func place_building(pos: Vector2i, building_def: BuildingResource) -> BuildingEntity
func remove_building(pos: Vector2i) -> void
func world_to_grid(world_pos: Vector2) -> Vector2i
func grid_to_world(grid_pos: Vector2i) -> Vector2
```

### 3. Inventory System (`InventoryManager.gd`)
**Autoload Singleton**

Responsibilities:
- Player inventory management
- Stack handling
- Item transfer between inventories

```gdscript
# Signals
signal inventory_changed
signal item_added(item: ItemResource, count: int)
signal item_removed(item: ItemResource, count: int)

# Data Structure
class InventorySlot:
    var item: ItemResource
    var count: int

var player_inventory: Array[InventorySlot]
var hotbar: Array[InventorySlot]

# Methods
func add_item(item: ItemResource, count: int = 1) -> int  # Returns overflow
func remove_item(item: ItemResource, count: int = 1) -> bool
func has_item(item: ItemResource, count: int = 1) -> bool
func get_item_count(item: ItemResource) -> int
```

### 4. Crafting System (`CraftingManager.gd`)
**Autoload Singleton**

Responsibilities:
- Recipe validation
- Crafting execution
- Queue management

```gdscript
# Signals
signal craft_started(recipe: RecipeResource)
signal craft_completed(recipe: RecipeResource)

# Methods
func can_craft(recipe: RecipeResource) -> bool
func craft(recipe: RecipeResource) -> bool
func get_available_recipes() -> Array[RecipeResource]
```

### 5. Research System (`ResearchManager.gd`)
**Autoload Singleton**

Responsibilities:
- Tech tree state
- Research progress tracking
- Unlocks management

```gdscript
# Signals
signal research_started(tech: TechnologyResource)
signal research_progress(tech: TechnologyResource, progress: float)
signal research_completed(tech: TechnologyResource)
signal technology_unlocked(tech: TechnologyResource)

# Data
var unlocked_technologies: Array[TechnologyResource]
var current_research: TechnologyResource
var research_progress: float

# Methods
func start_research(tech: TechnologyResource) -> bool
func add_science(pack_type: String, count: int) -> void
func is_unlocked(tech: TechnologyResource) -> bool
func get_available_research() -> Array[TechnologyResource]
```

### 6. Debris System (`DebrisManager.gd`)
**Autoload Singleton**

Responsibilities:
- Spawn debris at edges of screen
- Manage debris movement
- Handle debris collection
- Despawn off-screen debris

```gdscript
# Signals
signal debris_spawned(debris: DebrisEntity)
signal debris_collected(debris: DebrisEntity, collector: Node)
signal debris_despawned(debris: DebrisEntity)

# Configuration
var spawn_rate: float = 2.5  # seconds between spawns
var spawn_variance: float = 1.0
var drift_speed_range: Vector2 = Vector2(20, 60)

# Methods
func spawn_debris() -> DebrisEntity
func collect_debris(debris: DebrisEntity) -> Array[ItemStack]
```

### 7. Power System (`PowerManager.gd`)
**Autoload Singleton**

Responsibilities:
- Track power production/consumption
- Manage power network
- Handle brownouts

```gdscript
# Signals
signal power_changed(production: float, consumption: float)
signal brownout_started
signal brownout_ended

# Properties
var total_production: float
var total_consumption: float
var satisfaction: float  # 0.0 to 1.0

# Methods
func register_producer(building: BuildingEntity, output: float) -> void
func register_consumer(building: BuildingEntity, input: float) -> void
func get_power_satisfaction() -> float
```

---

## Resource Definitions

### ItemResource (`resources/items/ItemResource.gd`)
```gdscript
class_name ItemResource
extends Resource

@export var id: String
@export var name: String
@export var description: String
@export var icon: Texture2D
@export var stack_size: int = 100
@export var category: ItemCategory

enum ItemCategory {
    RAW_MATERIAL,
    PROCESSED,
    COMPONENT,
    BUILDING,
    SCIENCE,
    FLUID
}
```

### RecipeResource (`resources/recipes/RecipeResource.gd`)
```gdscript
class_name RecipeResource
extends Resource

@export var id: String
@export var name: String
@export var ingredients: Array[ItemStack]
@export var results: Array[ItemStack]
@export var crafting_time: float  # seconds
@export var category: RecipeCategory
@export var required_building: String  # "hand", "assembler", "furnace", etc.
@export var unlocked_by: TechnologyResource

class ItemStack:
    var item: ItemResource
    var count: int
```

### BuildingResource (`resources/buildings/BuildingResource.gd`)
```gdscript
class_name BuildingResource
extends Resource

@export var id: String
@export var name: String
@export var description: String
@export var size: Vector2i  # grid size
@export var power_consumption: float  # kW
@export var power_production: float  # kW
@export var build_cost: Array[ItemStack]
@export var category: BuildingCategory
@export var unlocked_by: TechnologyResource

enum BuildingCategory {
    COLLECTION,
    TRANSPORT,
    PROCESSING,
    STORAGE,
    POWER,
    RESEARCH,
    LOGISTICS
}
```

### TechnologyResource (`resources/research/TechnologyResource.gd`)
```gdscript
class_name TechnologyResource
extends Resource

@export var id: String
@export var name: String
@export var description: String
@export var icon: Texture2D
@export var prerequisites: Array[TechnologyResource]
@export var science_cost: Dictionary  # {pack_type: count}
@export var unlocks_recipes: Array[RecipeResource]
@export var unlocks_buildings: Array[BuildingResource]
```

---

## Entity System

### Base Classes

#### BuildingEntity (`scripts/entities/BuildingEntity.gd`)
```gdscript
class_name BuildingEntity
extends Node2D

var definition: BuildingResource
var grid_position: Vector2i
var rotation_index: int = 0  # 0, 1, 2, 3 for N, E, S, W
var inventory: BuildingInventory

signal building_activated
signal building_deactivated
signal inventory_changed

func _ready():
    add_to_group("buildings")

func get_input_positions() -> Array[Vector2i]
func get_output_positions() -> Array[Vector2i]
func can_accept_item(item: ItemResource) -> bool
func insert_item(item: ItemResource, count: int) -> int
func extract_item() -> ItemStack
```

#### DebrisEntity (`scripts/entities/DebrisEntity.gd`)
```gdscript
class_name DebrisEntity
extends Area2D

var debris_type: String
var contents: Array[ItemStack]
var drift_velocity: Vector2
var is_collectible: bool = true

signal collected(by: Node)
signal despawned

func _physics_process(delta):
    position += drift_velocity * delta

func collect() -> Array[ItemStack]:
    collected.emit(null)
    queue_free()
    return contents
```

### Building Types

#### Conveyor Belt (`scripts/entities/buildings/ConveyorBelt.gd`)
```gdscript
class_name ConveyorBelt
extends BuildingEntity

var items_on_belt: Array[BeltItem]
var belt_speed: float = 1.0  # tiles per second
var direction: Vector2i

class BeltItem:
    var item: ItemResource
    var position: float  # 0.0 to 1.0 along belt

func _process(delta):
    move_items(delta)
    transfer_to_next()
```

#### Inserter (`scripts/entities/buildings/Inserter.gd`)
```gdscript
class_name Inserter
extends BuildingEntity

var pickup_position: Vector2i
var drop_position: Vector2i
var swing_time: float = 1.0
var current_swing: float = 0.0
var held_item: ItemStack
var state: InserterState

enum InserterState { IDLE, PICKING, DROPPING }
```

#### Assembler (`scripts/entities/buildings/Assembler.gd`)
```gdscript
class_name Assembler
extends BuildingEntity

var current_recipe: RecipeResource
var input_inventory: Dictionary  # ItemResource -> count
var output_inventory: Dictionary
var crafting_progress: float = 0.0
var is_crafting: bool = false

func set_recipe(recipe: RecipeResource):
    current_recipe = recipe

func _process(delta):
    if is_crafting:
        update_crafting(delta)
    elif can_start_crafting():
        start_crafting()
```

#### Furnace (`scripts/entities/buildings/Furnace.gd`)
```gdscript
class_name Furnace
extends BuildingEntity

var smelting_recipes: Array[RecipeResource]
var input_ore: ItemStack
var output_plate: ItemStack
var fuel: ItemStack  # For stone furnace
var smelting_progress: float = 0.0
```

---

## Scene Structure

### Main Scene Tree
```
Main (Node2D)
├── Background (ParallaxBackground)
│   └── Stars (ParallaxLayer)
├── GameWorld (Node2D)
│   ├── DebrisLayer (Node2D)
│   │   └── [DebrisEntity instances]
│   ├── StationLayer (Node2D)
│   │   ├── FoundationTiles (TileMap)
│   │   └── Buildings (Node2D)
│   │       └── [BuildingEntity instances]
│   └── ItemsLayer (Node2D)
│       └── [Dropped items]
├── Camera (Camera2D)
└── UI (CanvasLayer)
    ├── HUD
    │   ├── Hotbar
    │   ├── ResourceDisplay
    │   └── Minimap
    ├── InventoryPanel
    ├── CraftingPanel
    ├── ResearchPanel
    └── BuildMenu
```

---

## Input Handling

### InputManager (`scripts/core/InputManager.gd`)
```gdscript
# Input Actions (defined in project settings)
# - "click" -> Left Mouse Button
# - "right_click" -> Right Mouse Button
# - "inventory" -> I
# - "interact" -> E
# - "rotate" -> R
# - "cancel" -> Escape
# - "move_up/down/left/right" -> WASD / Arrows
# - "zoom_in/out" -> Mouse Wheel

var current_mode: InputMode = InputMode.NORMAL
var selected_building: BuildingResource = null
var hovered_entity: Node2D = null

enum InputMode {
    NORMAL,
    BUILDING_PLACEMENT,
    DEMOLISH
}
```

---

## Sprite Generation

### SpriteGenerator (`scripts/core/SpriteGenerator.gd`)
Procedurally generates pixel art sprites at runtime.

```gdscript
class_name SpriteGenerator

const SPRITE_SIZE = 32

static func generate_ore(color: Color, variation: int = 0) -> ImageTexture
static func generate_plate(color: Color) -> ImageTexture
static func generate_building(type: String, size: Vector2i) -> ImageTexture
static func generate_debris(type: String) -> ImageTexture
static func generate_icon(base: String, overlay: String = "") -> ImageTexture
```

---

## Save System

### SaveManager (`scripts/core/SaveManager.gd`)
```gdscript
# Save file structure (JSON)
{
    "version": "1.0",
    "timestamp": 1234567890,
    "game_state": {
        "tick": 12345,
        "game_speed": 1.0
    },
    "player": {
        "inventory": [...],
        "hotbar": [...]
    },
    "station": {
        "foundations": [[0,0], [1,0], ...],
        "buildings": [
            {
                "type": "assembler_mk1",
                "position": [1, 1],
                "rotation": 0,
                "inventory": {...},
                "recipe": "iron_gear"
            }
        ]
    },
    "research": {
        "unlocked": ["automation_1", "logistics_1"],
        "current": "automation_2",
        "progress": 0.45
    },
    "debris": {
        "spawn_rate": 2.5
    }
}
```

---

## Performance Considerations

### Optimization Strategies

1. **Object Pooling**: Reuse debris and item entities
2. **Chunk-Based Updates**: Only update visible/nearby buildings
3. **Batched Rendering**: Use tilemap for belts, batch draw calls
4. **LOD for Debris**: Simplified rendering for distant debris
5. **Tick-Based Logic**: Process buildings on game ticks, not every frame

### Target Performance
- 60 FPS with 500+ buildings
- 100+ active debris entities
- Smooth scrolling and zooming

---

## File Structure

```
scripts/
├── core/
│   ├── GameManager.gd        ✓ Implemented
│   ├── GridManager.gd        ✓ Implemented
│   ├── SpriteGenerator.gd    ✓ Implemented
│   └── SaveManager.gd        ✗ Phase 5
├── systems/
│   ├── InventoryManager.gd   ✓ Implemented
│   ├── CraftingManager.gd    ✓ Implemented
│   ├── ResearchManager.gd    ✓ Implemented
│   ├── DebrisManager.gd      ✓ Implemented
│   ├── PowerManager.gd       ✓ Implemented
│   └── BuildingManager.gd    ✓ Implemented (Phase 2)
├── entities/
│   ├── BuildingEntity.gd     ✓ Implemented - Base class
│   ├── DebrisEntity.gd       ✓ Implemented - Floating debris
│   ├── StoneFurnace.gd       ✓ Implemented - 2x2 smelter
│   ├── SmallChest.gd         ✓ Implemented - 1x1 storage
│   ├── ConveyorBelt.gd       ✓ Implemented - Item transport
│   ├── Inserter.gd           ✓ Implemented - Item transfer
│   ├── Assembler.gd          ✗ Phase 3
│   ├── Lab.gd                ✗ Phase 3
│   └── DebrisCollector.gd    ✗ Phase 3
├── ui/
│   ├── HUD.gd                ✓ Implemented
│   ├── InventoryUI.gd        ✓ Implemented
│   ├── BuildMenuUI.gd        ✓ Implemented (Phase 2)
│   ├── CraftingUI.gd         ✗ Deferred
│   ├── ResearchUI.gd         ✗ Phase 3
│   └── TooltipUI.gd          ✗ Phase 3
├── game/
│   └── Main.gd               ✓ Implemented
└── data/
    ├── Enums.gd              ✓ Implemented
    ├── ItemStack.gd          ✓ Implemented
    └── Constants.gd          ✓ Implemented

resources/
├── items/
│   └── ItemResource.gd       ✓ Implemented
├── recipes/
│   └── RecipeResource.gd     ✓ Implemented
├── buildings/
│   └── BuildingResource.gd   ✓ Implemented
└── research/
    └── TechnologyResource.gd ✓ Implemented

scenes/
└── game/
    └── Main.tscn             ✓ Implemented
```

## Autoload Singletons (project.godot)

The following singletons are configured and load automatically:
- `GameManager` - Game state and tick system
- `GridManager` - Station grid management
- `InventoryManager` - Player inventory
- `CraftingManager` - Recipes and crafting
- `DebrisManager` - Debris spawning/collection
- `ResearchManager` - Tech tree
- `PowerManager` - Power network
- `BuildingManager` - Building placement and registry (Phase 2)
