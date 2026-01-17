# Space Factory - Phase 3 Handoff Document

This document provides everything needed to continue development. Phase 3 is mostly complete.

**Note:** The codebase was converted from GDScript to C# for better tooling, type safety, and performance.

## Quick Start

1. Open project in Godot 4.5
2. Build the C# solution: `dotnet build` in project root
3. Run `scenes/game/Main.tscn`
4. Press 'B' to open the build menu
5. Select a building and click to place
6. Press 'R' to rotate while placing (also works with Recipe list open)
7. Right-click to remove buildings
8. Press 'T' to open Research UI
9. Press 'C' to open Crafting UI
10. Press 'R' to view Recipe list
11. Review `ROADMAP.md` for remaining tasks

---

## What's Been Built (Phase 1 + Phase 2 + Phase 3)

### Core Architecture
All systems use **autoload singletons** configured in `project.godot`. Access them via static `Instance` property:
- `GameManager.Instance` - Game state, tick system (60 ticks/sec), pause
- `GridManager.Instance` - Station grid, building placement validation
- `InventoryManager.Instance` - Player inventory, item registry
- `CraftingManager.Instance` - Recipe registry, hand-crafting queue, recursive crafting
- `DebrisManager.Instance` - Debris spawning and collection (configurable spawn rates)
- `ResearchManager.Instance` - Tech tree with 8 technologies, research progress tracking
- `PowerManager.Instance` - Power network simulation
- `BuildingManager.Instance` - Building registry, placement, removal

### Phase 2 Additions

#### Building System
- **BuildingEntity** (`scripts/csharp/BuildingEntity.cs`) - Base class for all buildings
  - Grid positioning and rotation
  - Power network integration (auto-registers with PowerManager)
  - Tick-based processing via `ProcessBuilding()`
  - Internal inventory support for storage buildings
  - Insert/extract item methods for inserter compatibility

#### Buildings Implemented
| Building | Size | Description |
|----------|------|-------------|
| Stone Furnace | 2x2 | Smelts ores using coal fuel |
| Small Chest | 1x1 | 16-slot storage |
| Transport Belt | 1x1 | Moves items, auto-connects |
| Inserter | 1x1 | Transfers items between buildings |
| Long Inserter | 1x1 | Reaches 2 tiles (requires automation_1 research) |
| Solar Panel | 2x2 | Power generation (requires solar_energy research) |
| Lab | 2x2 | Consumes science packs for research progress |
| Assembler Mk1 | 2x2 | Auto-crafts items from recipes (requires automation_1 research) |
| Collector | 1x1 | Automated debris collection with animated arm, 4 output slots |
| Foundation | 1x1 | Expands buildable station area |

#### BuildingManager Singleton
- Building registry with `GetBuilding(id)` and `GetAllBuildings()`
- Build mode with ghost preview
- Placement validation (foundation + resource cost)
- Building removal with full refund
- Category filtering for build menu

#### Build Menu UI
- Press 'B' to toggle
- Categories: Processing, Storage, Transport, Collection, Power, Research, Foundation
- Shows building cost with color-coded availability
- Click to enter build mode
- Game continues running while build menu is open

#### Controls
- **B** - Toggle build menu
- **R** - Rotate building (while placing) / Open Recipe list
- **T** - Toggle Research UI (tech tree)
- **C** - Toggle Crafting UI (hand-craft items)
- **I** - Toggle Inventory
- **Left-click** - Place building (in build mode) / Interact / Collect debris
- **Shift+Left-click** - Stack transfer (move entire stack between inventories)
- **Right-click** - Cancel build mode / Remove building
- **Escape** - Exit build mode / Close UI panels
- **X button** - Close any draggable window

---

## Key Files to Understand

