extends Node

## BuildingManager - Handles building definitions, placement, and removal
##
## Manages the registry of building types and provides utilities for
## placing and removing buildings on the grid.

signal building_registered(building_def: BuildingResource)
signal building_placed(building: BuildingEntity)
signal building_removed(building: BuildingEntity)
signal build_mode_changed(enabled: bool, building_def: BuildingResource)

## All registered building resources by ID
var _building_registry: Dictionary = {}

## Currently selected building for placement (null if not in build mode)
var selected_building: BuildingResource = null

## Current rotation for placement
var placement_rotation: int = 0

## Ghost preview node
var ghost_preview: Node2D = null

## Reference to the buildings layer (set by Main)
var buildings_layer: Node2D = null


func _ready() -> void:
	_register_default_buildings()


## Register a building resource
func register_building(building_def: BuildingResource) -> void:
	if building_def and not building_def.id.is_empty():
		_building_registry[building_def.id] = building_def
		building_registered.emit(building_def)


## Get a building resource by ID
func get_building(building_id: String) -> BuildingResource:
	return _building_registry.get(building_id, null)


## Get all registered buildings
func get_all_buildings() -> Array[BuildingResource]:
	var result: Array[BuildingResource] = []
	for building in _building_registry.values():
		result.append(building)
	return result


## Get buildings by category
func get_buildings_by_category(category: Enums.BuildingCategory) -> Array[BuildingResource]:
	var result: Array[BuildingResource] = []
	for building in _building_registry.values():
		if building.category == category:
			result.append(building)
	return result


## Enter build mode with a specific building
func enter_build_mode(building_def: BuildingResource) -> void:
	selected_building = building_def
	placement_rotation = 0
	GameManager.set_game_state(Enums.GameState.BUILDING)
	build_mode_changed.emit(true, building_def)
	_create_ghost_preview()


## Exit build mode
func exit_build_mode() -> void:
	selected_building = null
	_remove_ghost_preview()
	GameManager.set_game_state(Enums.GameState.PLAYING)
	build_mode_changed.emit(false, null)


## Check if currently in build mode
func is_in_build_mode() -> bool:
	return selected_building != null


## Rotate placement clockwise
func rotate_placement_cw() -> void:
	if selected_building and selected_building.can_rotate:
		placement_rotation = (placement_rotation + 1) % 4
		_update_ghost_preview()


## Rotate placement counter-clockwise
func rotate_placement_ccw() -> void:
	if selected_building and selected_building.can_rotate:
		placement_rotation = (placement_rotation + 3) % 4
		_update_ghost_preview()


## Try to place building at grid position
func try_place_building(grid_pos: Vector2i) -> bool:
	if selected_building == null:
		return false

	if not GridManager.can_place_building(grid_pos, selected_building):
		return false

	# Check if player has resources
	if not _can_afford_building(selected_building):
		return false

	# Consume resources
	_consume_building_cost(selected_building)

	# Create the building
	var building := _create_building(selected_building, grid_pos, placement_rotation)
	if building == null:
		return false

	# Add to grid and scene
	if buildings_layer:
		buildings_layer.add_child(building)

	GridManager.place_building(grid_pos, building, selected_building)
	building_placed.emit(building)

	# Notify adjacent buildings
	_notify_neighbors(grid_pos, selected_building)

	return true


## Remove building at grid position
func remove_building(grid_pos: Vector2i) -> BuildingEntity:
	var building := GridManager.get_building(grid_pos)
	if building == null:
		return null

	if not building is BuildingEntity:
		return null

	var building_entity := building as BuildingEntity
	var origin := GridManager.get_building_origin(grid_pos)
	var building_def := building_entity.get_definition()

	# Remove from grid
	GridManager.remove_building(grid_pos)

	# Return items to player inventory (partial refund)
	if building_def:
		_refund_building(building_def)
		_notify_neighbors(origin, building_def)

	# Remove from scene
	building_entity.on_removed()
	building_entity.queue_free()

	building_removed.emit(building_entity)

	return building_entity


## Update ghost preview position
func update_ghost_position(grid_pos: Vector2i) -> void:
	if ghost_preview == null:
		return

	ghost_preview.position = GridManager.grid_to_world(grid_pos)

	# Update color based on placement validity
	var can_place := GridManager.can_place_building(grid_pos, selected_building)
	can_place = can_place and _can_afford_building(selected_building)

	var sprite := ghost_preview.get_node_or_null("Sprite") as Sprite2D
	if sprite:
		if can_place:
			sprite.modulate = Color(0.5, 1.0, 0.5, Constants.BUILDING_GHOST_ALPHA)
		else:
			sprite.modulate = Color(1.0, 0.5, 0.5, Constants.BUILDING_GHOST_ALPHA)


