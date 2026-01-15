class_name Constants
extends RefCounted

## Grid and tile settings
const TILE_SIZE: int = 32
const STARTING_STATION_SIZE: int = 3  # 3x3 starting grid

## Inventory settings
const PLAYER_INVENTORY_SLOTS: int = 40
const HOTBAR_SLOTS: int = 10
const DEFAULT_STACK_SIZE: int = 100
const ORE_STACK_SIZE: int = 50
const BUILDING_STACK_SIZE: int = 50

## Debris settings
const DEBRIS_BASE_SPAWN_RATE: float = 2.5  # seconds between spawns
const DEBRIS_SPAWN_VARIANCE: float = 1.0  # random variance in spawn time
const DEBRIS_MIN_SPEED: float = 20.0  # pixels per second
const DEBRIS_MAX_SPEED: float = 60.0
const DEBRIS_SPAWN_MARGIN: float = 100.0  # spawn this far outside screen
const DEBRIS_DESPAWN_MARGIN: float = 200.0  # despawn this far outside screen
const DEBRIS_CLICK_RADIUS: float = 24.0  # clickable radius

## Camera settings
const CAMERA_ZOOM_MIN: float = 0.5
const CAMERA_ZOOM_MAX: float = 2.0
const CAMERA_ZOOM_STEP: float = 0.1
const CAMERA_PAN_SPEED: float = 400.0  # pixels per second

## Building settings
const BUILDING_GHOST_ALPHA: float = 0.5
const INSERTER_SWING_TIME: float = 1.0  # seconds per swing
const BELT_SPEED_TIER_1: float = 1.0  # tiles per second
const BELT_SPEED_TIER_2: float = 2.0
const BELT_SPEED_TIER_3: float = 3.0

## Crafting settings
const HAND_CRAFT_SPEED_MULTIPLIER: float = 1.0
const ASSEMBLER_MK1_SPEED: float = 0.5
const ASSEMBLER_MK2_SPEED: float = 0.75
const ASSEMBLER_MK3_SPEED: float = 1.25
const FURNACE_STONE_SPEED: float = 1.0
const FURNACE_ELECTRIC_SPEED: float = 2.0

## Power settings
const SOLAR_PANEL_OUTPUT: float = 60.0  # kW
const ACCUMULATOR_CAPACITY: float = 5000.0  # kJ
const STEAM_ENGINE_OUTPUT: float = 900.0  # kW

## Research settings
const LAB_RESEARCH_SPEED: float = 1.0  # science packs per second

## Colors for procedural sprites
const COLOR_IRON_ORE := Color(0.55, 0.55, 0.6)
const COLOR_IRON_PLATE := Color(0.7, 0.7, 0.75)
const COLOR_COPPER_ORE := Color(0.8, 0.5, 0.3)
const COLOR_COPPER_PLATE := Color(0.9, 0.6, 0.4)
const COLOR_STONE := Color(0.65, 0.6, 0.55)
const COLOR_STONE_BRICK := Color(0.5, 0.45, 0.4)
const COLOR_COAL := Color(0.2, 0.2, 0.25)
const COLOR_STEEL := Color(0.5, 0.5, 0.55)
const COLOR_CIRCUIT_GREEN := Color(0.2, 0.7, 0.3)
const COLOR_CIRCUIT_RED := Color(0.8, 0.2, 0.2)
const COLOR_CIRCUIT_BLUE := Color(0.2, 0.4, 0.9)

## UI Colors
const UI_BACKGROUND := Color(0.15, 0.15, 0.2, 0.95)
const UI_BORDER := Color(0.4, 0.4, 0.5)
const UI_HIGHLIGHT := Color(0.3, 0.5, 0.8)
const UI_TEXT := Color(0.9, 0.9, 0.9)
const UI_TEXT_DIM := Color(0.6, 0.6, 0.6)

## Z-index layers
const Z_BACKGROUND: int = -100
const Z_FOUNDATION: int = 0
const Z_BELTS: int = 10
const Z_BUILDINGS: int = 20
const Z_ITEMS: int = 30
const Z_INSERTERS: int = 40
const Z_DEBRIS: int = 50
const Z_GHOST: int = 100
