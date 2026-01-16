class_name ConveyorBelt
extends BuildingEntity

## ConveyorBelt - A 1x1 transport building that moves items in a direction
##
## Items move along the belt at a configurable speed.
## Belts automatically connect to adjacent belts.

## Item on the belt (null if empty)
var belt_item: ItemResource = null

## Progress of item along belt (0.0 = start, 1.0 = end)
var item_progress: float = 0.0

## Belt speed in tiles per second
var belt_speed: float = Constants.BELT_SPEED_TIER_1

## Visual representation of item on belt
var item_sprite: Sprite2D = null

## Connected input belt (belt feeding into this one)
var input_belt: ConveyorBelt = null

## Connected output belt (belt this feeds into)
var output_belt: ConveyorBelt = null

## Reference to adjacent building at output (if not a belt)
var output_building: Node2D = null

## Animation offset for belt stripes
var animation_offset: float = 0.0

const TICKS_PER_SECOND: float = 60.0


func _ready() -> void:
	super._ready()
	z_index = Constants.Z_BELTS
	_update_connections()


func _generate_texture() -> Texture2D:
	return SpriteGenerator.generate_belt(get_direction())


func initialize(def: BuildingResource, pos: Vector2i, rotation: int = 0) -> void:
	super.initialize(def, pos, rotation)
	z_index = Constants.Z_BELTS
	_update_connections()


func _process_building() -> void:
	_process_belt_movement()
	_update_item_visual()


func _process_belt_movement() -> void:
	if belt_item == null:
		# Try to receive item from input
		_try_receive_from_input()
		return

	# Move item along belt
	var tick_progress := belt_speed / TICKS_PER_SECOND
	item_progress += tick_progress

	# Item reached end of belt
	if item_progress >= 1.0:
		_try_transfer_item()


func _try_receive_from_input() -> void:
	if input_belt != null and input_belt.belt_item != null and input_belt.item_progress >= 1.0:
		# Transfer from input belt
		belt_item = input_belt.belt_item
		item_progress = 0.0
		input_belt.belt_item = null
		input_belt.item_progress = 0.0
		_create_item_sprite()


func _try_transfer_item() -> void:
	if belt_item == null:
		return

	var transferred := false

	# Try to transfer to output belt
	if output_belt != null:
		if output_belt.belt_item == null:
			output_belt.belt_item = belt_item
			output_belt.item_progress = 0.0
			output_belt._create_item_sprite()
			transferred = true

	# Try to transfer to output building (chest, furnace, etc.)
	elif output_building != null:
		if output_building.has_method("can_accept_item"):
			var input_dir := Enums.opposite_direction(get_direction())
			if output_building.can_accept_item(belt_item, input_dir):
				if output_building.insert_item(belt_item, 1, input_dir):
					transferred = true

	if transferred:
		belt_item = null
		item_progress = 0.0
		_remove_item_sprite()
	else:
		# Item blocked, stay at end of belt
		item_progress = 1.0


func _create_item_sprite() -> void:
	if item_sprite != null:
		item_sprite.queue_free()

	if belt_item == null:
		return

	item_sprite = Sprite2D.new()
	item_sprite.texture = _get_item_texture(belt_item)
	item_sprite.z_index = Constants.Z_ITEMS
	item_sprite.scale = Vector2(0.5, 0.5)  # Items on belts are smaller
	add_child(item_sprite)
	_update_item_visual()


func _remove_item_sprite() -> void:
	if item_sprite != null:
		item_sprite.queue_free()
		item_sprite = null


func _get_item_texture(item: ItemResource) -> Texture2D:
	match item.category:
		Enums.ItemCategory.RAW_MATERIAL:
			return SpriteGenerator.generate_ore(item.sprite_color, item.id.hash())
		Enums.ItemCategory.PROCESSED:
			return SpriteGenerator.generate_plate(item.sprite_color)
		Enums.ItemCategory.COMPONENT:
			if "gear" in item.id:
				return SpriteGenerator.generate_gear(item.sprite_color)
			elif "cable" in item.id:
				return SpriteGenerator.generate_cable(item.sprite_color)
			elif "circuit" in item.id:
				return SpriteGenerator.generate_circuit(item.sprite_color, 1)
			else:
				return SpriteGenerator.generate_plate(item.sprite_color)
		_:
			return SpriteGenerator.generate_plate(item.sprite_color)


func _update_item_visual() -> void:
	if item_sprite == null or belt_item == null:
		return

	# Calculate item position along belt
	var dir_vec := Vector2(Enums.direction_to_vector(get_direction()))
	var start_pos := Vector2(Constants.TILE_SIZE / 2, Constants.TILE_SIZE / 2)
	var end_pos := start_pos + dir_vec * Constants.TILE_SIZE * 0.4

	item_sprite.position = start_pos.lerp(end_pos, item_progress)


func _update_connections() -> void:
	var my_dir := get_direction()
	var my_pos := grid_position

	# Check for input belt (belt pointing at us)
	var input_dir := Enums.opposite_direction(my_dir)
	var input_pos := my_pos + Enums.direction_to_vector(input_dir)
	var input_node := GridManager.get_building(input_pos)

	if input_node is ConveyorBelt:
		# Only connect if the belt is pointing at us
		if input_node.get_direction() == my_dir:
			input_belt = input_node
			input_node.output_belt = self
	else:
		input_belt = null

	# Check for output belt or building
	var output_pos := my_pos + Enums.direction_to_vector(my_dir)
	var output_node := GridManager.get_building(output_pos)

	if output_node is ConveyorBelt:
		output_belt = output_node
		output_building = null
		# Tell the other belt about us
		if output_node.input_belt == null:
			var expected_input_dir := Enums.opposite_direction(output_node.get_direction())
			if my_dir == output_node.get_direction() or Enums.opposite_direction(my_dir) == expected_input_dir:
				output_node.input_belt = self
	elif output_node != null:
		output_belt = null
		output_building = output_node
	else:
		output_belt = null
		output_building = null


## Update sprite when rotation changes
func rotate_cw() -> void:
	super.rotate_cw()
	_setup_sprite()
	_update_connections()


func rotate_ccw() -> void:
	super.rotate_ccw()
	_setup_sprite()
	_update_connections()


## Override: Belts accept items being dropped on them (from inserters)
func can_accept_item(item: ItemResource, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if item.is_fluid:
		return false
	return belt_item == null


## Override: Insert item onto belt
func insert_item(item: ItemResource, _count: int = 1, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if belt_item != null or item.is_fluid:
		return false

	belt_item = item
	item_progress = 0.0
	_create_item_sprite()
	return true


## Override: Check for items to pick up
func has_output_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	# Items can be picked up from belt if they're far enough along
	if belt_item != null and item_progress >= 0.5:
		return belt_item
	return null


## Override: Extract item from belt
func extract_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	if belt_item == null:
		return null

	var item := belt_item
	belt_item = null
	item_progress = 0.0
	_remove_item_sprite()
	return item


## Called when adjacent buildings change
func on_neighbor_changed() -> void:
	_update_connections()


## Check if belt is empty
func is_empty() -> bool:
	return belt_item == null


## Check if belt output is blocked
func is_blocked() -> bool:
	return belt_item != null and item_progress >= 1.0
