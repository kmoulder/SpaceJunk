class_name SpriteGenerator
extends RefCounted

## SpriteGenerator - Procedurally generates pixel art sprites
##
## Creates textures at runtime for items, buildings, and debris
## using a chunky 32x32 pixel art style.

const SPRITE_SIZE := 32


## Generate an ore/raw material sprite
static func generate_ore(base_color: Color, variation_seed: int = 0) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	var rng := RandomNumberGenerator.new()
	rng.seed = variation_seed

	# Fill with transparency
	img.fill(Color.TRANSPARENT)

	# Create an irregular rocky shape
	var center := Vector2(SPRITE_SIZE / 2, SPRITE_SIZE / 2)
	var base_radius := SPRITE_SIZE * 0.35

	for x in range(SPRITE_SIZE):
		for y in range(SPRITE_SIZE):
			var pos := Vector2(x, y)
			var dist := pos.distance_to(center)

			# Add noise to the radius for irregular shape
			var angle := center.angle_to_point(pos)
			var noise_offset := sin(angle * 5 + rng.randf() * 0.5) * 3.0
			noise_offset += sin(angle * 3) * 2.0

			if dist < base_radius + noise_offset:
				# Determine shade based on position
				var shade := 1.0 - (dist / (base_radius + 5)) * 0.3
				shade += rng.randf_range(-0.1, 0.1)

				# Add highlights and shadows
				if x < SPRITE_SIZE / 2 and y < SPRITE_SIZE / 2:
					shade += 0.15  # Top-left highlight
				if x > SPRITE_SIZE * 0.6 and y > SPRITE_SIZE * 0.6:
					shade -= 0.15  # Bottom-right shadow

				var color := base_color * shade
				color.a = 1.0
				img.set_pixel(x, y, color)

	# Add some random specks for texture
	for _i in range(8):
		var sx := rng.randi_range(4, SPRITE_SIZE - 5)
		var sy := rng.randi_range(4, SPRITE_SIZE - 5)
		var speck_color := base_color * rng.randf_range(0.6, 1.3)
		speck_color.a = 1.0
		if img.get_pixel(sx, sy).a > 0:
			img.set_pixel(sx, sy, speck_color)

	return ImageTexture.create_from_image(img)


## Generate a plate/processed material sprite
static func generate_plate(base_color: Color) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var margin := 4
	var plate_rect := Rect2i(margin, margin, SPRITE_SIZE - margin * 2, SPRITE_SIZE - margin * 2)

	# Draw main plate shape
	for x in range(plate_rect.position.x, plate_rect.end.x):
		for y in range(plate_rect.position.y, plate_rect.end.y):
			var color := base_color
			# Add subtle gradient
			var shade := 1.0 - (y - margin) / float(plate_rect.size.y) * 0.2
			color = color * shade
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Add highlight on top edge
	for x in range(plate_rect.position.x + 1, plate_rect.end.x - 1):
		var highlight := base_color * 1.3
		highlight.a = 1.0
		img.set_pixel(x, margin, highlight)
		img.set_pixel(x, margin + 1, base_color * 1.15)

	# Add shadow on bottom edge
	for x in range(plate_rect.position.x + 1, plate_rect.end.x - 1):
		var shadow := base_color * 0.6
		shadow.a = 1.0
		img.set_pixel(x, plate_rect.end.y - 1, shadow)

	# Add border
	var border_color := base_color * 0.5
	border_color.a = 1.0
	for x in range(plate_rect.position.x, plate_rect.end.x):
		img.set_pixel(x, margin, border_color)
		img.set_pixel(x, plate_rect.end.y - 1, border_color)
	for y in range(plate_rect.position.y, plate_rect.end.y):
		img.set_pixel(margin, y, border_color)
		img.set_pixel(plate_rect.end.x - 1, y, border_color)

	return ImageTexture.create_from_image(img)


