# Space Factory - Game Design Document

## Overview

Space Factory is a 2D top-down automation game inspired by Factorio, set on a space station floating through an endless debris field. Players progress from manual resource collection to fully automated production lines.

---

## Core Gameplay Loop

1. **Collect** - Gather drifting space debris (manually at first, automated later)
2. **Process** - Smelt raw materials into refined products
3. **Craft** - Combine materials into components and structures
4. **Build** - Place buildings on the station grid
5. **Research** - Unlock new technologies with science
6. **Expand** - Add foundation tiles to grow the station

---

## The Debris Field

### Behavior
- Debris drifts across the screen from various directions
- Spawn rate and variety increases as the game progresses
- Debris outside the collection range despawns after leaving the screen
- Different debris types have different drift speeds

### Debris Types

| Debris | Rarity | Yields | Notes |
|--------|--------|--------|-------|
| Iron Asteroid | Common | 1-3 Iron Ore | Gray rocky chunks |
| Copper Asteroid | Common | 1-3 Copper Ore | Orange-brown rocks |
| Stone Asteroid | Common | 1-2 Stone | Light gray chunks |
| Coal Asteroid | Uncommon | 1-3 Coal | Black crystalline rocks |
| Metal Scrap | Common | 1-2 Scrap Metal | Twisted metal pieces |
| Ice Chunk | Uncommon | 1-2 Ice | Blue-white crystals |
| Uranium Asteroid | Rare | 1 Uranium Ore | Glowing green rocks |
| Oil Canister | Rare | 10-25 Crude Oil | Floating containers |
| Ancient Tech | Very Rare | 1 Alien Artifact | Mysterious objects |

---

## Resource Processing Chain

### Tier 1: Basic Materials
```
Iron Ore ──────► Iron Plate (Furnace)
Copper Ore ────► Copper Plate (Furnace)
Stone ─────────► Stone Brick (Furnace)
Coal ──────────► [Fuel / Carbon] (Direct use or processed)
Scrap Metal ───► Iron Ore + Copper Ore (Recycler)
Ice ───────────► Water (Heater)
```

### Tier 2: Basic Components
```
Iron Plate ─────────────► Iron Gear Wheel (Assembler)
Copper Plate ───────────► Copper Cable (Assembler)
Iron Plate + Copper Cable ► Electronic Circuit (Assembler)
Stone Brick + Iron Plate ─► Foundation (Assembler)
```

### Tier 3: Intermediate Components
```
Electronic Circuit + Copper Cable + Plastic ► Advanced Circuit (Assembler)
Iron Plate + Steel Plate ──────────────────► Engine Unit (Assembler)
Iron Plate ────────────────────────────────► Steel Plate (Furnace, 5:1)
Crude Oil ─────────────────────────────────► Petroleum/Plastic (Refinery)
```

### Tier 4: Advanced Components
```
Advanced Circuit + Electronic Circuit + Sulfuric Acid ► Processing Unit (Assembler)
Engine Unit + Electronic Circuit + Steel Plate ───────► Flying Robot Frame (Assembler)
Steel Plate + Copper Plate + Plastic ─────────────────► Low Density Structure (Assembler)
```

---

## Building Types

### Collection Buildings
| Building | Function | Inputs | Outputs |
|----------|----------|--------|---------|
| Collector | Auto-collects debris in range with animated arm, 4 output slots | Power | Raw materials |
| Magnet Array | Pulls debris toward station | Power | Attracted debris |
| Tractor Beam | Long-range targeted collection | Power | Selected debris |

### Transport Buildings
| Building | Function | Size |
|----------|----------|------|
| Conveyor Belt | Moves items in one direction | 1x1 |
| Underground Belt | Moves items under obstacles | 1x1 (pair) |
| Splitter | Divides/merges belt lanes | 1x2 |
| Inserter | Moves items between buildings | 1x1 |
| Long Inserter | Moves items over 2 tiles | 1x1 |
| Filter Inserter | Moves only specified items | 1x1 |

### Processing Buildings
| Building | Function | Size | Power |
|----------|----------|------|-------|
| Stone Furnace | Smelts ore to plates (slow) | 2x2 | Coal |
| Electric Furnace | Smelts ore to plates (fast) | 2x2 | Electric |
| Assembler Mk1 | Crafts recipes (2 ingredients) | 2x2 | Electric |
| Assembler Mk2 | Crafts recipes (4 ingredients) | 3x3 | Electric |
| Assembler Mk3 | Crafts recipes (6 ingredients) | 3x3 | Electric |
| Chemical Plant | Processes fluids and chemicals | 3x3 | Electric |
| Refinery | Processes crude oil | 5x5 | Electric |
| Recycler | Breaks down scrap | 2x2 | Electric |

### Storage Buildings
| Building | Function | Capacity |
|----------|----------|----------|
| Small Chest | Basic storage | 16 stacks |
| Medium Chest | Expanded storage | 32 stacks |
| Large Chest | Maximum storage | 48 stacks |
| Fluid Tank | Stores liquids | 25,000 units |

### Power Buildings
| Building | Function | Output |
|----------|----------|--------|
| Solar Panel | Generates power from starlight | 60 kW |
| Accumulator | Stores excess power | 5 MJ |
| Steam Engine | Burns fuel for power | 900 kW |
| Reactor | Nuclear power (late game) | 40 MW |
| Power Pole | Distributes electricity | - |
| Substation | Wide-area distribution | - |

### Research Buildings
| Building | Function |
|----------|----------|
| Lab | Consumes science packs for research |
| Beacon | Broadcasts module effects |

### Logistics Buildings (Late Game)
| Building | Function |
|----------|----------|
| Roboport | Base for logistics robots |
| Requester Chest | Requests items from network |
| Provider Chest | Provides items to network |

