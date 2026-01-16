extends CanvasLayer

## BuildingUI - Interface for interacting with building inventories
##
## Opens when clicking on buildings with inventories (chests, furnaces).
## Shows building slots and player inventory side-by-side for easy transfers.

signal closed

var current_building: Node2D = null
var is_open: bool = false

## UI containers
var panel: PanelContainer
var title_label: Label
var close_button: Button
var building_slots_container: VBoxContainer
var progress_container: HBoxContainer
var progress_bar: ProgressBar
var player_inventory_container: VBoxContainer
var player_grid: GridContainer

## Slot tracking
var building_slots: Array[Panel] = []
var player_slots: Array[Panel] = []

## Tooltip
var tooltip: PanelContainer
var tooltip_label: Label
var hover_slot_index: int = -1
var hover_is_player_slot: bool = false
var hover_time: float = 0.0
const TOOLTIP_DELAY: float = 0.4  # seconds before showing tooltip

## Slot types for furnace
enum FurnaceSlot { FUEL = 0, INPUT = 1, OUTPUT = 2 }


func _ready() -> void:
	layer = 18
	_create_ui()
	_connect_signals()
	hide_ui()


func _create_ui() -> void:
	# Main panel
	panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(440, 500)
	add_child(panel)

	# Style the panel
	var style := StyleBoxFlat.new()
	style.bg_color = Constants.UI_BACKGROUND
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	panel.add_theme_stylebox_override("panel", style)

	# Center the panel
	panel.anchors_preset = Control.PRESET_CENTER
	panel.set_anchors_and_offsets_preset(Control.PRESET_CENTER)

	# Main VBox
	var main_vbox := VBoxContainer.new()
	main_vbox.add_theme_constant_override("separation", 8)
	panel.add_child(main_vbox)

	# Add margin
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 12)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_top", 8)
	margin.add_theme_constant_override("margin_bottom", 12)
	panel.add_child(margin)

	var content_vbox := VBoxContainer.new()
	content_vbox.add_theme_constant_override("separation", 12)
	margin.add_child(content_vbox)

	# Title bar
	var title_bar := HBoxContainer.new()
	content_vbox.add_child(title_bar)

	title_label = Label.new()
	title_label.text = "Building"
	title_label.add_theme_font_size_override("font_size", 18)
	title_label.add_theme_color_override("font_color", Constants.UI_TEXT)
	title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title_bar.add_child(title_label)

	close_button = Button.new()
	close_button.text = "X"
	close_button.custom_minimum_size = Vector2(28, 28)
	close_button.pressed.connect(hide_ui)
	title_bar.add_child(close_button)

	# Separator
	var sep1 := HSeparator.new()
	content_vbox.add_child(sep1)

	# Building slots section
	building_slots_container = VBoxContainer.new()
	building_slots_container.add_theme_constant_override("separation", 8)
	content_vbox.add_child(building_slots_container)

	# Progress bar (for furnaces)
	progress_container = HBoxContainer.new()
	progress_container.visible = false
	content_vbox.add_child(progress_container)

	var progress_label := Label.new()
	progress_label.text = "Progress:"
	progress_label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	progress_container.add_child(progress_label)

	progress_bar = ProgressBar.new()
	progress_bar.custom_minimum_size = Vector2(200, 20)
	progress_bar.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	progress_bar.min_value = 0.0
	progress_bar.max_value = 1.0
	progress_bar.value = 0.0
	progress_bar.show_percentage = false
	progress_container.add_child(progress_bar)

	# Separator
	var sep2 := HSeparator.new()
	content_vbox.add_child(sep2)

	# Player inventory section
	player_inventory_container = VBoxContainer.new()
	player_inventory_container.add_theme_constant_override("separation", 4)
	content_vbox.add_child(player_inventory_container)

	var player_label := Label.new()
	player_label.text = "Your Inventory"
	player_label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	player_inventory_container.add_child(player_label)

	player_grid = GridContainer.new()
	player_grid.columns = 10
	player_inventory_container.add_child(player_grid)

	# Create player inventory slots
	_create_player_slots()

	# Create tooltip
	_create_tooltip()


func _create_player_slots() -> void:
	player_slots.clear()
	for child in player_grid.get_children():
		child.queue_free()

	for i in range(Constants.PLAYER_INVENTORY_SLOTS):
		var slot := _create_slot(i, true)
		player_grid.add_child(slot)
		player_slots.append(slot)


