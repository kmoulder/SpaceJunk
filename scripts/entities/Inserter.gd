class_name Inserter
extends BuildingEntity

## Inserter - A 1x1 building that moves items between buildings/belts
##
## Picks up items from behind (input side) and drops them in front (output side).
## Has a swing animation as it transfers items.

## Item currently being held by the inserter
var held_item: ItemResource = null

## Current arm angle (0 = pickup position, 1 = drop position)
var arm_position: float = 0.0

## Whether the inserter is swinging (moving the arm)
var is_swinging: bool = false

## Direction of swing (1 = forward to drop, -1 = backward to pickup)
var swing_direction: int = 1

## Whether this is a long inserter (can reach 2 tiles)
var is_long: bool = false

## Filter: only pick up this item (null = no filter)
var item_filter: ItemResource = null

## Visual sprites
var base_sprite: Sprite2D = null
var arm_sprite: Sprite2D = null
var hand_sprite: Sprite2D = null

## Reference to source building
var source_building: Node2D = null

## Reference to destination building
var destination_building: Node2D = null

const TICKS_PER_SECOND: float = 60.0


func _ready() -> void:
	super._ready()
	z_index = Constants.Z_INSERTERS
	_update_targets()


func _generate_texture() -> Texture2D:
	# We'll create custom visuals instead
	return null


func _setup_sprite() -> void:
	# Clear existing sprites
	if base_sprite:
		base_sprite.queue_free()
	if arm_sprite:
		arm_sprite.queue_free()
	if hand_sprite:
		hand_sprite.queue_free()

	# Create base platform
	base_sprite = Sprite2D.new()
	var base_img := Image.create(Constants.TILE_SIZE, Constants.TILE_SIZE, false, Image.FORMAT_RGBA8)
	base_img.fill(Color.TRANSPARENT)

	var base_color := Color(0.5, 0.5, 0.2)
	for x in range(8, 24):
		for y in range(20, 28):
			base_img.set_pixel(x, y, base_color)

	base_sprite.texture = ImageTexture.create_from_image(base_img)
	base_sprite.centered = false
	add_child(base_sprite)

	# Create arm
	arm_sprite = Sprite2D.new()
	var arm_img := Image.create(Constants.TILE_SIZE, Constants.TILE_SIZE, false, Image.FORMAT_RGBA8)
	arm_img.fill(Color.TRANSPARENT)

	var arm_color := Color(0.6, 0.6, 0.3)
	var arm_length := 14 if not is_long else 20
	for y in range(16 - arm_length, 20):
		for x in range(14, 18):
			arm_img.set_pixel(x, y, arm_color)

	arm_sprite.texture = ImageTexture.create_from_image(arm_img)
	arm_sprite.centered = false
	add_child(arm_sprite)

	# Create hand/gripper
	hand_sprite = Sprite2D.new()
	var hand_img := Image.create(16, 8, false, Image.FORMAT_RGBA8)
	hand_img.fill(Color.TRANSPARENT)

	for x in range(2, 14):
		for y in range(2, 6):
			hand_img.set_pixel(x, y, arm_color)

	hand_sprite.texture = ImageTexture.create_from_image(hand_img)
	hand_sprite.centered = true
	hand_sprite.position = Vector2(Constants.TILE_SIZE / 2, 8)
	add_child(hand_sprite)

	_update_arm_visual()


func initialize(def: BuildingResource, pos: Vector2i, rotation: int = 0) -> void:
	super.initialize(def, pos, rotation)
	z_index = Constants.Z_INSERTERS
	_update_targets()


func _process_building() -> void:
	if not is_powered:
		return

	_update_targets()

	if is_swinging:
		_process_swing()
	else:
		_try_start_action()

	_update_arm_visual()


func _try_start_action() -> void:
	if held_item == null:
		# Try to pick up item
		_try_pickup()
	else:
		# Try to drop item
		_try_drop()


