extends CanvasLayer

## InventoryUI - Player inventory panel
##
## Grid-based inventory display with drag-and-drop support.

signal closed

@onready var panel: PanelContainer = $Panel
@onready var grid: GridContainer = $Panel/VBox/ScrollContainer/Grid
@onready var title_label: Label = $Panel/VBox/TitleBar/Title
@onready var close_button: Button = $Panel/VBox/TitleBar/CloseButton

var inventory_slots: Array[Panel] = []
var dragging_from: int = -1
var is_open: bool = false


func _ready() -> void:
	_setup_panel()
	_setup_inventory_grid()
	_connect_signals()
	hide_inventory()


func _setup_panel() -> void:
	# Style the main panel
	var style := StyleBoxFlat.new()
	style.bg_color = Constants.UI_BACKGROUND
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	panel.add_theme_stylebox_override("panel", style)

	close_button.pressed.connect(hide_inventory)


func _setup_inventory_grid() -> void:
	# Clear existing
	for child in grid.get_children():
		child.queue_free()

	inventory_slots.clear()

	# Configure grid
	grid.columns = 10

	# Create inventory slots
	for i in range(Constants.PLAYER_INVENTORY_SLOTS):
		var slot := _create_inventory_slot(i)
		grid.add_child(slot)
		inventory_slots.append(slot)

	_update_inventory()


func _create_inventory_slot(index: int) -> Panel:
	var slot := Panel.new()
	slot.custom_minimum_size = Vector2(48, 48)
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
	icon.custom_minimum_size = Vector2(40, 40)
	icon.position = Vector2(4, 4)
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	slot.add_child(icon)

	# Count label
	var count := Label.new()
	count.name = "Count"
	count.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	count.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	count.position = Vector2(4, 28)
	count.size = Vector2(40, 16)
	count.add_theme_font_size_override("font_size", 12)
	count.mouse_filter = Control.MOUSE_FILTER_IGNORE
	slot.add_child(count)

	# Connect input
	slot.gui_input.connect(_on_slot_input.bind(index))
	slot.mouse_entered.connect(_on_slot_hover.bind(index))
	slot.mouse_exited.connect(_on_slot_unhover)

	return slot


func _connect_signals() -> void:
	InventoryManager.inventory_changed.connect(_update_inventory)


func _update_inventory() -> void:
	for i in range(inventory_slots.size()):
		var slot := inventory_slots[i]
		var stack := InventoryManager.get_slot(i)

		var icon: TextureRect = slot.get_node("Icon")
		var count: Label = slot.get_node("Count")

		if stack and not stack.is_empty():
			icon.texture = _get_item_texture(stack.item)
			count.text = str(stack.count) if stack.count > 1 else ""
			count.visible = true
		else:
			icon.texture = null
			count.visible = false


func _get_item_texture(item: ItemResource) -> Texture2D:
	# Generate appropriate texture based on item category
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


func _on_slot_input(event: InputEvent, index: int) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			_handle_left_click(index, event.shift_pressed)
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			_handle_right_click(index)


func _handle_left_click(index: int, shift: bool) -> void:
	if dragging_from == -1:
		# Start dragging if slot has items
		var stack := InventoryManager.get_slot(index)
		if stack and not stack.is_empty():
			dragging_from = index
			_highlight_slot(index, true)
	else:
		# Complete swap/merge
		if index != dragging_from:
			InventoryManager.swap_slots(dragging_from, index)
		_highlight_slot(dragging_from, false)
		dragging_from = -1


func _handle_right_click(index: int) -> void:
	# Right-click to split stack (take half)
	var stack := InventoryManager.get_slot(index)
	if stack and stack.count > 1:
		var half := stack.count / 2
		# Find empty slot to put half in
		for i in range(Constants.PLAYER_INVENTORY_SLOTS):
			var other := InventoryManager.get_slot(i)
			if other.is_empty():
				var split_stack := stack.split(half)
				if split_stack:
					InventoryManager.set_slot(i, split_stack)
				break


func _highlight_slot(index: int, highlight: bool) -> void:
	if index < 0 or index >= inventory_slots.size():
		return

	var slot := inventory_slots[index]
	var style: StyleBoxFlat = slot.get_theme_stylebox("panel").duplicate()
	style.border_color = Constants.UI_HIGHLIGHT if highlight else Constants.UI_BORDER
	style.border_width_bottom = 2 if highlight else 1
	style.border_width_top = 2 if highlight else 1
	style.border_width_left = 2 if highlight else 1
	style.border_width_right = 2 if highlight else 1
	slot.add_theme_stylebox_override("panel", style)


func _on_slot_hover(index: int) -> void:
	var stack := InventoryManager.get_slot(index)
	if stack and not stack.is_empty():
		# Show tooltip
		var tooltip_text := stack.item.name
		if stack.count > 1:
			tooltip_text += " x" + str(stack.count)
		# TODO: Show tooltip via HUD


func _on_slot_unhover() -> void:
	# TODO: Hide tooltip
	pass


func show_inventory() -> void:
	visible = true
	is_open = true
	_update_inventory()
	# Pause game when inventory is open
	# GameManager.set_game_state(Enums.GameState.INVENTORY)


func hide_inventory() -> void:
	visible = false
	is_open = false
	dragging_from = -1
	closed.emit()
	# Resume game
	# GameManager.set_game_state(Enums.GameState.PLAYING)


func toggle_inventory() -> void:
	if is_open:
		hide_inventory()
	else:
		show_inventory()


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("inventory"):
		toggle_inventory()
	elif event.is_action_pressed("cancel") and is_open:
		hide_inventory()
