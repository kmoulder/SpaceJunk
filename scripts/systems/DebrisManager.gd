extends Node

## DebrisManager - Handles spawning and managing drifting debris
##
## Spawns debris at screen edges, manages their movement, and handles collection.

const DebrisEntityScript = preload("res://scripts/entities/DebrisEntity.gd")

signal debris_spawned(debris: Node2D)
signal debris_collected(debris: Node2D, items: Array)
signal debris_despawned(debris: Node2D)

## Active debris entities
var active_debris: Array[Node2D] = []

## Debris spawn timer
var spawn_timer: float = 0.0

## Current spawn rate (seconds between spawns)
var spawn_rate: float = Constants.DEBRIS_BASE_SPAWN_RATE

## Reference to the debris container node
var debris_container: Node2D = null

## Screen/viewport bounds for spawning
var spawn_bounds: Rect2 = Rect2()

## Debris type weights for spawning (type -> weight)
var debris_weights: Dictionary = {
	"iron_asteroid": 30,
	"copper_asteroid": 25,
	"stone_asteroid": 20,
	"coal_asteroid": 15,
	"scrap_metal": 20,
	"ice_chunk": 10
}


func _ready() -> void:
	# Connect to game manager for tick updates
	if GameManager:
		GameManager.game_tick.connect(_on_game_tick)

	# Update spawn bounds when viewport changes
	get_viewport().size_changed.connect(_update_spawn_bounds)
	_update_spawn_bounds()


func _update_spawn_bounds() -> void:
	var viewport_size := get_viewport().get_visible_rect().size
	spawn_bounds = Rect2(
		-Constants.DEBRIS_SPAWN_MARGIN,
		-Constants.DEBRIS_SPAWN_MARGIN,
		viewport_size.x + Constants.DEBRIS_SPAWN_MARGIN * 2,
		viewport_size.y + Constants.DEBRIS_SPAWN_MARGIN * 2
	)


func _on_game_tick(_tick: int) -> void:
	# Debris spawning is handled in _process for smoother timing
	pass


func _process(delta: float) -> void:
	if not GameManager.is_playing():
		return

	# Update spawn timer
	spawn_timer -= delta * GameManager.game_speed
	if spawn_timer <= 0:
		spawn_debris()
		# Randomize next spawn time
		var variance := randf_range(-Constants.DEBRIS_SPAWN_VARIANCE, Constants.DEBRIS_SPAWN_VARIANCE)
		spawn_timer = spawn_rate + variance

	# Update and check debris
	_update_debris(delta)


func _update_debris(delta: float) -> void:
	var to_remove: Array[Node2D] = []

	for debris in active_debris:
		if not is_instance_valid(debris):
			to_remove.append(debris)
			continue

		# Move debris
		if debris.has_method("update_movement"):
			debris.update_movement(delta * GameManager.game_speed)

		# Check if debris is off-screen (despawn)
		var despawn_rect := Rect2(
			-Constants.DEBRIS_DESPAWN_MARGIN,
			-Constants.DEBRIS_DESPAWN_MARGIN,
			spawn_bounds.size.x + Constants.DEBRIS_DESPAWN_MARGIN,
			spawn_bounds.size.y + Constants.DEBRIS_DESPAWN_MARGIN
		)

		# Get camera-adjusted position
		var screen_pos := debris.global_position
		if get_viewport().get_camera_2d():
			var camera := get_viewport().get_camera_2d()
			screen_pos = debris.global_position - camera.global_position + spawn_bounds.size / 2

		if not despawn_rect.has_point(screen_pos):
			to_remove.append(debris)

	# Remove despawned debris
	for debris in to_remove:
		_despawn_debris(debris)


## Spawn a new piece of debris
func spawn_debris() -> Node2D:
	if debris_container == null:
		return null

	# Choose debris type based on weights
	var debris_type := _choose_debris_type()

	# Create debris entity
	var debris := _create_debris_entity(debris_type)
	if debris == null:
		return null

	# Position at screen edge
	var spawn_pos := _get_spawn_position()
	debris.global_position = spawn_pos

	# Set velocity toward/across the screen
	var velocity := _get_drift_velocity(spawn_pos)
	if debris.has_method("set_drift_velocity"):
		debris.set_drift_velocity(velocity)

	# Add to container and track
	debris_container.add_child(debris)
	active_debris.append(debris)

	debris_spawned.emit(debris)
	return debris


func _choose_debris_type() -> String:
	var total_weight := 0
	for weight in debris_weights.values():
		total_weight += weight

	var roll := randi_range(0, total_weight - 1)
	var accumulated := 0

	for type in debris_weights:
		accumulated += debris_weights[type]
		if roll < accumulated:
			return type

	return "iron_asteroid"  # Fallback