func _create_tooltip() -> void:
	tooltip = PanelContainer.new()
	tooltip.visible = false
	tooltip.z_index = 100
	add_child(tooltip)

	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.95)
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	style.set_content_margin_all(6)
	tooltip.add_theme_stylebox_override("panel", style)

	tooltip_label = Label.new()
	tooltip_label.add_theme_font_size_override("font_size", 12)
	tooltip_label.add_theme_color_override("font_color", Constants.UI_TEXT)
	tooltip.add_child(tooltip_label)


func _create_slot(index: int, is_player_slot: bool) -> Panel:
	var slot := Panel.new()
	slot.custom_minimum_size = Vector2(40, 40)
	slot.mouse_filter = Control.MOUSE_FILTER_STOP

	# Style
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.1, 0.1, 0.15, 0.9)
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(1)
	style.set_corner_radius_all(4)
	slot.add_theme_stylebox_override("panel", style)

	# Icon
	var icon := TextureRect.new()
	icon.name = "Icon"
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.custom_minimum_size = Vector2(32, 32)
	icon.position = Vector2(4, 4)
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	slot.add_child(icon)

	# Count label
	var count := Label.new()
	count.name = "Count"
	count.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	count.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	count.position = Vector2(4, 22)
	count.size = Vector2(32, 14)
	count.add_theme_font_size_override("font_size", 11)
	count.add_theme_color_override("font_color", Constants.UI_TEXT)
	count.mouse_filter = Control.MOUSE_FILTER_IGNORE
	slot.add_child(count)

	# Connect input
	if is_player_slot:
		slot.gui_input.connect(_on_player_slot_input.bind(index))
		slot.mouse_entered.connect(_on_slot_mouse_entered.bind(index, true))
		slot.mouse_exited.connect(_on_slot_mouse_exited)
	else:
		slot.gui_input.connect(_on_building_slot_input.bind(index))
		slot.mouse_entered.connect(_on_slot_mouse_entered.bind(index, false))
		slot.mouse_exited.connect(_on_slot_mouse_exited)

	return slot


func _connect_signals() -> void:
	InventoryManager.inventory_changed.connect(_update_player_inventory)
	GameManager.game_tick.connect(_on_game_tick)


func _process(delta: float) -> void:
	if not is_open:
		return

	# Handle tooltip delay
	if hover_slot_index >= 0:
		hover_time += delta
		if hover_time >= TOOLTIP_DELAY and not tooltip.visible:
			_show_tooltip()


func _on_slot_mouse_entered(index: int, is_player: bool) -> void:
	hover_slot_index = index
	hover_is_player_slot = is_player
	hover_time = 0.0
	# Don't show immediately - wait for delay in _process


func _on_slot_mouse_exited() -> void:
	hover_slot_index = -1
	hover_time = 0.0
	_hide_tooltip()


func _show_tooltip() -> void:
	var stack: ItemStack = null

	if hover_is_player_slot:
		stack = InventoryManager.get_slot(hover_slot_index)
	else:
		# Get from building
		if current_building is SmallChest:
			stack = current_building.get_slot(hover_slot_index)
		elif current_building is StoneFurnace:
			var furnace := current_building as StoneFurnace
			match hover_slot_index:
				FurnaceSlot.FUEL:
					stack = furnace.fuel_slot
				FurnaceSlot.INPUT:
					stack = furnace.input_slot
				FurnaceSlot.OUTPUT:
					stack = furnace.output_slot

	if stack == null or stack.is_empty():
		return

	# Build tooltip text
	var text := stack.item.name
	if stack.count > 1:
		text += " x" + str(stack.count)

	tooltip_label.text = text
	tooltip.visible = true

	# Position near mouse
	var mouse_pos := get_viewport().get_mouse_position()
	tooltip.position = mouse_pos + Vector2(16, 16)


func _hide_tooltip() -> void:
	tooltip.visible = false


func _on_game_tick() -> void:
	if is_open and current_building != null:
		_update_building_display()


func open_for_building(building: Node2D) -> void:
	current_building = building
	is_open = true
	visible = true

	_setup_building_ui()
	_update_building_display()
	_update_player_inventory()


func _setup_building_ui() -> void:
	# Clear existing building slots
	building_slots.clear()
	for child in building_slots_container.get_children():
		child.queue_free()

	if current_building == null:
		return

	# Get building name
	if current_building.has_method("get_definition"):
		var def = current_building.get_definition()
		if def:
			title_label.text = def.name

	# Setup based on building type
	if current_building is SmallChest:
		_setup_chest_ui()
		progress_container.visible = false
	elif current_building is StoneFurnace:
		_setup_furnace_ui()
		progress_container.visible = true
	else:
		# Generic building with internal inventory
		_setup_generic_ui()
		progress_container.visible = false


