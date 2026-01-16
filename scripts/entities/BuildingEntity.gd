class_name BuildingEntity
extends Node2D

## BuildingEntity - Base class for all placeable buildings
##
## Provides common functionality for buildings including:
## - Grid positioning and rotation
## - Power network integration
## - Tick-based processing
## - Inventory management (for storage buildings)

signal building_destroyed(building: BuildingEntity)

## The building definition resource
var definition: BuildingResource

## Grid position of the building's origin tile
var grid_position: Vector2i = Vector2i.ZERO

## Rotation index (0=North, 1=East, 2=South, 3=West)
var rotation_index: int = 0

## Whether the building is powered (if it requires power)
var is_powered: bool = true

## Internal inventory for buildings with storage
var internal_inventory: Array[ItemStack] = []

## Current crafting progress (0.0 to 1.0) for processing buildings
var crafting_progress: float = 0.0

## Sprite for the building
var sprite: Sprite2D


func _ready() -> void:
	# Connect to game tick for processing
	GameManager.game_tick.connect(_on_game_tick)

	# Set up sprite if we have a definition
	if definition:
		_setup_sprite()
		_setup_inventory()
		_register_power()


## Initialize the building with a definition and position
func initialize(def: BuildingResource, pos: Vector2i, rotation: int = 0) -> void:
	definition = def
	grid_position = pos
	rotation_index = rotation

	# Position the building in world space
	position = GridManager.grid_to_world(pos)
	z_index = Constants.Z_BUILDINGS

	_setup_sprite()
	_setup_inventory()
	_register_power()


func _setup_sprite() -> void:
	if sprite:
		sprite.queue_free()

	sprite = Sprite2D.new()
	sprite.centered = false
	sprite.texture = _generate_texture()

	# Apply rotation
	sprite.rotation = rotation_index * PI / 2
	if rotation_index == 1:  # East
		sprite.position.x = definition.size.y * Constants.TILE_SIZE
	elif rotation_index == 2:  # South
		sprite.position.x = definition.size.x * Constants.TILE_SIZE
		sprite.position.y = definition.size.y * Constants.TILE_SIZE
	elif rotation_index == 3:  # West
		sprite.position.y = definition.size.x * Constants.TILE_SIZE

	add_child(sprite)


func _generate_texture() -> Texture2D:
	# Override in subclasses for custom textures
	return SpriteGenerator.generate_building(Color(0.4, 0.4, 0.5), definition.size)


func _setup_inventory() -> void:
	if definition.storage_slots > 0:
		internal_inventory.clear()
		for _i in range(definition.storage_slots):
			internal_inventory.append(ItemStack.new())


func _register_power() -> void:
	if definition.power_consumption > 0:
		PowerManager.register_consumer(self, definition.power_consumption)
	if definition.power_production > 0:
		PowerManager.register_producer(self, definition.power_production)


## Get the building definition
func get_definition() -> BuildingResource:
	return definition


## Get the current rotation direction
func get_direction() -> Enums.Direction:
	return rotation_index as Enums.Direction


## Rotate the building clockwise
func rotate_cw() -> void:
	if definition and definition.can_rotate:
		rotation_index = (rotation_index + 1) % 4
		_setup_sprite()


## Rotate the building counter-clockwise
func rotate_ccw() -> void:
	if definition and definition.can_rotate:
		rotation_index = (rotation_index + 3) % 4
		_setup_sprite()


## Called each game tick - override in subclasses
func _on_game_tick(_tick: int) -> void:
	if not is_powered and definition and definition.power_consumption > 0:
		return

	_process_building()


## Override in subclasses to implement building logic
func _process_building() -> void:
	pass


## Check if building can accept items (for inserters)
func can_accept_item(item: ItemResource, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if internal_inventory.is_empty():
		return false

	for slot in internal_inventory:
		if slot.is_empty() or (slot.item == item and not slot.is_full()):
			return true
	return false


## Insert an item into the building (returns true if successful)
func insert_item(item: ItemResource, count: int = 1, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if internal_inventory.is_empty():
		return false

	var remaining := count

	# Try existing stacks first
	for slot in internal_inventory:
		if slot.item == item and not slot.is_full():
			remaining = slot.add(remaining)
			if remaining <= 0:
				return true

	# Try empty slots
	for slot in internal_inventory:
		if slot.is_empty():
			slot.item = item
			remaining = slot.add(remaining)
			if remaining <= 0:
				return true

	return remaining < count


## Check if building has items to output (for inserters)
func has_output_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	# Override in subclasses to specify output logic
	return null


## Extract an item from the building (returns true if successful)
func extract_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	# Override in subclasses
	return null


## Get the total count of a specific item in internal inventory
func get_internal_item_count(item: ItemResource) -> int:
	var total := 0
	for slot in internal_inventory:
		if slot.item == item:
			total += slot.count
	return total


## Remove items from internal inventory
func remove_internal_item(item: ItemResource, count: int) -> bool:
	var remaining := count

	for slot in internal_inventory:
		if slot.item == item and remaining > 0:
			var removed := slot.remove(remaining)
			remaining -= removed
			if slot.count <= 0:
				slot.item = null

	return remaining <= 0


## Called when the building is removed
func on_removed() -> void:
	# Unregister from power network
	if definition:
		if definition.power_consumption > 0:
			PowerManager.unregister_consumer(self)
		if definition.power_production > 0:
			PowerManager.unregister_producer(self)

	# Disconnect from game tick
	if GameManager.game_tick.is_connected(_on_game_tick):
		GameManager.game_tick.disconnect(_on_game_tick)

	building_destroyed.emit(self)


func _exit_tree() -> void:
	on_removed()
