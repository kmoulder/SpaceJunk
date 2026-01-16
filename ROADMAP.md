# Space Factory - Implementation Roadmap

## Overview

This document outlines the phased implementation plan for Space Factory. Each phase builds upon the previous, delivering playable milestones.

---

## Phase 1: Core Foundation *(Current)*

**Goal**: Playable prototype with manual resource collection and basic crafting

### 1.1 Project Setup
- [x] Create directory structure
- [x] Create README.md
- [x] Create DESIGN.md
- [x] Create ARCHITECTURE.md
- [x] Create ROADMAP.md
- [x] Initialize Godot 4.5 project
- [x] Configure project settings (window size, input maps)
- [x] Set up autoload singletons

### 1.2 Core Systems
- [x] GameManager (game state, pause, time)
- [x] GridManager (coordinate system, tile tracking)
- [x] SpriteGenerator (procedural pixel art)
- [x] InputManager (mouse/keyboard handling - integrated in Main.gd)

### 1.3 Basic Visuals
- [x] Starfield parallax background
- [x] Generate ore/debris sprites
- [x] Generate plate sprites
- [x] Station foundation tile sprite
- [x] Basic UI frame sprites

### 1.4 Debris System
- [x] DebrisEntity with drift movement
- [x] DebrisManager spawning system
- [x] Click-to-collect mechanic
- [x] Debris types: Iron, Copper, Stone, Coal, Scrap, Ice
- [ ] Visual feedback on collection (particles/animation)

### 1.5 Inventory System
- [x] InventoryManager singleton
- [x] ItemResource definition
- [x] ItemStack data class
- [x] Basic inventory storage
- [x] Inventory UI (toggle with 'I')
- [x] Hotbar display

### 1.6 Station Grid
- [x] 3x3 starting foundation
- [x] Grid overlay visualization
- [x] Camera controls (pan, zoom)

### 1.7 Hand Crafting
- [x] RecipeResource definition
- [x] CraftingManager singleton
- [x] Basic recipes (ore → plate, gears, cables, circuits)
- [ ] Crafting UI panel

**Phase 1 Deliverable**: Player can collect drifting debris, store items in inventory, and hand-craft basic materials.

---

## Phase 2: Basic Automation *(Complete)*

**Goal**: First automated production with buildings and conveyors

### 2.1 Building System
- [x] BuildingResource definition
- [x] BuildingEntity base class
- [x] Building placement mode
- [x] Building ghost preview
- [x] Rotation support (R key)
- [x] Building removal (right-click)
- [x] Building inventory component

### 2.2 First Buildings
- [x] Stone Furnace (smelts ore → plates)
- [x] Small Chest (basic storage)
- [x] Build menu UI

### 2.3 Transport: Conveyors
- [x] ConveyorBelt entity
- [x] Belt item movement
- [x] Belt connection logic
- [x] Belt sprite generation (animated)
- [ ] Belt placement in lines (drag to place multiple)

### 2.4 Transport: Inserters
- [x] Inserter entity
- [x] Pickup/drop mechanics
- [x] Swing animation
- [ ] Filter inserter variant

### 2.5 Crafting Expansion
- [x] Iron Gear Wheel recipe
- [x] Copper Cable recipe
- [x] Electronic Circuit recipe
- [x] Stone Brick recipe

### 2.6 Power System (Basic)
- [x] PowerManager singleton
- [ ] Power pole placement
- [x] Solar Panel building (defined, requires research)
- [ ] Power network visualization
- [x] Building power requirements

**Phase 2 Deliverable**: Player can build furnaces, chests, belts, and inserters to automate ore processing.

---

## Phase 3: Research & Expansion

**Goal**: Tech tree progression and station expansion

### 3.1 Research System
- [ ] TechnologyResource definition
- [ ] ResearchManager singleton
- [ ] Lab building
- [ ] Research UI (tech tree view)
- [ ] Research progress tracking

### 3.2 Science Packs
- [ ] Automation Science Pack (red)
- [ ] Logistic Science Pack (green)
- [ ] Science pack recipes
- [ ] Lab consumption mechanics

### 3.3 Station Expansion
- [ ] Foundation item
- [ ] Foundation crafting recipe
- [ ] Foundation placement on edges
- [ ] Station boundary detection

### 3.4 Assembler Building
- [ ] Assembler Mk1 entity
- [ ] Recipe selection UI
- [ ] Multi-input handling
- [ ] Crafting progress display

### 3.5 More Buildings
- [ ] Long Inserter
- [ ] Fast Inserter
- [ ] Underground Belt
- [ ] Splitter
- [ ] Medium Chest

### 3.6 Debris Collector
- [ ] Automated debris collection building
- [ ] Collection range visualization
- [ ] Output to adjacent belts/chests

