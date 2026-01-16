# Space Factory - Phase 3 Handoff Document

This document provides everything needed to continue development from Phase 2 to Phase 3.

**Note:** The codebase was converted from GDScript to C# for better tooling, type safety, and performance.

## Quick Start

1. Open project in Godot 4.5
2. Build the C# solution: `dotnet build` in project root
3. Run `scenes/game/Main.tscn`
4. Press 'B' to open the build menu
5. Select a building and click to place
6. Press 'R' to rotate while placing
7. Right-click to remove buildings
8. Review `ROADMAP.md` for Phase 3 tasks

---

## What's Been Built (Phase 1 + Phase 2)

### Core Architecture
All systems use **autoload singletons** configured in `project.godot`. Access them via static `Instance` property:
- `GameManager.Instance` - Game state, tick system (60 ticks/sec), pause
- `GridManager.Instance` - Station grid, building placement validation
- `InventoryManager.Instance` - Player inventory, item registry
- `CraftingManager.Instance` - Recipe registry, hand-crafting queue
- `DebrisManager.Instance` - Debris spawning and collection
- `ResearchManager.Instance` - Tech tree (ready but no UI)
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
| Long Inserter | 1x1 | Reaches 2 tiles (requires research) |
| Solar Panel | 2x2 | Power generation (requires research) |

#### BuildingManager Singleton
- Building registry with `GetBuilding(id)` and `GetAllBuildings()`
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
| `scripts/csharp/BuildingEntity.cs` | Base class for all buildings |
| `scripts/csharp/StoneFurnace.cs` | Furnace with fuel/input/output slots |
| `scripts/csharp/SmallChest.cs` | Storage with 16 inventory slots |
| `scripts/csharp/ConveyorBelt.cs` | Item transport with auto-connection |
| `scripts/csharp/Inserter.cs` | Item transfer with swing animation |
| `scripts/csharp/BuildingManager.cs` | Building placement and registry |
| `scripts/csharp/BuildMenuUI.cs` | Build menu interface |

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

3. **No crafting UI** - Hand-crafting queue exists but no UI to trigger it (Phase 1 leftover).

4. **Collection feedback** - Debris still lacks particle effects on collection.

5. **Building UI** - No UI for interacting with placed buildings (e.g., seeing furnace contents).

6. **Research gating** - Technology requirements checked but most buildings are available by default for testing.

---

## Testing Checklist

Before starting Phase 3 work, verify:
- [ ] Game runs without errors (build with `dotnet build` first)
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
scenes/game/Main.tscn                - Main game scene
scripts/csharp/Main.cs               - Main scene logic + building integration
scripts/csharp/GameManager.cs        - Game state singleton
scripts/csharp/GridManager.cs        - Grid/building singleton
scripts/csharp/SpriteGenerator.cs    - Procedural sprites
scripts/csharp/BuildingManager.cs    - Building placement
scripts/csharp/InventoryManager.cs   - Player inventory
scripts/csharp/CraftingManager.cs    - Recipes and crafting
scripts/csharp/DebrisManager.cs      - Debris spawning
scripts/csharp/ResearchManager.cs    - Tech tree
scripts/csharp/PowerManager.cs       - Power networks
scripts/csharp/BuildingEntity.cs     - Building base class
scripts/csharp/StoneFurnace.cs       - Furnace building
scripts/csharp/SmallChest.cs         - Chest building
scripts/csharp/ConveyorBelt.cs       - Belt building
scripts/csharp/Inserter.cs           - Inserter building
scripts/csharp/HUD.cs                - Hotbar and resource display
scripts/csharp/InventoryUI.cs        - Inventory panel
scripts/csharp/BuildMenuUI.cs        - Build menu
scripts/csharp/BuildingUI.cs         - Building interaction UI
scripts/csharp/Enums.cs              - Game enums
scripts/csharp/Constants.cs          - Game constants
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
