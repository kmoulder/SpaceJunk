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
- [x] Crafting UI panel (RecipeUI.cs, toggle with 'C')

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

## Phase 3: Research & Expansion *(Mostly Complete)*

**Goal**: Tech tree progression and station expansion

### 3.1 Research System
- [x] TechnologyResource definition (scripts/csharp/TechnologyResource.cs)
- [x] ResearchManager singleton (8 technologies defined with prerequisites)
- [x] Lab building (Lab.cs, 2x2, consumes science packs)
- [x] Research UI (ResearchUI.cs, toggle with 'T')
- [x] Research progress tracking (AddScience(), progress bar)
- [x] Research completion notifications in HUD

### 3.2 Science Packs
- [x] Automation Science Pack (red) - item registered
- [x] Logistic Science Pack (green) - item registered
- [x] Science pack recipes (CraftingManager: copper_plate+iron_gear, inserter+transport_belt)
- [x] Lab consumption mechanics (1 pack per second when researching)

### 3.3 Station Expansion
- [x] Foundation item
- [x] Foundation crafting recipe
- [x] Foundation placement on edges (adjacent to existing station)
- [x] Foundation category in build menu
- [x] Station boundary detection

### 3.4 Assembler Building
- [x] Assembler Mk1 entity (Assembler.cs, tier system)
- [x] Recipe selection UI (in BuildingUI)
- [x] Recipe requirements display with color-coded ingredients
- [x] Multi-input handling (4 input slots)
- [x] Crafting progress display (GetCraftingProgress())

### 3.5 More Buildings
- [x] Long Inserter (unlocked by automation_1 research)
- [ ] Fast Inserter
- [ ] Underground Belt
- [ ] Splitter
- [ ] Medium Chest

### 3.6 Debris Collector
- [x] Automated debris collection building (Collector.cs)
- [x] Animated robotic arm (Extending, Grabbing, Retracting states)
- [x] 4 output slots (stops when full, encourages automation)
- [x] Output via inserters or manual extraction
- [x] Collection range based on tier (Constants.CollectorTier1Range)

### 3.7 UI Improvements (Added)
- [x] Draggable windows (BuildingUI, InventoryUI, RecipeUI, ResearchUI)
- [x] X close buttons on all windows
- [x] Craft queue display in HUD with progress
- [x] x5 batch crafting (Shift+click)
- [x] Recipe list view (press R)
- [x] Game continues during build mode
- [x] Research unlock refresh in build menu
- [x] Recursive crafting (auto-craft intermediates when raw materials available)

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

**Active Phase**: Phase 3 - Research & Expansion (Mostly Complete)

**Completed in Phase 3**:
1. ✓ Research system (TechnologyResource, ResearchManager with 8 techs)
2. ✓ Research UI (ResearchUI.cs, toggle with 'T', shows tech tree with progress)
3. ✓ Research completion notifications in HUD
4. ✓ Lab building (2x2, consumes science packs, feeds ResearchManager)
5. ✓ Science packs (red/green items and crafting recipes)
6. ✓ Assembler Mk1 with recipe selection UI and ingredient requirements display
7. ✓ Long Inserter (unlocked via automation_1 research)
8. ✓ Crafting UI (RecipeUI.cs, toggle with 'C', hand-crafting panel)
9. ✓ x5 batch crafting (Shift+click)
10. ✓ Craft queue display in HUD
11. ✓ Debris Collector building with animated arm and 4 output slots
12. ✓ Foundation placement for station expansion
13. ✓ Draggable windows with X close buttons
14. ✓ Recipe list view (press R)
15. ✓ Recursive crafting (auto-craft intermediates like Factorio)

**Remaining from Phase 1/2**:
1. Collection feedback (particles/sounds when clicking debris)
2. Belt drag-placement (place multiple in a line)
3. Filter inserter variant
4. Power pole placement and network visualization

**Remaining in Phase 3**:
1. Fast Inserter
2. Underground Belt
3. Splitter
4. Medium Chest

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
