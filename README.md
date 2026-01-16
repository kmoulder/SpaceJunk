# Space Factory

A Factorio-inspired automation game set on a space station drifting through a debris field. Collect floating space junk, process raw materials into refined goods, and build an ever-expanding automated factory in the void of space.

## Current Status: Phase 2 Complete

**What's Working:**
- Debris spawns and drifts across the screen
- Click debris to collect resources into inventory
- Press 'I' to open/close 40-slot inventory
- Press 'B' to open build menu
- Place buildings on station grid (furnace, chest, belt, inserter)
- Press 'R' to rotate buildings while placing
- Right-click to cancel placement or remove buildings
- Hotbar displays at bottom of screen
- WASD/arrows to pan camera, scroll to zoom
- 3x3 station foundation renders at center
- All core singletons operational (Game, Grid, Inventory, Crafting, Research, Power, Debris, Building)
- Procedural pixel art generation for all sprites

**Phase 2 Buildings:**
- Stone Furnace (2x2) - Smelts ores with coal fuel
- Small Chest (1x1) - 16-slot storage
- Transport Belt (1x1) - Moves items, auto-connects to adjacent belts
- Inserter (1x1) - Transfers items between buildings

**Remaining Items (deferred):**
- Crafting UI panel (recipes exist but no UI to trigger hand-crafting)
- Collection visual feedback (particles/sounds)
- Belt drag-placement (place multiple in a line)
- Power pole placement and network visualization

**See [ROADMAP.md](ROADMAP.md) for full implementation plan.**

---

## Game Concept

You are the operator of a small 3x3 space station floating through an endless debris field. Initially, you must manually click to collect nearby asteroids, metal scrap, and other space junk. As you gather resources, you can process them into refined materials and construct buildings to automate collection, transport, and manufacturing.

The ultimate goal is to build a self-sustaining space factory, researching new technologies, expanding your station's footprint with manufactured foundations, and eventually launching... something grand.

## Core Features

- **Manual to Automation Progression**: Start by clicking to collect debris, end with fully automated production lines
- **Drifting Debris Field**: Resources float past your station - time your collection or build collectors to grab them automatically
- **Grid-Based Building**: Snap-to-grid placement system for structures on your station
- **Expandable Station**: Craft foundation tiles to grow your 3x3 starting area
- **Factorio-Style Production Chains**: Ore → Plates → Components → Products
- **Research System**: Unlock new buildings and recipes through science
- **Inventory Management**: Stackable items, accessible via 'I' key

## Tech Stack

- **Engine**: Godot 4.5
- **Graphics**: Procedurally generated pixel art (32x32 sprites)
- **Perspective**: Top-down 2D

## Project Structure

```
SpaceFactory/
├── assets/
│   ├── sprites/          # Generated sprite textures
│   └── audio/            # Sound effects and music
├── scenes/
│   ├── ui/               # UI scenes (inventory, crafting, etc.)
│   └── game/             # Game world scenes
├── scripts/
│   ├── core/             # Core game systems
│   ├── entities/         # Game entity scripts
│   ├── systems/          # Game system managers
│   ├── ui/               # UI controller scripts
│   └── data/             # Data classes and enums
├── resources/
│   ├── items/            # Item resource definitions
│   ├── recipes/          # Crafting recipe resources
│   ├── buildings/        # Building definitions
│   └── research/         # Tech tree definitions
├── DESIGN.md             # Game Design Document
├── ARCHITECTURE.md       # Technical Architecture
└── ROADMAP.md           # Implementation Roadmap
```

## Getting Started

1. Open the project in Godot 4.5
2. Run the main scene (`scenes/game/Main.tscn`)
3. Click on floating debris to collect resources
4. Press 'I' to open inventory
5. Use number keys 1-0 to select hotbar slots
6. Build and automate!

**Note:** On first run, you'll receive starting resources (iron ore, copper ore, coal, stone, iron plates, iron gears, circuits) for testing buildings.

## Documentation

- [Game Design Document](DESIGN.md) - Detailed game mechanics and systems
- [Technical Architecture](ARCHITECTURE.md) - Code structure and patterns
- [Implementation Roadmap](ROADMAP.md) - Phased development plan
- [Phase 3 Handoff](HANDOFF.md) - Guide for continuing development

## Controls

| Key | Action |
|-----|--------|
| Left Click | Collect debris / Place building |
| Right Click | Cancel placement / Remove building |
| I | Toggle inventory |
| B | Toggle build menu |
| R | Rotate building (while placing) |
| E | Interact with building |
| WASD / Arrow Keys | Pan camera |
| Mouse Wheel | Zoom in/out |
| Escape | Exit build mode / Open menu |

## License

[TBD]
