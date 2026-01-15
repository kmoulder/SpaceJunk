extends Node

## GridManager - Handles the station grid, building placement, and spatial queries
##
## Tracks which tiles have foundation, which tiles have buildings,
## and provides utilities for coordinate conversion.

signal foundation_added(pos: Vector2i)
signal foundation_removed(pos: Vector2i)
signal building_placed(pos: Vector2i, building: Node2D)
signal building_removed(pos: Vector2i, building: Node2D)

## Dictionary of foundation tile positions -> true
var foundation_tiles: Dictionary = {}

## Dictionary of tile positions -> building node
var buildings: Dictionary = {}

## Dictionary of tile positions -> building definition (for multi-tile buildings)
var building_origins: Dictionary = {}


func _ready() -> void:
	# Initialize starting 3x3 station
	_create_starting_station()


func _create_starting_station() -> void:
	# Create a 3x3 foundation centered at origin
	var half_size := Constants.STARTING_STATION_SIZE / 2
	for x in range(-half_size, half_size + 1):
		for y in range(-half_size, half_size + 1):
			add_foundation(Vector2i(x, y))


## Convert world position to grid position
func world_to_grid(world_pos: Vector2) -> Vector2i:
	return Vector2i(
		floori(world_pos.x / Constants.TILE_SIZE),
		floori(world_pos.y / Constants.TILE_SIZE)
	)


## Convert grid position to world position (top-left corner of tile)
func grid_to_world(grid_pos: Vector2i) -> Vector2:
	return Vector2(grid_pos) * Constants.TILE_SIZE


## Convert grid position to world position (center of tile)
func grid_to_world_center(grid_pos: Vector2i) -> Vector2:
	return grid_to_world(grid_pos) + Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE) * 0.5


## Check if a position has foundation
func has_foundation(pos: Vector2i) -> bool:
	return foundation_tiles.has(pos)


## Add foundation at a position
func add_foundation(pos: Vector2i) -> bool:
	if has_foundation(pos):
		return false
	foundation_tiles[pos] = true
	foundation_added.emit(pos)
	return true


## Remove foundation at a position (only if no building)
func remove_foundation(pos: Vector2i) -> bool:
	if not has_foundation(pos):
		return false
	if has_building(pos):
		return false
	foundation_tiles.erase(pos)
	foundation_removed.emit(pos)
	return true


## Check if a position has a building
func has_building(pos: Vector2i) -> bool:
	return buildings.has(pos)


## Get building at a position
func get_building(pos: Vector2i) -> Node2D:
	return buildings.get(pos, null)


## Check if a building can be placed at a position
func can_place_building(pos: Vector2i, building_def: BuildingResource) -> bool:
	# Check all tiles the building would occupy
	for offset in building_def.get_footprint():
		var check_pos := pos + offset
		# Must have foundation
		if not has_foundation(check_pos):
			return false
		# Must not have another building
		if has_building(check_pos):
			return false
	return true


## Place a building at a position
func place_building(pos: Vector2i, building: Node2D, building_def: BuildingResource) -> bool:
	if not can_place_building(pos, building_def):
		return false

	# Mark all tiles as occupied
	for offset in building_def.get_footprint():
		var tile_pos := pos + offset
		buildings[tile_pos] = building
		building_origins[tile_pos] = pos

	building_placed.emit(pos, building)
	return true


## Remove a building from the grid
func remove_building(pos: Vector2i) -> Node2D:
	var building := get_building(pos)
	if building == null:
		return null

	# Find the origin position
	var origin: Vector2i = building_origins.get(pos, pos)

	# Get the building definition to know its footprint
	var building_def: BuildingResource = null
	if building.has_method("get_definition"):
		building_def = building.get_definition()

	# Remove from all tiles
	if building_def:
		for offset in building_def.get_footprint():
			var tile_pos := origin + offset
			buildings.erase(tile_pos)
			building_origins.erase(tile_pos)
	else:
		# Fallback for simple 1x1 buildings
		buildings.erase(pos)
		building_origins.erase(pos)

	building_removed.emit(origin, building)
	return building


## Get all buildings in an area
func get_buildings_in_area(top_left: Vector2i, size: Vector2i) -> Array[Node2D]:
	var result: Array[Node2D] = []
	var seen := {}
	for x in range(size.x):
		for y in range(size.y):
			var pos := top_left + Vector2i(x, y)
			var building := get_building(pos)
			if building and not seen.has(building):
				seen[building] = true
				result.append(building)
	return result


## Check if a foundation tile can be added (must be adjacent to existing)
func can_add_foundation(pos: Vector2i) -> bool:
	if has_foundation(pos):
		return false
	# Check if adjacent to existing foundation
	for dir in [Vector2i.UP, Vector2i.DOWN, Vector2i.LEFT, Vector2i.RIGHT]:
		if has_foundation(pos + dir):
			return true
	return false


## Get all foundation positions
func get_all_foundation() -> Array[Vector2i]:
	var result: Array[Vector2i] = []
	for pos in foundation_tiles.keys():
		result.append(pos)
	return result


## Get the bounding box of the station
func get_station_bounds() -> Rect2i:
	if foundation_tiles.is_empty():
		return Rect2i(0, 0, 0, 0)

	var min_pos := Vector2i(999999, 999999)
	var max_pos := Vector2i(-999999, -999999)

	for pos in foundation_tiles.keys():
		min_pos.x = mini(min_pos.x, pos.x)
		min_pos.y = mini(min_pos.y, pos.y)
		max_pos.x = maxi(max_pos.x, pos.x)
		max_pos.y = maxi(max_pos.y, pos.y)

	return Rect2i(min_pos, max_pos - min_pos + Vector2i.ONE)


## Get adjacent building (for inserters, belts, etc.)
func get_adjacent_building(pos: Vector2i, direction: Enums.Direction) -> Node2D:
	var offset := Enums.direction_to_vector(direction)
	return get_building(pos + offset)


## Get the origin position of a building (for multi-tile buildings)
func get_building_origin(pos: Vector2i) -> Vector2i:
	return building_origins.get(pos, pos)
