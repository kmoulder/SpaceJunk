@tool
class_name BuildingResource
extends Resource

## Unique identifier for this building
@export var id: String = ""

## Display name
@export var name: String = ""

## Description shown in tooltips
@export_multiline var description: String = ""

## Building size in grid tiles
@export var size: Vector2i = Vector2i(1, 1)

## Building category
@export var category: Enums.BuildingCategory = Enums.BuildingCategory.PROCESSING

## Power consumption in kW (0 if none)
@export var power_consumption: float = 0.0

## Power production in kW (0 if none)
@export var power_production: float = 0.0

## Build cost item IDs and counts
@export var build_cost_ids: Array[String] = []
@export var build_cost_counts: Array[int] = []

## Technology required to unlock (empty = always available)
@export var required_technology: String = ""

## Whether this building can be rotated
@export var can_rotate: bool = true

## The scene to instantiate for this building
@export var scene_path: String = ""

## Crafting speed multiplier (for assemblers/furnaces)
@export var crafting_speed: float = 1.0

## Maximum number of ingredient types this building can handle
@export var max_ingredients: int = 2

## Storage capacity (for chests)
@export var storage_slots: int = 0

## Fluid storage capacity (for tanks)
@export var fluid_capacity: float = 0.0

## Collection range (for debris collectors)
@export var collection_range: float = 0.0

## Get build cost as an array of dictionaries
func get_build_cost() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for i in range(mini(build_cost_ids.size(), build_cost_counts.size())):
		result.append({
			"item_id": build_cost_ids[i],
			"count": build_cost_counts[i]
		})
	return result

## Get the footprint positions relative to origin (0,0)
func get_footprint() -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for x in range(size.x):
		for y in range(size.y):
			result.append(Vector2i(x, y))
	return result

## Get the center offset for this building
func get_center_offset() -> Vector2:
	return Vector2(size) * Constants.TILE_SIZE * 0.5

func _to_string() -> String:
	return "Building<%s>" % id