## Generate a gear sprite
static func generate_gear(base_color: Color) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var center := Vector2(SPRITE_SIZE / 2, SPRITE_SIZE / 2)
	var outer_radius := SPRITE_SIZE * 0.4
	var inner_radius := SPRITE_SIZE * 0.15
	var tooth_count := 8
	var tooth_height := 4.0

	for x in range(SPRITE_SIZE):
		for y in range(SPRITE_SIZE):
			var pos := Vector2(x, y)
			var dist := pos.distance_to(center)
			var angle := center.angle_to_point(pos)

			# Check if we're in a tooth
			var tooth_angle := fmod(angle + PI, TAU / tooth_count)
			var in_tooth := tooth_angle < TAU / tooth_count / 2

			var effective_radius := outer_radius
			if in_tooth:
				effective_radius += tooth_height

			if dist < effective_radius and dist > inner_radius:
				var shade := 0.9 + (center.y - y) / SPRITE_SIZE * 0.3
				var color := base_color * shade
				color.a = 1.0
				img.set_pixel(x, y, color)

	return ImageTexture.create_from_image(img)


## Generate a cable/wire sprite
static func generate_cable(base_color: Color) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	# Draw coiled cable
	var center_x := SPRITE_SIZE / 2
	var cable_width := 3

	for y in range(4, SPRITE_SIZE - 4):
		var wave := sin(y * 0.5) * 4
		var x_start := int(center_x + wave - cable_width / 2)
		for dx in range(cable_width):
			var x := x_start + dx
			if x >= 0 and x < SPRITE_SIZE:
				var shade := 1.0 - abs(dx - cable_width / 2) / float(cable_width) * 0.3
				var color := base_color * shade
				color.a = 1.0
				img.set_pixel(x, y, color)

	return ImageTexture.create_from_image(img)


## Generate a circuit sprite
static func generate_circuit(base_color: Color, complexity: int = 1) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)

	# Green PCB background
	var pcb_color := Color(0.1, 0.3, 0.15)
	img.fill(Color.TRANSPARENT)

	var margin := 3
	for x in range(margin, SPRITE_SIZE - margin):
		for y in range(margin, SPRITE_SIZE - margin):
			img.set_pixel(x, y, pcb_color)

	# Add traces
	var trace_color := Color(0.7, 0.6, 0.2)  # Gold traces
	var rng := RandomNumberGenerator.new()
	rng.seed = complexity

	# Horizontal traces
	for _i in range(3 + complexity):
		var y := rng.randi_range(margin + 2, SPRITE_SIZE - margin - 3)
		var x_start := rng.randi_range(margin, SPRITE_SIZE / 2)
		var x_end := rng.randi_range(SPRITE_SIZE / 2, SPRITE_SIZE - margin)
		for x in range(x_start, x_end):
			img.set_pixel(x, y, trace_color)

	# Vertical traces
	for _i in range(2 + complexity):
		var x := rng.randi_range(margin + 2, SPRITE_SIZE - margin - 3)
		var y_start := rng.randi_range(margin, SPRITE_SIZE / 2)
		var y_end := rng.randi_range(SPRITE_SIZE / 2, SPRITE_SIZE - margin)
		for y in range(y_start, y_end):
			img.set_pixel(x, y, trace_color)

	# Add chip in center
	var chip_size := 8 + complexity * 2
	var chip_start := (SPRITE_SIZE - chip_size) / 2
	for x in range(chip_start, chip_start + chip_size):
		for y in range(chip_start, chip_start + chip_size):
			img.set_pixel(x, y, base_color)

	# Chip highlight
	for x in range(chip_start + 1, chip_start + chip_size - 1):
		var highlight := base_color * 1.3
		highlight.a = 1.0
		img.set_pixel(x, chip_start + 1, highlight)

	return ImageTexture.create_from_image(img)