func _create_ghost_preview() -> void:
	_remove_ghost_preview()

	if selected_building == null:
		return

	ghost_preview = Node2D.new()
	ghost_preview.z_index = Constants.Z_GHOST

	var sprite := Sprite2D.new()
	sprite.name = "Sprite"
	sprite.centered = false
	sprite.texture = _get_building_texture(selected_building)
	sprite.modulate = Color(1.0, 1.0, 1.0, Constants.BUILDING_GHOST_ALPHA)

	# Apply rotation
	sprite.rotation = placement_rotation * PI / 2
	if placement_rotation == 1:  # East
		sprite.position.x = selected_building.size.y * Constants.TILE_SIZE
	elif placement_rotation == 2:  # South
		sprite.position.x = selected_building.size.x * Constants.TILE_SIZE
		sprite.position.y = selected_building.size.y * Constants.TILE_SIZE
	elif placement_rotation == 3:  # West
		sprite.position.y = selected_building.size.x * Constants.TILE_SIZE

	ghost_preview.add_child(sprite)

	if buildings_layer:
		buildings_layer.add_child(ghost_preview)


func _update_ghost_preview() -> void:
	if ghost_preview == null:
		return

	var sprite := ghost_preview.get_node_or_null("Sprite") as Sprite2D
	if sprite == null:
		return

	# Update rotation
	sprite.rotation = placement_rotation * PI / 2
	sprite.position = Vector2.ZERO

	if placement_rotation == 1:  # East
		sprite.position.x = selected_building.size.y * Constants.TILE_SIZE
	elif placement_rotation == 2:  # South
		sprite.position.x = selected_building.size.x * Constants.TILE_SIZE
		sprite.position.y = selected_building.size.y * Constants.TILE_SIZE
	elif placement_rotation == 3:  # West
		sprite.position.y = selected_building.size.x * Constants.TILE_SIZE


func _remove_ghost_preview() -> void:
	if ghost_preview:
		ghost_preview.queue_free()
		ghost_preview = null


func _get_building_texture(building_def: BuildingResource) -> Texture2D:
	match building_def.id:
		"stone_furnace":
			return SpriteGenerator.generate_furnace(false)
		"electric_furnace":
			return SpriteGenerator.generate_furnace(true)
		"small_chest":
			return SpriteGenerator.generate_chest(Color(0.6, 0.5, 0.3))
		"transport_belt":
			return SpriteGenerator.generate_belt(placement_rotation as Enums.Direction)
		"inserter":
			return SpriteGenerator.generate_inserter(false)
		"long_inserter":
			return SpriteGenerator.generate_inserter(true)
		"solar_panel":
			return SpriteGenerator.generate_solar_panel()
		_:
			return SpriteGenerator.generate_building(Color(0.4, 0.4, 0.5), building_def.size)


func _create_building(building_def: BuildingResource, pos: Vector2i, rotation: int) -> BuildingEntity:
	var building: BuildingEntity = null

	match building_def.id:
		"stone_furnace":
			building = StoneFurnace.new()
		"small_chest":
			building = SmallChest.new()
		"transport_belt":
			building = ConveyorBelt.new()
		"inserter":
			building = Inserter.new()
		"long_inserter":
			var inserter := Inserter.new()
			inserter.is_long = true
			building = inserter
		_:
			building = BuildingEntity.new()

	if building:
		building.initialize(building_def, pos, rotation)

	return building


func _can_afford_building(building_def: BuildingResource) -> bool:
	var cost := building_def.get_build_cost()
	for entry in cost:
		var item_id: String = entry["item_id"]
		var count: int = entry["count"]
		var item := InventoryManager.get_item(item_id)
		if item == null or not InventoryManager.has_item(item, count):
			return false
	return true


func _consume_building_cost(building_def: BuildingResource) -> void:
	var cost := building_def.get_build_cost()
	for entry in cost:
		var item_id: String = entry["item_id"]
		var count: int = entry["count"]
		var item := InventoryManager.get_item(item_id)
		if item:
			InventoryManager.remove_item(item, count)