func _setup_chest_ui() -> void:
	var chest := current_building as SmallChest

	var label := Label.new()
	label.text = "Storage (16 slots)"
	label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	building_slots_container.add_child(label)

	var grid := GridContainer.new()
	grid.columns = 4
	grid.add_theme_constant_override("h_separation", 4)
	grid.add_theme_constant_override("v_separation", 4)
	building_slots_container.add_child(grid)

	for i in range(16):
		var slot := _create_slot(i, false)
		grid.add_child(slot)
		building_slots.append(slot)


func _setup_furnace_ui() -> void:
	var furnace := current_building as StoneFurnace

	var hbox := HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 20)
	building_slots_container.add_child(hbox)

	# Fuel slot
	var fuel_vbox := VBoxContainer.new()
	fuel_vbox.add_theme_constant_override("separation", 4)
	hbox.add_child(fuel_vbox)

	var fuel_label := Label.new()
	fuel_label.text = "Fuel"
	fuel_label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	fuel_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	fuel_vbox.add_child(fuel_label)

	var fuel_slot := _create_slot(FurnaceSlot.FUEL, false)
	fuel_slot.custom_minimum_size = Vector2(48, 48)
	fuel_vbox.add_child(fuel_slot)
	building_slots.append(fuel_slot)

	# Input slot
	var input_vbox := VBoxContainer.new()
	input_vbox.add_theme_constant_override("separation", 4)
	hbox.add_child(input_vbox)

	var input_label := Label.new()
	input_label.text = "Input"
	input_label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	input_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	input_vbox.add_child(input_label)

	var input_slot := _create_slot(FurnaceSlot.INPUT, false)
	input_slot.custom_minimum_size = Vector2(48, 48)
	input_vbox.add_child(input_slot)
	building_slots.append(input_slot)

	# Arrow indicator
	var arrow := Label.new()
	arrow.text = "→"
	arrow.add_theme_font_size_override("font_size", 24)
	arrow.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	arrow.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	hbox.add_child(arrow)

	# Output slot
	var output_vbox := VBoxContainer.new()
	output_vbox.add_theme_constant_override("separation", 4)
	hbox.add_child(output_vbox)

	var output_label := Label.new()
	output_label.text = "Output"
	output_label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	output_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	output_vbox.add_child(output_label)

	var output_slot := _create_slot(FurnaceSlot.OUTPUT, false)
	output_slot.custom_minimum_size = Vector2(48, 48)
	output_vbox.add_child(output_slot)
	building_slots.append(output_slot)


func _setup_generic_ui() -> void:
	# For buildings with generic internal_inventory
	if not current_building.has_method("get_slot"):
		return

	var label := Label.new()
	label.text = "Contents"
	label.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	building_slots_container.add_child(label)

	var grid := GridContainer.new()
	grid.columns = 4
	building_slots_container.add_child(grid)

	# Try to determine slot count
	var slot_count := 16  # Default
	if current_building.has_method("get_definition"):
		var def = current_building.get_definition()
		if def and def.storage_slots > 0:
			slot_count = def.storage_slots

	for i in range(slot_count):
		var slot := _create_slot(i, false)
		grid.add_child(slot)
		building_slots.append(slot)


func _update_building_display() -> void:
	if current_building == null:
		return

	if current_building is SmallChest:
		_update_chest_display()
	elif current_building is StoneFurnace:
		_update_furnace_display()
	else:
		_update_generic_display()


func _update_chest_display() -> void:
	var chest := current_building as SmallChest

	for i in range(building_slots.size()):
		var slot := building_slots[i]
		var stack := chest.get_slot(i)
		_update_slot_display(slot, stack)


func _update_furnace_display() -> void:
	var furnace := current_building as StoneFurnace

	# Update fuel slot (index 0)
	if building_slots.size() > FurnaceSlot.FUEL:
		_update_slot_display(building_slots[FurnaceSlot.FUEL], furnace.fuel_slot)

	# Update input slot (index 1)
	if building_slots.size() > FurnaceSlot.INPUT:
		_update_slot_display(building_slots[FurnaceSlot.INPUT], furnace.input_slot)

	# Update output slot (index 2)
	if building_slots.size() > FurnaceSlot.OUTPUT:
		_update_slot_display(building_slots[FurnaceSlot.OUTPUT], furnace.output_slot)

	# Update progress bar
	progress_bar.value = furnace.get_smelting_progress()