## Generate a building sprite
static func generate_building(base_color: Color, size: Vector2i) -> ImageTexture:
	var pixel_size := size * SPRITE_SIZE
	var img := Image.create(pixel_size.x, pixel_size.y, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var margin := 2

	# Main body
	for x in range(margin, pixel_size.x - margin):
		for y in range(margin, pixel_size.y - margin):
			var shade := 1.0 - float(y) / pixel_size.y * 0.2
			var color := base_color * shade
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Top highlight
	for x in range(margin + 1, pixel_size.x - margin - 1):
		var highlight := base_color * 1.2
		highlight.a = 1.0
		img.set_pixel(x, margin + 1, highlight)

	# Border
	var border := base_color * 0.5
	border.a = 1.0
	for x in range(margin, pixel_size.x - margin):
		img.set_pixel(x, margin, border)
		img.set_pixel(x, pixel_size.y - margin - 1, border)
	for y in range(margin, pixel_size.y - margin):
		img.set_pixel(margin, y, border)
		img.set_pixel(pixel_size.x - margin - 1, y, border)

	# Add some detail rectangles
	var detail_color := base_color * 0.7
	var detail_margin := 6
	for x in range(detail_margin, pixel_size.x - detail_margin):
		for y in range(pixel_size.y / 2, pixel_size.y / 2 + 4):
			img.set_pixel(x, y, detail_color)

	return ImageTexture.create_from_image(img)


## Generate a foundation tile sprite
static func generate_foundation() -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)

	var base := Color(0.25, 0.25, 0.3)
	var grid_line := Color(0.35, 0.35, 0.4)
	var border := Color(0.2, 0.2, 0.25)

	img.fill(base)

	# Add subtle grid pattern
	for i in range(0, SPRITE_SIZE, 8):
		for j in range(SPRITE_SIZE):
			img.set_pixel(i, j, grid_line)
			img.set_pixel(j, i, grid_line)

	# Border
	for i in range(SPRITE_SIZE):
		img.set_pixel(i, 0, border)
		img.set_pixel(i, SPRITE_SIZE - 1, border)
		img.set_pixel(0, i, border)
		img.set_pixel(SPRITE_SIZE - 1, i, border)

	return ImageTexture.create_from_image(img)


## Generate a chest sprite
static func generate_chest(base_color: Color) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var margin := 4
	var lid_height := 8

	# Draw box body
	for x in range(margin, SPRITE_SIZE - margin):
		for y in range(lid_height + margin, SPRITE_SIZE - margin):
			var shade := 0.9
			if x == margin or x == SPRITE_SIZE - margin - 1:
				shade = 0.7
			if y == SPRITE_SIZE - margin - 1:
				shade = 0.6
			var color := base_color * shade
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Draw lid
	for x in range(margin - 1, SPRITE_SIZE - margin + 1):
		for y in range(margin, margin + lid_height):
			var shade := 1.1 - float(y - margin) / lid_height * 0.2
			var color := base_color * shade
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Lid clasp
	var clasp_color := Color(0.8, 0.7, 0.3)
	for x in range(SPRITE_SIZE / 2 - 2, SPRITE_SIZE / 2 + 2):
		img.set_pixel(x, margin + lid_height - 1, clasp_color)
		img.set_pixel(x, margin + lid_height, clasp_color)

	return ImageTexture.create_from_image(img)


## Generate a furnace sprite
static func generate_furnace(is_electric: bool = false) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE * 2, SPRITE_SIZE * 2, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var base := Color(0.4, 0.35, 0.3) if not is_electric else Color(0.5, 0.5, 0.55)
	var size := SPRITE_SIZE * 2
	var margin := 4

	# Main body
	for x in range(margin, size - margin):
		for y in range(margin, size - margin):
			var shade := 1.0 - float(y) / size * 0.2
			var color := base * shade
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Fire opening
	var fire_color := Color(1.0, 0.4, 0.1)
	var opening_margin := 12
	for x in range(opening_margin, size - opening_margin):
		for y in range(size / 2, size - opening_margin):
			var intensity := 1.0 - float(y - size / 2) / (size / 2 - opening_margin) * 0.5
			var color := fire_color * intensity
			color.a = 1.0
			img.set_pixel(x, y, color)

	# Border
	var border := base * 0.5
	border.a = 1.0
	for i in range(margin, size - margin):
		img.set_pixel(i, margin, border)
		img.set_pixel(i, size - margin - 1, border)
		img.set_pixel(margin, i, border)
		img.set_pixel(size - margin - 1, i, border)

	return ImageTexture.create_from_image(img)