| File | What It Does |
|------|--------------|
| `scripts/csharp/BuildingEntity.cs` | Base class for all buildings |
| `scripts/csharp/StoneFurnace.cs` | Furnace with fuel/input/output slots |
| `scripts/csharp/SmallChest.cs` | Storage with 16 inventory slots |
| `scripts/csharp/ConveyorBelt.cs` | Item transport with auto-connection |
| `scripts/csharp/Inserter.cs` | Item transfer with swing animation |
| `scripts/csharp/Lab.cs` | 2x2 research building, consumes science packs |
| `scripts/csharp/Assembler.cs` | 2x2 auto-crafter with tier system |
| `scripts/csharp/Collector.cs` | Debris collector with animated arm, 4 output slots |
| `scripts/csharp/BuildingManager.cs` | Building placement and registry |
| `scripts/csharp/ResearchManager.cs` | Tech tree, research progress, unlocks |
| `scripts/csharp/BuildMenuUI.cs` | Build menu interface |
| `scripts/csharp/BuildingUI.cs` | Building interaction panel (draggable, X to close) |
| `scripts/csharp/ResearchUI.cs` | Research panel with tech tree |
| `scripts/csharp/RecipeUI.cs` | Hand-crafting panel with x5 batch crafting |
| `scripts/csharp/HUD.cs` | Toolbar, notifications, craft queue display |

---

## How Buildings Work

### Furnace Processing
```csharp
// StoneFurnace has three special slots:
private ItemStack _fuelSlot;      // Coal goes here
private ItemStack _inputSlot;     // Ore goes here
private ItemStack _outputSlot;    // Plates come out here

// Furnace automatically:
// 1. Finds matching recipe for input ore
// 2. Consumes fuel to maintain burn time
// 3. Progresses crafting each tick
// 4. Outputs result when complete
```

### Belt Item Movement
```csharp
// ConveyorBelt moves items at Constants.BeltSpeedTier1 (1 tile/sec)
// Items have progress 0.0 -> 1.0 along belt
// Belts auto-connect to adjacent belts facing the same way
// Items transfer when progress >= 1.0 and next belt is empty
```

### Inserter Logic
```csharp
// Inserter picks from BEHIND (opposite of facing direction)
// and drops IN FRONT (facing direction)
// Swing takes Constants.InserterSwingTime seconds
// Will only pick up if destination can accept item
```

### Collector Logic
```csharp
// Collector has 4 output slots (OutputSlots array)
// Animated arm extends toward nearest debris in range
// States: Idle -> Extending -> Grabbing -> Retracting
// Stops collecting when all 4 slots are full
// Items must be removed by inserter or manually
// Collection range defined by Constants.CollectorTier1Range
```

---

## Phase 3 Status (from ROADMAP.md)

### ✓ 3.1 Research System UI (COMPLETE)
- ✓ Research/tech tree panel (toggle with T) - `ResearchUI.cs`
- ✓ Show available and locked technologies with status icons
- ✓ Research progress display with progress bar
- ✓ Lab building to consume science packs - `Lab.cs`
- ✓ Research completion notifications in HUD

### ✓ 3.2 Science Packs (COMPLETE)
- ✓ Automation Science Pack (red) - item registered in InventoryManager
- ✓ Logistic Science Pack (green) - item registered in InventoryManager
- ✓ Recipes for science pack crafting in CraftingManager
- ✓ Lab consumes packs for research progress

### ✓ 3.3 Station Expansion (COMPLETE)
- ✓ Foundation item + recipe
- ✓ Foundation category in build menu
- ✓ Allow placing foundation adjacent to existing station
- ✓ Expand buildable area

### ✓ 3.4 Assembler Building (COMPLETE)
- ✓ Assembler Mk1 entity with tier system - `Assembler.cs`
- ✓ Recipe selection UI in BuildingUI
- ✓ Recipe requirements display with color-coded ingredients (green=have, red=need)
- ✓ Multi-input handling (4 slots)
- ✓ Crafting progress display

### 3.5 More Buildings (PARTIAL)
- ✓ Long Inserter (unlocked by automation_1 research)
- Fast Inserter (not started)
- Underground Belt (not started)
- Splitter (not started)
- Medium Chest (not started)

