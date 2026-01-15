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

## Phase 2: Basic Automation

**Goal**: First automated production with buildings and conveyors

### 2.1 Building System
- [ ] BuildingResource definition
- [ ] BuildingEntity base class
- [ ] Building placement mode
- [ ] Building ghost preview
- [ ] Rotation support (R key)
- [ ] Building removal (right-click)
- [ ] Building inventory component

### 2.2 First Buildings
- [ ] Stone Furnace (smelts ore → plates)
- [ ] Small Chest (basic storage)
- [ ] Build menu UI

### 2.3 Transport: Conveyors
- [ ] ConveyorBelt entity
- [ ] Belt item movement
- [ ] Belt connection logic
- [ ] Belt sprite generation (animated)
- [ ] Belt placement in lines

### 2.4 Transport: Inserters
- [ ] Inserter entity
- [ ] Pickup/drop mechanics
- [ ] Swing animation
- [ ] Filter inserter variant

### 2.5 Crafting Expansion
- [ ] Iron Gear Wheel recipe
- [ ] Copper Cable recipe
- [ ] Electronic Circuit recipe
- [ ] Stone Brick recipe

### 2.6 Power System (Basic)
- [ ] PowerManager singleton
- [ ] Power pole placement
- [ ] Solar Panel building
- [ ] Power network visualization
- [ ] Building power requirements

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

**Active Phase**: Phase 1 - Core Foundation (Nearly Complete)

**Remaining Phase 1 Tasks**:
1. Collection feedback (particles/sounds when clicking debris)
2. Crafting UI panel for hand-crafting

**Next Phase Preview** (Phase 2):
1. Building placement system
2. Stone Furnace implementation
3. Conveyor belts and inserters

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