---

## Science Packs (Research)

### Science Pack Types

| Pack | Color | Ingredients | Unlocks |
|------|-------|-------------|---------|
| Automation Science | Red | Iron Gear + Copper Plate | Basic automation |
| Logistic Science | Green | Inserter + Belt | Transport systems |
| Military Science | Black | Ammo + Wall + Grenade | Defense (if implemented) |
| Chemical Science | Blue | Adv Circuit + Engine + Sulfur | Oil processing |
| Production Science | Purple | Rail + E-Furnace + Prod Module | Advanced production |
| Utility Science | Yellow | Processing Unit + Robot Frame + LDS | High tech |
| Space Science | White | Launched satellite | Infinite research |

### Research Tree (Simplified)

```
START
  │
  ├─► Automation 1 (unlocks: Assembler Mk1, Long Inserter)
  │     │
  │     └─► Automation 2 (unlocks: Assembler Mk2, Fast Inserter)
  │           │
  │           └─► Automation 3 (unlocks: Assembler Mk3)
  │
  ├─► Logistics 1 (unlocks: Underground Belt, Splitter)
  │     │
  │     └─► Logistics 2 (unlocks: Fast Belts)
  │           │
  │           └─► Logistics 3 (unlocks: Express Belts)
  │
  ├─► Electronics (unlocks: Electronic Circuit recipe)
  │     │
  │     └─► Advanced Electronics (unlocks: Advanced Circuit)
  │           │
  │           └─► Advanced Electronics 2 (unlocks: Processing Unit)
  │
  ├─► Steel Processing (unlocks: Steel Plate recipe)
  │
  ├─► Electric Energy Distribution (unlocks: Substations)
  │
  ├─► Oil Processing (unlocks: Refinery, Chemical Plant)
  │     │
  │     └─► Advanced Oil Processing
  │           │
  │           └─► Plastics (unlocks: Plastic Bar)
  │
  ├─► Engine (unlocks: Engine Unit)
  │
  ├─► Solar Energy (unlocks: Solar Panel)
  │     │
  │     └─► Electric Accumulator
  │
  ├─► Robotics (unlocks: Flying Robot Frame)
  │     │
  │     └─► Logistic System (unlocks: Roboport, Logistic Robots)
  │
  └─► Station Expansion (unlocks: Foundation crafting)
```

---

## The Space Station

### Grid System
- Each tile is 32x32 pixels
- Buildings snap to grid
- Starting size: 3x3 tiles (96x96 pixels of buildable area)
- Can expand by placing Foundation tiles on edges

### Building Placement Rules
1. Buildings must be placed on foundation tiles
2. Buildings cannot overlap
3. Some buildings have connection requirements (e.g., belts must connect)
4. Buildings can be rotated with 'R' key before placement
5. Buildings can be removed and returned to inventory

### Station Expansion
- Foundation tiles must be adjacent to existing station
- Foundation recipe: 2 Stone Brick + 1 Iron Plate
- No limit to station size (performance-limited)

---

## Inventory System

### Player Inventory
- Grid-based inventory (accessible with 'I')
- 40 slots (4 rows x 10 columns)
- Items stack (most to 100, some to 50)
- Hotbar: 10 quick-access slots at bottom of screen

### Stack Sizes
| Category | Stack Size |
|----------|------------|
| Ores | 50 |
| Plates | 100 |
| Components | 100 |
| Buildings | 50 |
| Fluids | N/A (stored in tanks) |

---

## UI Elements

### Main HUD
- Hotbar (bottom)
- Resource counters (top-left)
- Minimap (top-right)
- Building info panel (right side when selecting)

### Popup Menus
- Inventory (I)
- Crafting menu (C or in inventory)
- Research tree (T)
- Recipe list (R)
- Building recipes (when building selected)
- Settings (Escape)

### Crafting Features
- **Direct Crafting**: Click "Craft" when you have all ingredients
- **Recursive Crafting**: Click "Craft+" (blue button) to auto-craft missing intermediate items
  - Example: Craft Electronic Circuit with only copper plates and iron plates
  - System automatically queues Copper Cable first, then the circuit
- **Batch Crafting**: x5 button to queue 5 items at once
- **Visual Feedback**:
  - Green button = direct craft possible
  - Blue button = will craft intermediates first
  - Gray button = missing raw materials

---

## Progression Milestones

1. **First Steps**: Collect 10 pieces of debris
2. **Smelting**: Build and use a Stone Furnace
3. **Automation Begins**: Place first Assembler
4. **Power Up**: Establish electric power grid
5. **Moving Parts**: Create first conveyor system
6. **Growth**: Expand station with first Foundation
7. **Red Science**: Produce Automation Science Packs
8. **Green Science**: Produce Logistic Science Packs
9. **Oil Baron**: Process first crude oil
10. **Blue Science**: Produce Chemical Science Packs
11. **Robots**: Deploy first logistics robot
12. **Megabase**: Station reaches 100 tiles

---

## Balance Considerations

### Crafting Times (Base)
| Tier | Time Range |
|------|------------|
| Basic (plates) | 1-2 seconds |
| Components | 2-5 seconds |
| Intermediate | 5-10 seconds |
| Advanced | 10-30 seconds |
| Buildings | 2-10 seconds |

### Power Consumption
| Tier | Range |
|------|-------|
| Basic buildings | 50-100 kW |
| Standard machines | 100-200 kW |
| Advanced machines | 200-500 kW |
| Heavy industry | 500+ kW |

### Debris Spawn Rate
- Base rate: 1 debris every 2-3 seconds
- Increases with research progress
- Rare debris: ~5% chance
- Very rare: ~1% chance