## Generate a belt sprite (single tile, direction indicated by parameter)
static func generate_belt(direction: Enums.Direction) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)

	var base := Color(0.3, 0.3, 0.35)
	var stripe := Color(0.5, 0.45, 0.2)
	var arrow := Color(0.6, 0.55, 0.3)

	img.fill(base)

	# Add stripes based on direction
	var is_horizontal := direction == Enums.Direction.EAST or direction == Enums.Direction.WEST
	var stripe_spacing := 8

	if is_horizontal:
		for x in range(0, SPRITE_SIZE, stripe_spacing):
			for y in range(4, SPRITE_SIZE - 4):
				img.set_pixel(x, y, stripe)
				if x + 1 < SPRITE_SIZE:
					img.set_pixel(x + 1, y, stripe)
	else:
		for y in range(0, SPRITE_SIZE, stripe_spacing):
			for x in range(4, SPRITE_SIZE - 4):
				img.set_pixel(x, y, stripe)
				if y + 1 < SPRITE_SIZE:
					img.set_pixel(x, y + 1, stripe)

	# Add direction arrow in center
	var cx := SPRITE_SIZE / 2
	var cy := SPRITE_SIZE / 2

	match direction:
		Enums.Direction.NORTH:
			for i in range(-3, 4):
				img.set_pixel(cx + i, cy + 2, arrow)
			for i in range(-2, 3):
				img.set_pixel(cx + i, cy, arrow)
			for i in range(-1, 2):
				img.set_pixel(cx + i, cy - 2, arrow)
			img.set_pixel(cx, cy - 4, arrow)
		Enums.Direction.SOUTH:
			for i in range(-3, 4):
				img.set_pixel(cx + i, cy - 2, arrow)
			for i in range(-2, 3):
				img.set_pixel(cx + i, cy, arrow)
			for i in range(-1, 2):
				img.set_pixel(cx + i, cy + 2, arrow)
			img.set_pixel(cx, cy + 4, arrow)
		Enums.Direction.EAST:
			for i in range(-3, 4):
				img.set_pixel(cx - 2, cy + i, arrow)
			for i in range(-2, 3):
				img.set_pixel(cx, cy + i, arrow)
			for i in range(-1, 2):
				img.set_pixel(cx + 2, cy + i, arrow)
			img.set_pixel(cx + 4, cy, arrow)
		Enums.Direction.WEST:
			for i in range(-3, 4):
				img.set_pixel(cx + 2, cy + i, arrow)
			for i in range(-2, 3):
				img.set_pixel(cx, cy + i, arrow)
			for i in range(-1, 2):
				img.set_pixel(cx - 2, cy + i, arrow)
			img.set_pixel(cx - 4, cy, arrow)

	return ImageTexture.create_from_image(img)


## Generate an inserter sprite
static func generate_inserter(is_long: bool = false) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var base := Color(0.5, 0.5, 0.2)
	var arm := Color(0.6, 0.6, 0.3)

	# Base platform
	for x in range(8, 24):
		for y in range(20, 28):
			img.set_pixel(x, y, base)

	# Arm
	var arm_length := 12 if not is_long else 18
	for y in range(16 - arm_length, 20):
		for x in range(14, 18):
			img.set_pixel(x, y, arm)

	# Hand/gripper
	var hand_y := 16 - arm_length
	for x in range(10, 22):
		img.set_pixel(x, hand_y, arm)
		img.set_pixel(x, hand_y + 1, arm)

	return ImageTexture.create_from_image(img)