### ✓ 3.6 Debris Collector Building (COMPLETE)
- ✓ Automatic debris collection with animated robotic arm - `Collector.cs`
- ✓ 4 output slots (stops when full, encourages automation)
- ✓ Output via inserters or manual extraction
- ✓ Collection range based on tier (Constants.CollectorTier1Range)
- ✓ States: Idle, Extending, Grabbing, Retracting with visual feedback

---

## How Systems Communicate

### Signal-Based Architecture
```
BuildingManager.BuildingPlaced → GridManager stores reference
BuildingManager.BuildingRemoved → GridManager removes reference
BuildingEntity._Ready() → registers with PowerManager
BuildingEntity.OnRemoved() → unregisters from PowerManager
GameManager.GameTick → all buildings process via OnGameTick()
```

### Building Tick Processing
```csharp
// Buildings hook into game tick automatically:
public override void _Ready()
{
    base._Ready();
    if (GameManager.Instance != null)
        GameManager.Instance.GameTick += OnGameTick;
}

private void OnGameTick(int tick)
{
    if (!IsPowered)
        return;
    ProcessBuilding();  // Override this
}

protected virtual void ProcessBuilding()
{
    // Furnace: check recipe, consume fuel, progress crafting
    // Belt: move items, transfer to next belt
    // Inserter: swing arm, pick up, drop items
}
```

---

## Sprite Generation Reference

New building sprites in SpriteGenerator:
```csharp
SpriteGenerator.Instance.GenerateFurnace(bool isElectric)  // 64x64 (2x2)
SpriteGenerator.Instance.GenerateChest(Color color)        // 32x32 (1x1)
SpriteGenerator.Instance.GenerateBelt(Enums.Direction dir) // 32x32
SpriteGenerator.Instance.GenerateInserter(bool isLong)     // 32x32
SpriteGenerator.Instance.GenerateSolarPanel()              // 64x64 (2x2)
```

---

## Known Issues / Technical Debt

1. **Inserter arm visual** - The arm rotation visual isn't perfectly implemented; may need refinement for smooth animation.

2. **Belt corners** - Belts only go straight; corner/turn pieces not yet implemented.

3. **Collection feedback** - Debris still lacks particle effects on collection.

4. **Power poles** - Power network exists but power pole placement and visualization not implemented.

5. **Belt drag-placement** - Can only place belts one at a time, no drag-to-place-multiple.

## Recent UI Improvements

1. **Draggable Windows** - Building UI, Inventory, Crafting, and Research panels can be dragged by their title bars.

2. **X Close Buttons** - All windows have X buttons in the top-right corner to close them.

3. **Craft Queue Display** - HUD shows current crafting queue with progress bar and item count.

4. **Research Notifications** - Toast notifications appear when research completes.

5. **Recipe List** - Press R to view all recipes in the game.

6. **x5 Batch Crafting** - Shift+click on craft button to queue 5 items at once.

7. **Assembler Recipe Display** - When assembler has a recipe selected, shows ingredient requirements with color coding.

8. **Recursive Crafting** - Automatically crafts intermediate items when you have raw materials. Button shows "Craft+" (blue) when intermediates will be crafted.

---

## Testing Checklist

Before continuing development, verify:
- [ ] Game runs without errors (build with `dotnet build` first)
- [ ] Press B opens build menu (game continues while open)
- [ ] Press T opens Research UI
- [ ] Press C opens Crafting UI
- [ ] Press R opens Recipe list
- [ ] Can place Stone Furnace (costs 5 stone)
- [ ] Can place Small Chest (costs 2 iron plates)
- [ ] Can place Transport Belt (costs 1 gear + 1 iron plate)
- [ ] Can place Inserter (costs 1 gear + 1 plate + 1 circuit)
- [ ] Can place Collector (Collection category)
- [ ] Can place Foundation (Foundation category, adjacent to existing)
- [ ] Can place Lab (costs 10 iron + 10 copper + 10 circuits)
- [ ] R rotates building preview
- [ ] Right-click removes buildings (refunds materials)
- [ ] Buildings appear on station grid
- [ ] Furnace smelts ore when given fuel and input
- [ ] Collector extends arm toward debris, collects into 4 slots
- [ ] Collector stops when all 4 slots are full
- [ ] Can research Automation via Research UI
- [ ] Research completion shows notification
- [ ] After researching Automation, Long Inserter and Assembler Mk1 appear in build menu
- [ ] Assembler shows recipe requirements with color coding
- [ ] Lab consumes science packs when research is active
- [ ] Shift+click transfers full stacks in building inventories
- [ ] Shift+click on craft queues 5 items
- [ ] Craft queue displays in HUD with progress
- [ ] Windows are draggable and have X close buttons
- [ ] Recursive crafting: "Craft+" button appears when intermediates needed
- [ ] Clicking "Craft+" queues intermediate recipes before final item

