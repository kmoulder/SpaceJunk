# Space Factory

A Factorio-inspired automation game set on a space station drifting through a debris field. Collect floating space junk, process raw materials into refined goods, and build an ever-expanding automated factory in the void of space.

## Current Status: Phase 1 Complete

**What's Working:**
- Debris spawns and drifts across the screen
- Click debris to collect resources into inventory
- Press 'I' to open/close 40-slot inventory
- Hotbar displays at bottom of screen
- WASD/arrows to pan camera, scroll to zoom
- 3x3 station foundation renders at center
- All core singletons operational (Game, Grid, Inventory, Crafting, Research, Power, Debris)
- Procedural pixel art generation for all sprites

**Remaining Phase 1 Items:**
- Crafting UI panel (recipes exist but no UI to trigger hand-crafting)
- Collection visual feedback (particles/sounds)

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

**Note:** On first run, you'll receive 20 Iron Ore, 15 Copper Ore, and 10 Coal as starting resources for testing.

## Documentation

- [Game Design Document](DESIGN.md) - Detailed game mechanics and systems
- [Technical Architecture](ARCHITECTURE.md) - Code structure and patterns
- [Implementation Roadmap](ROADMAP.md) - Phased development plan
- [Phase 2 Handoff](HANDOFF.md) - Guide for continuing development

## Controls

| Key | Action |
|-----|--------|
| Left Click | Collect debris / Place building |
| Right Click | Cancel placement / Remove building |
| I | Toggle inventory |
| E | Interact with building |
| WASD / Arrow Keys | Pan camera |
| Mouse Wheel | Zoom in/out |
| Escape | Open menu |

## License

[TBD]