## Generate a debris sprite based on type
static func generate_debris(debris_type: String, variation: int = 0) -> ImageTexture:
	match debris_type:
		"iron_asteroid":
			return generate_ore(Constants.COLOR_IRON_ORE, variation)
		"copper_asteroid":
			return generate_ore(Constants.COLOR_COPPER_ORE, variation)
		"stone_asteroid":
			return generate_ore(Constants.COLOR_STONE, variation)
		"coal_asteroid":
			return generate_ore(Constants.COLOR_COAL, variation)
		"scrap_metal":
			return _generate_scrap(variation)
		"ice_chunk":
			return _generate_ice(variation)
		_:
			return generate_ore(Color.GRAY, variation)


static func _generate_scrap(variation: int) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var rng := RandomNumberGenerator.new()
	rng.seed = variation

	var metal_color := Color(0.5, 0.5, 0.55)

	# Draw random metal pieces
	for _piece in range(4):
		var px := rng.randi_range(4, SPRITE_SIZE - 12)
		var py := rng.randi_range(4, SPRITE_SIZE - 12)
		var pw := rng.randi_range(4, 10)
		var ph := rng.randi_range(4, 10)

		var piece_color := metal_color * rng.randf_range(0.7, 1.2)
		piece_color.a = 1.0

		for x in range(px, mini(px + pw, SPRITE_SIZE)):
			for y in range(py, mini(py + ph, SPRITE_SIZE)):
				img.set_pixel(x, y, piece_color)

	return ImageTexture.create_from_image(img)


static func _generate_ice(variation: int) -> ImageTexture:
	var img := Image.create(SPRITE_SIZE, SPRITE_SIZE, false, Image.FORMAT_RGBA8)
	img.fill(Color.TRANSPARENT)

	var rng := RandomNumberGenerator.new()
	rng.seed = variation

	var ice_color := Color(0.7, 0.85, 0.95, 0.9)
	var center := Vector2(SPRITE_SIZE / 2, SPRITE_SIZE / 2)

	# Crystalline shape
	for x in range(SPRITE_SIZE):
		for y in range(SPRITE_SIZE):
			var pos := Vector2(x, y)
			var dist := pos.distance_to(center)
			var angle := center.angle_to_point(pos)

			# Create crystalline edges
			var radius := 10.0 + sin(angle * 6) * 3 + rng.randf() * 2
			if dist < radius:
				var alpha := 0.7 + (1.0 - dist / radius) * 0.3
				var color := ice_color
				color.a = alpha
				img.set_pixel(x, y, color)

	return ImageTexture.create_from_image(img)


## Generate a solar panel sprite
static func generate_solar_panel() -> ImageTexture:
	var img := Image.create(SPRITE_SIZE * 2, SPRITE_SIZE * 2, false, Image.FORMAT_RGBA8)
	var size := SPRITE_SIZE * 2
	img.fill(Color.TRANSPARENT)

	var frame := Color(0.4, 0.4, 0.45)
	var panel := Color(0.15, 0.2, 0.35)
	var cell := Color(0.1, 0.15, 0.4)

	var margin := 4
	var frame_width := 3

	# Outer frame
	for x in range(margin, size - margin):
		for y in range(margin, size - margin):
			img.set_pixel(x, y, frame)

	# Panel area
	for x in range(margin + frame_width, size - margin - frame_width):
		for y in range(margin + frame_width, size - margin - frame_width):
			img.set_pixel(x, y, panel)

	# Solar cells grid
	var cell_size := 10
	var cell_gap := 2
	var start := margin + frame_width + cell_gap
	for cx in range(start, size - margin - frame_width - cell_size, cell_size + cell_gap):
		for cy in range(start, size - margin - frame_width - cell_size, cell_size + cell_gap):
			for x in range(cx, cx + cell_size):
				for y in range(cy, cy + cell_size):
					img.set_pixel(x, y, cell)

	return ImageTexture.create_from_image(img)
