extends Node2D

## Main - Primary game scene controller
##
## Sets up the game world, camera, and coordinates between systems.

@onready var background: ParallaxBackground = $Background
@onready var game_world: Node2D = $GameWorld
@onready var debris_layer: Node2D = $GameWorld/DebrisLayer
@onready var station_layer: Node2D = $GameWorld/StationLayer
@onready var foundation_tiles: Node2D = $GameWorld/StationLayer/FoundationTiles
@onready var buildings_layer: Node2D = $GameWorld/StationLayer/Buildings
@onready var camera: Camera2D = $Camera2D
@onready var hud: CanvasLayer = $HUD
@onready var inventory_ui: CanvasLayer = $InventoryUI

## Camera movement
var camera_target_position: Vector2 = Vector2.ZERO
var camera_zoom_target: float = 1.0

## Foundation tile sprites cache
var foundation_sprites: Dictionary = {}


func _ready() -> void:
	_setup_camera()
	_setup_background()
	_setup_debris_system()
	_setup_station()
	_start_game()


func _setup_camera() -> void:
	camera.position = Vector2.ZERO
	camera.zoom = Vector2.ONE
	camera_zoom_target = 1.0


func _setup_background() -> void:
	# Create starfield background
	var stars_layer := ParallaxLayer.new()
	stars_layer.motion_scale = Vector2(0.1, 0.1)  # Slow parallax for distant stars
	background.add_child(stars_layer)

	var stars_sprite := _create_starfield()
	stars_layer.add_child(stars_sprite)


func _create_starfield() -> Sprite2D:
	# Create a procedural starfield texture
	var size := 512
	var img := Image.create(size, size, false, Image.FORMAT_RGBA8)
	img.fill(Color(0.02, 0.02, 0.05))

	var rng := RandomNumberGenerator.new()
	rng.seed = 12345

	# Add stars
	for _i in range(200):
		var x := rng.randi_range(0, size - 1)
		var y := rng.randi_range(0, size - 1)
		var brightness := rng.randf_range(0.3, 1.0)
		var star_color := Color(brightness, brightness, brightness * 1.1, 1.0)
		img.set_pixel(x, y, star_color)

		# Some stars are slightly larger
		if rng.randf() < 0.1:
			for dx in range(-1, 2):
				for dy in range(-1, 2):
					var sx := clampi(x + dx, 0, size - 1)
					var sy := clampi(y + dy, 0, size - 1)
					var dim := brightness * 0.3
					var current := img.get_pixel(sx, sy)
					img.set_pixel(sx, sy, Color(
						maxf(current.r, dim),
						maxf(current.g, dim),
						maxf(current.b, dim * 1.1),
						1.0
					))

	var texture := ImageTexture.create_from_image(img)

	var sprite := Sprite2D.new()
	sprite.texture = texture
	sprite.centered = false
	sprite.region_enabled = true
	sprite.region_rect = Rect2(0, 0, 2048, 2048)  # Tile the texture
	sprite.position = Vector2(-1024, -1024)

	return sprite


func _setup_debris_system() -> void:
	# Tell debris manager where to spawn debris
	DebrisManager.set_debris_container(debris_layer)


func _setup_station() -> void:
	# Create visual foundation tiles for starting station
	_update_foundation_visuals()

	# Connect to grid manager for updates
	GridManager.foundation_added.connect(_on_foundation_added)
	GridManager.foundation_removed.connect(_on_foundation_removed)


func _update_foundation_visuals() -> void:
	# Generate foundation texture
	var foundation_texture := SpriteGenerator.generate_foundation()

	# Create sprites for each foundation tile
	for pos in GridManager.get_all_foundation():
		_add_foundation_sprite(pos, foundation_texture)


func _add_foundation_sprite(pos: Vector2i, texture: Texture2D = null) -> void:
	if foundation_sprites.has(pos):
		return

	if texture == null:
		texture = SpriteGenerator.generate_foundation()

	var sprite := Sprite2D.new()
	sprite.texture = texture
	sprite.centered = false
	sprite.position = GridManager.grid_to_world(pos)
	sprite.z_index = Constants.Z_FOUNDATION

	foundation_tiles.add_child(sprite)
	foundation_sprites[pos] = sprite


func _remove_foundation_sprite(pos: Vector2i) -> void:
	if foundation_sprites.has(pos):
		foundation_sprites[pos].queue_free()
		foundation_sprites.erase(pos)


func _on_foundation_added(pos: Vector2i) -> void:
	_add_foundation_sprite(pos)


func _on_foundation_removed(pos: Vector2i) -> void:
	_remove_foundation_sprite(pos)


func _start_game() -> void:
	# Initialize game state
	GameManager.start_new_game()

	# Give player some starting items for testing
	_give_starting_items()


func _give_starting_items() -> void:
	# Debug: Give some starting resources
	var iron := InventoryManager.get_item("iron_ore")
	var copper := InventoryManager.get_item("copper_ore")
	var coal := InventoryManager.get_item("coal")

	if iron:
		InventoryManager.add_item(iron, 20)
	if copper:
		InventoryManager.add_item(copper, 15)
	if coal:
		InventoryManager.add_item(coal, 10)


func _process(delta: float) -> void:
	_handle_camera_input(delta)
	_update_camera(delta)


func _handle_camera_input(delta: float) -> void:
	# Pan camera with WASD/arrows
	var pan_direction := Vector2.ZERO

	if Input.is_action_pressed("move_up"):
		pan_direction.y -= 1
	if Input.is_action_pressed("move_down"):
		pan_direction.y += 1
	if Input.is_action_pressed("move_left"):
		pan_direction.x -= 1
	if Input.is_action_pressed("move_right"):
		pan_direction.x += 1

	if pan_direction != Vector2.ZERO:
		pan_direction = pan_direction.normalized()
		camera_target_position += pan_direction * Constants.CAMERA_PAN_SPEED * delta / camera.zoom.x


func _input(event: InputEvent) -> void:
	# Zoom with mouse wheel
	if event.is_action_pressed("zoom_in"):
		camera_zoom_target = minf(camera_zoom_target + Constants.CAMERA_ZOOM_STEP, Constants.CAMERA_ZOOM_MAX)
	elif event.is_action_pressed("zoom_out"):
		camera_zoom_target = maxf(camera_zoom_target - Constants.CAMERA_ZOOM_STEP, Constants.CAMERA_ZOOM_MIN)

	# Handle clicking on game world
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			_handle_world_click(event.position)


func _update_camera(delta: float) -> void:
	# Smooth camera movement
	camera.position = camera.position.lerp(camera_target_position, 5.0 * delta)

	# Smooth zoom
	var current_zoom := camera.zoom.x
	var new_zoom := lerpf(current_zoom, camera_zoom_target, 5.0 * delta)
	camera.zoom = Vector2(new_zoom, new_zoom)


func _handle_world_click(screen_pos: Vector2) -> void:
	# Convert screen position to world position
	var world_pos := camera.get_global_mouse_position()

	# Check if clicking on grid for building placement
	var grid_pos := GridManager.world_to_grid(world_pos)

	# For now, just check if clicking on station area
	if GridManager.has_foundation(grid_pos):
		print("Clicked on station tile: ", grid_pos)
		# TODO: Handle building placement


func get_mouse_world_position() -> Vector2:
	return camera.get_global_mouse_position()


func get_mouse_grid_position() -> Vector2i:
	return GridManager.world_to_grid(get_mouse_world_position())