func _refund_building(building_def: BuildingResource) -> void:
	# Refund 100% of materials for now
	var cost := building_def.get_build_cost()
	for entry in cost:
		var item_id: String = entry["item_id"]
		var count: int = entry["count"]
		var item := InventoryManager.get_item(item_id)
		if item:
			InventoryManager.add_item(item, count)


func _notify_neighbors(pos: Vector2i, building_def: BuildingResource) -> void:
	# Notify adjacent buildings that something changed
	var footprint := building_def.get_footprint()

	for offset: Vector2i in footprint:
		var tile_pos: Vector2i = pos + offset
		for dir: Vector2i in [Vector2i.UP, Vector2i.DOWN, Vector2i.LEFT, Vector2i.RIGHT]:
			var neighbor_pos: Vector2i = tile_pos + dir
			var neighbor := GridManager.get_building(neighbor_pos)
			if neighbor and neighbor.has_method("on_neighbor_changed"):
				neighbor.on_neighbor_changed()


## Register default buildings
func _register_default_buildings() -> void:
	# Stone Furnace
	var stone_furnace := BuildingResource.new()
	stone_furnace.id = "stone_furnace"
	stone_furnace.name = "Stone Furnace"
	stone_furnace.description = "Smelts ores into plates using coal as fuel"
	stone_furnace.size = Vector2i(2, 2)
	stone_furnace.category = Enums.BuildingCategory.PROCESSING
	stone_furnace.crafting_speed = Constants.FURNACE_STONE_SPEED
	stone_furnace.max_ingredients = 2
	stone_furnace.build_cost_ids = ["stone"]
	stone_furnace.build_cost_counts = [5]
	register_building(stone_furnace)

	# Small Chest
	var small_chest := BuildingResource.new()
	small_chest.id = "small_chest"
	small_chest.name = "Small Chest"
	small_chest.description = "Stores items. 16 slots."
	small_chest.size = Vector2i(1, 1)
	small_chest.category = Enums.BuildingCategory.STORAGE
	small_chest.storage_slots = 16
	small_chest.can_rotate = false
	small_chest.build_cost_ids = ["iron_plate"]
	small_chest.build_cost_counts = [2]
	register_building(small_chest)

	# Transport Belt
	var transport_belt := BuildingResource.new()
	transport_belt.id = "transport_belt"
	transport_belt.name = "Transport Belt"
	transport_belt.description = "Moves items in a direction"
	transport_belt.size = Vector2i(1, 1)
	transport_belt.category = Enums.BuildingCategory.TRANSPORT
	transport_belt.build_cost_ids = ["iron_gear", "iron_plate"]
	transport_belt.build_cost_counts = [1, 1]
	register_building(transport_belt)

	# Inserter
	var inserter := BuildingResource.new()
	inserter.id = "inserter"
	inserter.name = "Inserter"
	inserter.description = "Moves items between buildings"
	inserter.size = Vector2i(1, 1)
	inserter.category = Enums.BuildingCategory.TRANSPORT
	inserter.build_cost_ids = ["iron_gear", "iron_plate", "electronic_circuit"]
	inserter.build_cost_counts = [1, 1, 1]
	register_building(inserter)

	# Long Inserter
	var long_inserter := BuildingResource.new()
	long_inserter.id = "long_inserter"
	long_inserter.name = "Long Inserter"
	long_inserter.description = "Moves items over 2 tiles"
	long_inserter.size = Vector2i(1, 1)
	long_inserter.category = Enums.BuildingCategory.TRANSPORT
	long_inserter.required_technology = "automation"
	long_inserter.build_cost_ids = ["iron_gear", "iron_plate", "electronic_circuit"]
	long_inserter.build_cost_counts = [1, 1, 1]
	register_building(long_inserter)

	# Solar Panel
	var solar_panel := BuildingResource.new()
	solar_panel.id = "solar_panel"
	solar_panel.name = "Solar Panel"
	solar_panel.description = "Generates power from starlight"
	solar_panel.size = Vector2i(2, 2)
	solar_panel.category = Enums.BuildingCategory.POWER
	solar_panel.power_production = Constants.SOLAR_PANEL_OUTPUT
	solar_panel.can_rotate = false
	solar_panel.required_technology = "solar_energy"
	solar_panel.build_cost_ids = ["steel_plate", "electronic_circuit", "copper_plate"]
	solar_panel.build_cost_counts = [5, 15, 5]
	register_building(solar_panel)