func _update_generic_display() -> void:
	for i in range(building_slots.size()):
		var slot := building_slots[i]
		var stack: ItemStack = null
		if current_building.has_method("get_slot"):
			stack = current_building.get_slot(i)
		_update_slot_display(slot, stack)


func _update_slot_display(slot: Panel, stack: ItemStack) -> void:
	var icon: TextureRect = slot.get_node("Icon")
	var count: Label = slot.get_node("Count")

	if stack and not stack.is_empty():
		icon.texture = _get_item_texture(stack.item)
		count.text = str(stack.count) if stack.count > 1 else ""
		count.visible = stack.count > 1
	else:
		icon.texture = null
		count.text = ""
		count.visible = false


func _update_player_inventory() -> void:
	for i in range(player_slots.size()):
		var slot := player_slots[i]
		var stack := InventoryManager.get_slot(i)
		_update_slot_display(slot, stack)


func _get_item_texture(item: ItemResource) -> Texture2D:
	match item.category:
		Enums.ItemCategory.RAW_MATERIAL:
			return SpriteGenerator.generate_ore(item.sprite_color)
		Enums.ItemCategory.PROCESSED:
			return SpriteGenerator.generate_plate(item.sprite_color)
		Enums.ItemCategory.COMPONENT:
			if "gear" in item.id:
				return SpriteGenerator.generate_gear(item.sprite_color)
			elif "cable" in item.id:
				return SpriteGenerator.generate_cable(item.sprite_color)
			elif "circuit" in item.id:
				return SpriteGenerator.generate_circuit(item.sprite_color)
			else:
				return SpriteGenerator.generate_plate(item.sprite_color)
		_:
			return SpriteGenerator.generate_plate(item.sprite_color)


func _on_player_slot_input(event: InputEvent, index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_transfer_from_player(index)


func _on_building_slot_input(event: InputEvent, index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_transfer_from_building(index)


func _transfer_from_player(slot_index: int) -> void:
	var stack := InventoryManager.get_slot(slot_index)
	if stack == null or stack.is_empty():
		return

	if current_building == null:
		return

	# Try to transfer to building
	if current_building is StoneFurnace:
		_transfer_to_furnace(stack, slot_index)
	elif current_building.has_method("can_accept_item"):
		if current_building.can_accept_item(stack.item):
			if current_building.insert_item(stack.item, 1):
				InventoryManager.remove_item_at(slot_index, 1)
				_update_building_display()
				_update_player_inventory()


func _transfer_to_furnace(stack: ItemStack, player_slot: int) -> void:
	var furnace := current_building as StoneFurnace

	# Check if it's fuel or ore
	if furnace.can_accept_item(stack.item):
		if furnace.insert_item(stack.item, 1):
			InventoryManager.remove_item_at(player_slot, 1)
			_update_building_display()
			_update_player_inventory()


func _transfer_from_building(slot_index: int) -> void:
	if current_building == null:
		return

	var item: ItemResource = null

	if current_building is SmallChest:
		var chest := current_building as SmallChest
		var stack := chest.get_slot(slot_index)
		if stack and not stack.is_empty():
			item = stack.item
			# Remove from chest
			stack.remove(1)
			if stack.count <= 0:
				stack.item = null
	elif current_building is StoneFurnace:
		var furnace := current_building as StoneFurnace
		var stack: ItemStack = null

		match slot_index:
			FurnaceSlot.FUEL:
				stack = furnace.fuel_slot
			FurnaceSlot.INPUT:
				stack = furnace.input_slot
			FurnaceSlot.OUTPUT:
				stack = furnace.output_slot

		if stack and not stack.is_empty():
			item = stack.item
			stack.remove(1)
			if stack.count <= 0:
				stack.item = null

	# Add to player inventory
	if item:
		if InventoryManager.add_item(item, 1):
			_update_building_display()
			_update_player_inventory()
		else:
			# Failed to add to inventory, put it back
			if current_building is SmallChest:
				current_building.insert_item(item, 1)
			elif current_building is StoneFurnace:
				current_building.insert_item(item, 1)


func hide_ui() -> void:
	visible = false
	is_open = false
	current_building = null
	hover_slot_index = -1
	_hide_tooltip()
	closed.emit()


func toggle_ui() -> void:
	if is_open:
		hide_ui()


func _input(event: InputEvent) -> void:
	if not is_open:
		return

	if event.is_action_pressed("cancel"):
		hide_ui()
		get_viewport().set_input_as_handled()
	elif event.is_action_pressed("inventory"):
		# Close building UI when pressing I (inventory key)
		hide_ui()
		get_viewport().set_input_as_handled()
