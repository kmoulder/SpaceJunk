class_name Enums
extends RefCounted

## Item categories for organization and filtering
enum ItemCategory {
	RAW_MATERIAL,
	PROCESSED,
	COMPONENT,
	INTERMEDIATE,
	BUILDING,
	SCIENCE,
	FLUID
}

## Building categories for the build menu
enum BuildingCategory {
	COLLECTION,
	TRANSPORT,
	PROCESSING,
	STORAGE,
	POWER,
	RESEARCH,
	LOGISTICS,
	FOUNDATION
}

## Recipe categories for crafting menu organization
enum RecipeCategory {
	SMELTING,
	CRAFTING,
	CHEMISTRY,
	REFINING
}

## Building crafting type requirements
enum CraftingType {
	HAND,
	FURNACE,
	ASSEMBLER,
	CHEMICAL_PLANT,
	REFINERY
}

## Game states
enum GameState {
	MENU,
	PLAYING,
	PAUSED,
	BUILDING,
	INVENTORY
}

## Debris rarity tiers
enum DebrisRarity {
	COMMON,
	UNCOMMON,
	RARE,
	VERY_RARE
}

## Directions for buildings and belts
enum Direction {
	NORTH,
	EAST,
	SOUTH,
	WEST
}

## Helper to get direction vector
static func direction_to_vector(dir: Direction) -> Vector2i:
	match dir:
		Direction.NORTH: return Vector2i(0, -1)
		Direction.EAST: return Vector2i(1, 0)
		Direction.SOUTH: return Vector2i(0, 1)
		Direction.WEST: return Vector2i(-1, 0)
	return Vector2i.ZERO

## Helper to rotate direction clockwise
static func rotate_direction_cw(dir: Direction) -> Direction:
	return (dir + 1) % 4 as Direction

## Helper to rotate direction counter-clockwise
static func rotate_direction_ccw(dir: Direction) -> Direction:
	return (dir + 3) % 4 as Direction

## Helper to get opposite direction
static func opposite_direction(dir: Direction) -> Direction:
	return (dir + 2) % 4 as Direction
