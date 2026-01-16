# Space Factory - Phase 3 Handoff Document

This document provides everything needed to continue development from Phase 2 to Phase 3.

## Quick Start

1. Open project in Godot 4.5
2. Run `scenes/game/Main.tscn`
3. Press 'B' to open the build menu
4. Select a building and click to place
5. Press 'R' to rotate while placing
6. Right-click to remove buildings
7. Review `ROADMAP.md` for Phase 3 tasks

---

## What's Been Built (Phase 1 + Phase 2)

### Core Architecture
All systems use **autoload singletons** configured in `project.godot`. Access them globally:
- `GameManager` - Game state, tick system (60 ticks/sec), pause
- `GridManager` - Station grid, building placement validation
- `InventoryManager` - Player inventory, item registry
- `CraftingManager` - Recipe registry, hand-crafting queue
- `DebrisManager` - Debris spawning and collection
- `ResearchManager` - Tech tree (ready but no UI)
- `PowerManager` - Power network simulation
- `BuildingManager` - Building registry, placement, removal (NEW)

### Phase 2 Additions

#### Building System
- **BuildingEntity** (`scripts/entities/BuildingEntity.gd`) - Base class for all buildings
  - Grid positioning and rotation
  - Power network integration (auto-registers with PowerManager)
  - Tick-based processing via `_process_building()`
  - Internal inventory support for storage buildings
  - Insert/extract item methods for inserter compatibility

#### Buildings Implemented
| Building | Size | Description |
|----------|------|-------------|
| Stone Furnace | 2x2 | Smelts ores using coal fuel |
| Small Chest | 1x1 | 16-slot storage |
| Transport Belt | 1x1 | Moves items, auto-connects |
| Inserter | 1x1 | Transfers items between buildings |
| Long Inserter | 1x1 | Reaches 2 tiles (requires research) |
| Solar Panel | 2x2 | Power generation (requires research) |

#### BuildingManager Singleton
- Building registry with `get_building(id)` and `get_all_buildings()`
- Build mode with ghost preview
- Placement validation (foundation + resource cost)
- Building removal with full refund
- Category filtering for build menu

#### Build Menu UI
- Press 'B' to toggle
- Categories: Processing, Storage, Transport, Power
- Shows building cost with color-coded availability
- Click to enter build mode

#### Controls
- **B** - Toggle build menu
- **R** - Rotate building (while placing)
- **Left-click** - Place building (in build mode) / Interact
- **Right-click** - Cancel build mode / Remove building
- **Escape** - Exit build mode

---

## Key Files to Understand

| File | What It Does |
|------|--------------|
| `scripts/entities/BuildingEntity.gd` | Base class for all buildings |
| `scripts/entities/StoneFurnace.gd` | Furnace with fuel/input/output slots |
| `scripts/entities/SmallChest.gd` | Storage with 16 inventory slots |
| `scripts/entities/ConveyorBelt.gd` | Item transport with auto-connection |
| `scripts/entities/Inserter.gd` | Item transfer with swing animation |
| `scripts/systems/BuildingManager.gd` | Building placement and registry |
| `scripts/ui/BuildMenuUI.gd` | Build menu interface |

---

## How Buildings Work

### Furnace Processing
```gdscript
# StoneFurnace has three special slots:
var fuel_slot: ItemStack      # Coal goes here
var input_slot: ItemStack     # Ore goes here
var output_slot: ItemStack    # Plates come out here

# Furnace automatically:
# 1. Finds matching recipe for input ore
# 2. Consumes fuel to maintain burn time
# 3. Progresses crafting each tick
# 4. Outputs result when complete
```

### Belt Item Movement
```gdscript
# ConveyorBelt moves items at Constants.BELT_SPEED_TIER_1 (1 tile/sec)
# Items have progress 0.0 -> 1.0 along belt
# Belts auto-connect to adjacent belts facing the same way
# Items transfer when progress >= 1.0 and next belt is empty
```

### Inserter Logic
```gdscript
# Inserter picks from BEHIND (opposite of facing direction)
# and drops IN FRONT (facing direction)
# Swing takes Constants.INSERTER_SWING_TIME seconds
# Will only pick up if destination can accept item
```

---

## Phase 3 Tasks (from ROADMAP.md)

### 3.1 Research System UI
- Create research/tech tree panel (toggle with T)
- Show available and locked technologies
- Research progress display
- Lab building to consume science packs

### 3.2 Science Packs
- Create Automation Science Pack item
- Recipe for science pack crafting
- Lab consumes packs for research progress

### 3.3 Station Expansion
- Foundation item + recipe
- Allow placing foundation adjacent to existing
- Expand buildable area