func _try_pickup() -> void:
	if source_building == null:
		return

	# Check if source has items
	var pickup_dir := get_direction()  # We pick up from our facing direction's opposite
	var available_item: ItemResource = null

	if source_building.has_method("has_output_item"):
		available_item = source_building.has_output_item(pickup_dir)

	if available_item == null:
		return

	# Check filter
	if item_filter != null and available_item != item_filter:
		return

	# Check if destination can accept
	if destination_building != null and destination_building.has_method("can_accept_item"):
		var drop_dir := Enums.opposite_direction(get_direction())
		if not destination_building.can_accept_item(available_item, drop_dir):
			return

	# Start swinging to pickup position
	is_swinging = true
	swing_direction = -1  # Swing backward to pickup
	arm_position = 0.5  # Start from middle


func _try_drop() -> void:
	if destination_building == null:
		return

	# Start swinging to drop position
	is_swinging = true
	swing_direction = 1  # Swing forward to drop
	arm_position = 0.0  # Start from pickup position


func _process_swing() -> void:
	var swing_speed := 1.0 / (Constants.INSERTER_SWING_TIME * TICKS_PER_SECOND)
	arm_position += swing_direction * swing_speed

	# Complete pickup
	if swing_direction == -1 and arm_position <= 0.0:
		arm_position = 0.0
		_complete_pickup()

	# Complete drop
	if swing_direction == 1 and arm_position >= 1.0:
		arm_position = 1.0
		_complete_drop()


func _complete_pickup() -> void:
	is_swinging = false

	if source_building == null:
		return

	if source_building.has_method("extract_item"):
		var pickup_dir := Enums.opposite_direction(get_direction())
		held_item = source_building.extract_item(pickup_dir)

	# If we got an item, start moving to drop
	if held_item != null:
		is_swinging = true
		swing_direction = 1


func _complete_drop() -> void:
	is_swinging = false

	if held_item == null:
		return

	if destination_building == null:
		return

	if destination_building.has_method("insert_item"):
		var drop_dir := Enums.opposite_direction(get_direction())
		if destination_building.insert_item(held_item, 1, drop_dir):
			held_item = null

	# If we couldn't drop, we'll try again next tick


func _update_targets() -> void:
	var my_pos := grid_position
	var my_dir := get_direction()

	# Source is behind us (opposite of facing direction)
	var source_offset := 1 if not is_long else 2
	var source_dir := Enums.opposite_direction(my_dir)
	var source_pos := my_pos + Enums.direction_to_vector(source_dir) * source_offset
	source_building = GridManager.get_building(source_pos)

	# Destination is in front of us (facing direction)
	var dest_offset := 1 if not is_long else 2
	var dest_pos := my_pos + Enums.direction_to_vector(my_dir) * dest_offset
	destination_building = GridManager.get_building(dest_pos)


func _update_arm_visual() -> void:
	if arm_sprite == null or hand_sprite == null:
		return

	# Rotate arm based on facing direction and arm position
	var base_rotation: float = rotation_index * PI / 2
	var swing_angle: float = lerpf(-PI/3, PI/3, arm_position)

	arm_sprite.rotation = base_rotation
	arm_sprite.position = Vector2(0, 0)

	# Update hand position
	var arm_length := 14.0 if not is_long else 20.0
	var pivot := Vector2(Constants.TILE_SIZE / 2, 24)
	var hand_offset := Vector2(0, -arm_length).rotated(base_rotation + swing_angle)
	hand_sprite.position = pivot + hand_offset
	hand_sprite.rotation = base_rotation + swing_angle


## Override rotation to update visuals
func rotate_cw() -> void:
	super.rotate_cw()
	_setup_sprite()
	_update_targets()


func rotate_ccw() -> void:
	super.rotate_ccw()
	_setup_sprite()
	_update_targets()


## Set filter for this inserter
func set_filter(item: ItemResource) -> void:
	item_filter = item


## Clear filter
func clear_filter() -> void:
	item_filter = null


## Get current filter
func get_filter() -> ItemResource:
	return item_filter


## Check if inserter is idle
func is_idle() -> bool:
	return not is_swinging and held_item == null


## Check if inserter is holding an item
func is_holding() -> bool:
	return held_item != null


## Called when adjacent buildings change
func on_neighbor_changed() -> void:
	_update_targets()
