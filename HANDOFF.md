# Space Factory - Phase 2 Handoff Document

This document provides everything needed to continue development from Phase 1 to Phase 2.

## Quick Start

1. Open project in Godot 4.5
2. Run `scenes/game/Main.tscn`
3. Review `ROADMAP.md` for Phase 2 tasks

---

## What's Been Built (Phase 1)

### Core Architecture
All systems use **autoload singletons** configured in `project.godot`. Access them globally:
- `GameManager` - Game state, tick system (60 ticks/sec), pause
- `GridManager` - Station grid, building placement validation
- `InventoryManager` - Player inventory, item registry
- `CraftingManager` - Recipe registry, hand-crafting queue
- `DebrisManager` - Debris spawning and collection
- `ResearchManager` - Tech tree (ready but no UI)
- `PowerManager` - Power network (ready for buildings)

### Key Files to Understand

| File | What It Does |
|------|--------------|
| `scripts/data/Constants.gd` | All game constants (tile size, speeds, colors) |
| `scripts/data/Enums.gd` | All enumerations + helper functions |
| `scripts/core/SpriteGenerator.gd` | Procedural sprite generation (static methods) |
| `scripts/game/Main.gd` | Main scene setup, camera, input handling |

### Pre-Registered Items
`InventoryManager` auto-registers these items on startup:
- Raw: `iron_ore`, `copper_ore`, `stone`, `coal`, `scrap_metal`, `ice`
- Processed: `iron_plate`, `copper_plate`, `stone_brick`, `steel_plate`
- Components: `iron_gear`, `copper_cable`, `electronic_circuit`

### Pre-Registered Recipes
`CraftingManager` auto-registers these recipes:
- Smelting (FURNACE): iron_plate, copper_plate, stone_brick, steel_plate
- Hand-craft (HAND): iron_gear, copper_cable, electronic_circuit

### Pre-Registered Technologies
`ResearchManager` has a basic tech tree ready (see `_register_default_technologies()`)

---

## Phase 2 Tasks (from ROADMAP.md)

### 2.1 Building System
Create `scripts/entities/BuildingEntity.gd` as base class:
```gdscript
class_name BuildingEntity
extends Node2D

var definition: BuildingResource
var grid_position: Vector2i
var rotation_index: int = 0  # 0-3 for N/E/S/W

func get_definition() -> BuildingResource:
    return definition
```

Key integration points:
- `GridManager.place_building(pos, node, def)` - Already implemented
- `GridManager.remove_building(pos)` - Already implemented
- `GridManager.can_place_building(pos, def)` - Already implemented

### 2.2 First Buildings to Implement

**Stone Furnace** (2x2, coal-powered):
- Input: 1 ore + fuel (coal)
- Output: 1 plate
- Use `CraftingManager.get_recipes_for_building(Enums.CraftingType.FURNACE)`

**Small Chest** (1x1):
- 16 inventory slots
- Inserters can put/take items

### 2.3 Conveyor Belts
- 1x1 tile, directional
- Items move along belt at `Constants.BELT_SPEED_TIER_1` (1 tile/sec)
- Use `SpriteGenerator.generate_belt(direction)` for sprites
- Connect to adjacent belts automatically

### 2.4 Inserters
- Pickup from one side, drop to other side
- Swing time in `Constants.INSERTER_SWING_TIME`
- Use `SpriteGenerator.generate_inserter(is_long)` for sprites
- Check `GridManager.get_adjacent_building()` for targets

### 2.5 Build Menu UI
Create `scripts/ui/BuildMenuUI.gd`:
- Show available buildings (check `ResearchManager.is_technology_unlocked()`)
- Ghost preview during placement
- R key to rotate (`Enums.rotate_direction_cw()`)
- Left-click to place, right-click to cancel

---

## How Systems Communicate

### Signal-Based Architecture
```
DebrisManager.debris_collected → updates InventoryManager
InventoryManager.inventory_changed → updates HUD
CraftingManager.craft_completed → updates InventoryManager
GridManager.building_placed → (connect your building logic)
PowerManager.brownout_started → (buildings should slow down)
```

### Game Tick System
Buildings should hook into `GameManager.game_tick`:
```gdscript
func _ready():
    GameManager.game_tick.connect(_on_tick)

func _on_tick(tick: int):
    # Process one tick of building logic
    # Use GameManager.game_speed for time scaling
```

---

## Sprite Generation Reference

All sprites are 32x32 pixels. Use these static methods:

```gdscript
# Items
SpriteGenerator.generate_ore(color, variation_seed)
SpriteGenerator.generate_plate(color)
SpriteGenerator.generate_gear(color)
SpriteGenerator.generate_cable(color)
SpriteGenerator.generate_circuit(color, complexity)

# Buildings
SpriteGenerator.generate_building(color, size_vector2i)
SpriteGenerator.generate_furnace(is_electric)
SpriteGenerator.generate_chest(color)
SpriteGenerator.generate_belt(direction_enum)
SpriteGenerator.generate_inserter(is_long)
SpriteGenerator.generate_solar_panel()
SpriteGenerator.generate_foundation()

# Debris
SpriteGenerator.generate_debris(type_string, variation)
```

Colors are defined in `Constants.gd` (COLOR_IRON_PLATE, COLOR_COPPER_ORE, etc.)

---

## Known Issues / Technical Debt

1. **Debris entities are created inline** in `DebrisManager._create_debris_entity()` rather than as a separate scene/class. Consider extracting to `scripts/entities/DebrisEntity.gd`.

2. **No crafting UI** - `CraftingManager` has queue system ready, but no UI to trigger hand-crafting. This is a remaining Phase 1 item.

3. **Hotbar not fully connected** - Hotbar displays but doesn't sync with inventory for quick-select building placement.

4. **No collection feedback** - Debris disappears instantly when clicked. Should add particles or animation.

---

## Testing Checklist

Before starting Phase 2 work, verify:
- [ ] Game runs without errors in Godot 4.5
- [ ] Debris spawns and drifts across screen
- [ ] Clicking debris adds items to inventory
- [ ] 'I' key opens/closes inventory
- [ ] WASD pans camera
- [ ] Scroll wheel zooms
- [ ] Station foundation (3x3 gray grid) visible at center

---

## File Quick Reference

```
scenes/game/Main.tscn          - Main game scene
scripts/game/Main.gd           - Main scene logic
scripts/core/GameManager.gd    - Game state singleton
scripts/core/GridManager.gd    - Grid/building singleton
scripts/core/SpriteGenerator.gd - Procedural sprites
scripts/systems/*.gd           - All manager singletons
scripts/data/*.gd              - Enums, Constants, ItemStack
scripts/ui/HUD.gd              - Hotbar and resource display
scripts/ui/InventoryUI.gd      - Inventory panel
resources/*/*.gd               - Resource class definitions
```

---

## Questions?

Refer to:
- `DESIGN.md` - Game mechanics and balance
- `ARCHITECTURE.md` - Technical details and patterns
- `ROADMAP.md` - Full implementation plan with checkboxes