func _create_debris_entity(debris_type: String) -> Node2D:
	# Create debris using the DebrisEntity class
	var debris: Area2D = Area2D.new()
	debris.set_script(DebrisEntityScript)
	debris.name = "Debris_" + debris_type

	# Initialize with type and contents
	var contents := _generate_contents(debris_type)
	debris.initialize(debris_type, contents, randi())

	# Connect input for clicking
	debris.input_event.connect(_on_debris_input.bind(debris))

	return debris


func _generate_contents(debris_type: String) -> Array:
	var contents := []
	var rng := RandomNumberGenerator.new()
	rng.randomize()

	match debris_type:
		"iron_asteroid":
			contents.append({"item_id": "iron_ore", "count": rng.randi_range(1, 3)})
		"copper_asteroid":
			contents.append({"item_id": "copper_ore", "count": rng.randi_range(1, 3)})
		"stone_asteroid":
			contents.append({"item_id": "stone", "count": rng.randi_range(1, 2)})
		"coal_asteroid":
			contents.append({"item_id": "coal", "count": rng.randi_range(1, 3)})
		"scrap_metal":
			contents.append({"item_id": "scrap_metal", "count": rng.randi_range(1, 2)})
		"ice_chunk":
			contents.append({"item_id": "ice", "count": rng.randi_range(1, 2)})

	return contents


func _get_spawn_position() -> Vector2:
	var viewport_size := get_viewport().get_visible_rect().size

	# Get camera position for world coordinates
	var camera_offset := Vector2.ZERO
	if get_viewport().get_camera_2d():
		camera_offset = get_viewport().get_camera_2d().global_position - viewport_size / 2

	# Choose a random edge
	var edge := randi_range(0, 3)
	var pos := Vector2.ZERO

	match edge:
		0:  # Top
			pos.x = randf_range(0, viewport_size.x)
			pos.y = -Constants.DEBRIS_SPAWN_MARGIN
		1:  # Right
			pos.x = viewport_size.x + Constants.DEBRIS_SPAWN_MARGIN
			pos.y = randf_range(0, viewport_size.y)
		2:  # Bottom
			pos.x = randf_range(0, viewport_size.x)
			pos.y = viewport_size.y + Constants.DEBRIS_SPAWN_MARGIN
		3:  # Left
			pos.x = -Constants.DEBRIS_SPAWN_MARGIN
			pos.y = randf_range(0, viewport_size.y)

	return pos + camera_offset


func _get_drift_velocity(spawn_pos: Vector2) -> Vector2:
	var viewport_size := get_viewport().get_visible_rect().size

	# Get camera position
	var camera_offset := Vector2.ZERO
	if get_viewport().get_camera_2d():
		camera_offset = get_viewport().get_camera_2d().global_position - viewport_size / 2

	# Target somewhere on screen (with some randomness)
	var center := camera_offset + viewport_size / 2
	var target := center + Vector2(
		randf_range(-viewport_size.x * 0.3, viewport_size.x * 0.3),
		randf_range(-viewport_size.y * 0.3, viewport_size.y * 0.3)
	)

	# Direction and speed
	var direction := (target - spawn_pos).normalized()
	var speed := randf_range(Constants.DEBRIS_MIN_SPEED, Constants.DEBRIS_MAX_SPEED)

	# Add some randomness to direction
	direction = direction.rotated(randf_range(-0.3, 0.3))

	return direction * speed


func _on_debris_input(viewport: Node, event: InputEvent, _shape_idx: int, debris: Node2D) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			collect_debris(debris)


## Collect a debris entity
func collect_debris(debris: Node2D) -> Array:
	if not is_instance_valid(debris):
		return []

	var contents: Array = []
	if debris.has_method("get_contents"):
		contents = debris.get_contents()

	# Add items to inventory
	var collected_items := []
	for content in contents:
		var item := InventoryManager.get_item(content.item_id)
		if item:
			var overflow := InventoryManager.add_item(item, content.count)
			if overflow < content.count:
				collected_items.append({
					"item": item,
					"count": content.count - overflow
				})

	debris_collected.emit(debris, collected_items)
	_despawn_debris(debris)

	return collected_items


func _despawn_debris(debris: Node2D) -> void:
	if not is_instance_valid(debris):
		active_debris.erase(debris)
		return

	debris_despawned.emit(debris)
	active_debris.erase(debris)
	debris.queue_free()


## Set the debris container node
func set_debris_container(container: Node2D) -> void:
	debris_container = container


## Clear all debris
func clear_all_debris() -> void:
	for debris in active_debris.duplicate():
		_despawn_debris(debris)
	active_debris.clear()


## Get current debris count
func get_debris_count() -> int:
	return active_debris.size()


## Adjust spawn rate (for progression)
func set_spawn_rate(rate: float) -> void:
	spawn_rate = maxf(0.5, rate)


## Add a new debris type with weight
func add_debris_type(type: String, weight: int) -> void:
	debris_weights[type] = weight


## Modify weight of existing debris type
func set_debris_weight(type: String, weight: int) -> void:
	if debris_weights.has(type):
		debris_weights[type] = weight