---

## File Quick Reference

```
scenes/game/Main.tscn                - Main game scene
scripts/csharp/Main.cs               - Main scene logic + building integration
scripts/csharp/GameManager.cs        - Game state singleton
scripts/csharp/GridManager.cs        - Grid/building singleton
scripts/csharp/SpriteGenerator.cs    - Procedural sprites
scripts/csharp/BuildingManager.cs    - Building placement
scripts/csharp/InventoryManager.cs   - Player inventory
scripts/csharp/CraftingManager.cs    - Recipes and crafting
scripts/csharp/DebrisManager.cs      - Debris spawning (configurable rates)
scripts/csharp/ResearchManager.cs    - Tech tree (8 technologies)
scripts/csharp/PowerManager.cs       - Power networks
scripts/csharp/BuildingEntity.cs     - Building base class
scripts/csharp/StoneFurnace.cs       - Furnace building
scripts/csharp/SmallChest.cs         - Chest building
scripts/csharp/ConveyorBelt.cs       - Belt building
scripts/csharp/Inserter.cs           - Inserter building
scripts/csharp/Lab.cs                - Lab building (research)
scripts/csharp/Assembler.cs          - Assembler building (auto-craft, recipe display)
scripts/csharp/Collector.cs          - Debris collector (4 slots, animated arm)
scripts/csharp/HUD.cs                - Toolbar, notifications, craft queue display
scripts/csharp/InventoryUI.cs        - Inventory panel (draggable)
scripts/csharp/BuildMenuUI.cs        - Build menu (all categories)
scripts/csharp/BuildingUI.cs         - Building interaction UI (draggable, recipe requirements)
scripts/csharp/ResearchUI.cs         - Research/tech tree panel
scripts/csharp/RecipeUI.cs           - Hand-crafting panel (x5 batch, recipe list)
scripts/csharp/Enums.cs              - Game enums
scripts/csharp/Constants.cs          - Game constants (collector ranges, debris rates)
scripts/csharp/ItemStack.cs          - Item stack class
scripts/csharp/ItemResource.cs       - Item definition resource
scripts/csharp/RecipeResource.cs     - Recipe definition resource
scripts/csharp/TechnologyResource.cs - Technology definition resource
scripts/csharp/BuildingResource.cs   - Building definition resource
```

---

## C# Conversion Notes

The codebase was converted from GDScript to C# with these patterns:

### Singleton Access
```csharp
// Access autoload singletons via static Instance property
var item = InventoryManager.Instance?.GetItem("iron_ore");
GridManager.Instance?.PlaceBuilding(pos, building);
```

### No Namespaces
C# files do NOT use namespaces (Godot autoload discovery issue). Classes are defined at global scope with `// SpaceFactory` comment.

### Node References
C# exported properties with NodePath don't auto-resolve. Nodes are fetched manually in `_Ready()`:
```csharp
public override void _Ready()
{
    Camera ??= GetNodeOrNull<Camera2D>("Camera2D");
    Hud ??= GetNodeOrNull<HUD>("HUD");
}
```

### Signals
C# signals use delegate naming convention:
```csharp
[Signal]
public delegate void BuildingPlacedEventHandler(BuildingEntity building);

// Emit
EmitSignal(SignalName.BuildingPlaced, building);

// Connect
BuildingManager.Instance.BuildingPlaced += OnBuildingPlaced;
```

---

## Starter Items for Testing

The game gives starting items for testing Phase 2:
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