**Phase 3 Deliverable**: Player can research technologies, automate science pack production, and expand the station.

---

## Phase 4: Advanced Production

**Goal**: Complex production chains and oil processing

### 4.1 Steel Processing
- [ ] Steel Plate recipe (5 iron → 1 steel)
- [ ] Electric Furnace building

### 4.2 Oil Processing
- [ ] Crude Oil canister debris type
- [ ] Fluid storage (tanks)
- [ ] Pipe building
- [ ] Refinery building
- [ ] Chemical Plant building
- [ ] Petroleum Gas
- [ ] Plastic Bar

### 4.3 Advanced Components
- [ ] Advanced Circuit recipe
- [ ] Engine Unit recipe
- [ ] Processing Unit recipe

### 4.4 Chemical Science
- [ ] Chemical Science Pack (blue)
- [ ] Sulfur production
- [ ] Sulfuric Acid

### 4.5 Power Expansion
- [ ] Accumulator building
- [ ] Substation building
- [ ] Steam Engine (fuel-powered)
- [ ] Better power UI

### 4.6 Assembler Mk2
- [ ] Handles 4-ingredient recipes
- [ ] 3x3 footprint
- [ ] Faster crafting speed

**Phase 4 Deliverable**: Player can process oil, craft advanced circuits, and produce blue science.

---

## Phase 5: Logistics & Polish

**Goal**: Robot logistics and quality-of-life features

### 5.1 Logistics Robots
- [ ] Flying Robot Frame component
- [ ] Roboport building
- [ ] Logistics Robot entity
- [ ] Construction Robot entity
- [ ] Logistic network zones

### 5.2 Logistic Chests
- [ ] Requester Chest
- [ ] Provider Chest
- [ ] Storage Chest
- [ ] Passive Provider Chest

### 5.3 Late-Game Science
- [ ] Production Science Pack (purple)
- [ ] Utility Science Pack (yellow)
- [ ] Associated recipes

### 5.4 Quality of Life
- [ ] Copy/paste building settings
- [ ] Blueprint system (save building layouts)
- [ ] Undo/redo for building
- [ ] Statistics panel (production rates)
- [ ] Alerts system

### 5.5 Save/Load
- [ ] SaveManager implementation
- [ ] Auto-save feature
- [ ] Save slot UI
- [ ] Load game UI

### 5.6 Audio
- [ ] Ambient space sounds
- [ ] Building placement sounds
- [ ] Collection sounds
- [ ] Research complete fanfare
- [ ] Background music

**Phase 5 Deliverable**: Full logistics network, save/load, and polished audio/visual experience.

---

## Phase 6: Endgame & Beyond

**Goal**: Victory condition and infinite replayability

### 6.1 Victory Condition
- [ ] Define endgame goal (satellite launch, portal construction, etc.)
- [ ] Final tier buildings
- [ ] Victory screen

### 6.2 Infinite Research
- [ ] Mining productivity
- [ ] Robot speed
- [ ] Worker robot capacity
- [ ] Research speed

### 6.3 Rare Resources
- [ ] Uranium processing
- [ ] Nuclear power
- [ ] Alien Artifacts usage

### 6.4 Advanced Features
- [ ] Module system (productivity, speed, efficiency)
- [ ] Beacons
- [ ] Express belts (3 tiers)

### 6.5 Polish & Optimization
- [ ] Performance optimization
- [ ] Memory management
- [ ] Large station support (1000+ buildings)

**Phase 6 Deliverable**: Complete game with victory condition and endgame content.

---

## Status Legend

- [ ] Not started
- [~] In progress
- [x] Completed
- [-] Skipped/Deferred

---

## Current Focus

**Active Phase**: Phase 3 - Research & Expansion

**Remaining from Phase 1/2**:
1. Collection feedback (particles/sounds when clicking debris)
2. Crafting UI panel for hand-crafting
3. Belt drag-placement (place multiple in a line)
4. Filter inserter variant
5. Power pole placement and network visualization

**Next Phase Preview** (Phase 3):
1. Research UI (tech tree panel)
2. Lab building for science pack consumption
3. Assembler building for automated crafting
4. Station expansion with foundation placement
5. Debris collector building

---

## Version History

| Version | Date | Description |
|---------|------|-------------|
| 0.1.0 | TBD | Phase 1 complete - Basic collection and inventory |
| 0.2.0 | TBD | Phase 2 complete - Basic automation |
| 0.3.0 | TBD | Phase 3 complete - Research and expansion |
| 0.4.0 | TBD | Phase 4 complete - Advanced production |
| 0.5.0 | TBD | Phase 5 complete - Logistics and polish |
| 1.0.0 | TBD | Phase 6 complete - Full release |