### 3.4 Assembler Building
- Multi-ingredient crafting machine
- Recipe selection UI
- Faster than hand-crafting

### 3.5 More Buildings
- Fast Inserter (research unlock)
- Underground Belt
- Splitter
- Medium Chest

### 3.6 Debris Collector Building
- Automatic debris collection
- Range visualization
- Output to belts/chests

---

## How Systems Communicate

### Signal-Based Architecture
```
BuildingManager.building_placed → GridManager stores reference
BuildingManager.building_removed → GridManager removes reference
BuildingEntity._ready() → registers with PowerManager
BuildingEntity.on_removed() → unregisters from PowerManager
GameManager.game_tick → all buildings process via _on_game_tick()
```

### Building Tick Processing
```gdscript
# Buildings hook into game tick automatically:
func _ready() -> void:
    GameManager.game_tick.connect(_on_game_tick)

func _on_game_tick(tick: int) -> void:
    if not is_powered:
        return
    _process_building()  # Override this

func _process_building() -> void:
    # Furnace: check recipe, consume fuel, progress crafting
    # Belt: move items, transfer to next belt
    # Inserter: swing arm, pick up, drop items
    pass
```

---

## Sprite Generation Reference

New building sprites in SpriteGenerator:
```gdscript
SpriteGenerator.generate_furnace(is_electric: bool) -> ImageTexture  # 64x64 (2x2)
SpriteGenerator.generate_chest(color: Color) -> ImageTexture          # 32x32 (1x1)
SpriteGenerator.generate_belt(direction: Enums.Direction) -> ImageTexture  # 32x32
SpriteGenerator.generate_inserter(is_long: bool) -> ImageTexture      # 32x32
SpriteGenerator.generate_solar_panel() -> ImageTexture                # 64x64 (2x2)
```

---

## Known Issues / Technical Debt

1. **Inserter arm visual** - The arm rotation visual isn't perfectly implemented; may need refinement for smooth animation.

2. **Belt corners** - Belts only go straight; corner/turn pieces not yet implemented.

3. **No crafting UI** - Hand-crafting queue exists but no UI to trigger it (Phase 1 leftover).

4. **Collection feedback** - Debris still lacks particle effects on collection.

5. **Building UI** - No UI for interacting with placed buildings (e.g., seeing furnace contents).

6. **Research gating** - Technology requirements checked but most buildings are available by default for testing.

---

## Testing Checklist

Before starting Phase 3 work, verify:
- [ ] Game runs without errors
- [ ] Press B opens build menu
- [ ] Can place Stone Furnace (costs 5 stone)
- [ ] Can place Small Chest (costs 2 iron plates)
- [ ] Can place Transport Belt (costs 1 gear + 1 iron plate)
- [ ] Can place Inserter (costs 1 gear + 1 plate + 1 circuit)
- [ ] R rotates building preview
- [ ] Right-click removes buildings (refunds materials)
- [ ] Buildings appear on station grid
- [ ] Furnace smelts ore when given fuel and input

---

## File Quick Reference

```
scenes/game/Main.tscn              - Main game scene
scripts/game/Main.gd               - Main scene logic + building integration
scripts/core/GameManager.gd        - Game state singleton
scripts/core/GridManager.gd        - Grid/building singleton
scripts/core/SpriteGenerator.gd    - Procedural sprites
scripts/systems/BuildingManager.gd - Building placement (NEW)
scripts/systems/*.gd               - All manager singletons
scripts/entities/BuildingEntity.gd - Building base class (NEW)
scripts/entities/StoneFurnace.gd   - Furnace building (NEW)
scripts/entities/SmallChest.gd     - Chest building (NEW)
scripts/entities/ConveyorBelt.gd   - Belt building (NEW)
scripts/entities/Inserter.gd       - Inserter building (NEW)
scripts/ui/BuildMenuUI.gd          - Build menu (NEW)
scripts/ui/HUD.gd                  - Hotbar and resource display
scripts/ui/InventoryUI.gd          - Inventory panel
scripts/data/*.gd                  - Enums, Constants, ItemStack
resources/*/*.gd                   - Resource class definitions
```

---

## Starter Items for Testing

The game now gives more starting items for testing Phase 2:
- 50 Iron Ore
- 30 Copper Ore
- 20 Coal
- 30 Stone
- 20 Iron Plates
- 10 Iron Gears
- 10 Electronic Circuits

This allows testing all basic buildings without manual crafting.

---

## Questions?

Refer to:
- `DESIGN.md` - Game mechanics and balance
- `ARCHITECTURE.md` - Technical details and patterns
- `ROADMAP.md` - Full implementation plan with checkboxes
