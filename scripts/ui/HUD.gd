extends CanvasLayer

## HUD - Main game heads-up display
##
## Shows hotbar, resource counters, and minimap.

@onready var hotbar_container: HBoxContainer = $HotbarPanel/HotbarContainer
@onready var resource_panel: VBoxContainer = $ResourcePanel
@onready var crafting_progress: ProgressBar = $CraftingProgress
@onready var tooltip: PanelContainer = $Tooltip
@onready var tooltip_label: Label = $Tooltip/Label

var hotbar_slots: Array[Panel] = []


func _ready() -> void:
	_setup_hotbar()
	_setup_resource_display()
	_connect_signals()

	# Hide tooltip initially
	tooltip.visible = false
	crafting_progress.visible = false


func _setup_hotbar() -> void:
	# Clear existing
	for child in hotbar_container.get_children():
		child.queue_free()

	hotbar_slots.clear()

	# Create hotbar slots
	for i in range(Constants.HOTBAR_SLOTS):
		var slot := _create_hotbar_slot(i)
		hotbar_container.add_child(slot)
		hotbar_slots.append(slot)

	_update_hotbar()


func _create_hotbar_slot(index: int) -> Panel:
	var slot := Panel.new()
	slot.custom_minimum_size = Vector2(48, 48)

	# Style
	var style := StyleBoxFlat.new()
	style.bg_color = Constants.UI_BACKGROUND
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(4)
	slot.add_theme_stylebox_override("panel", style)

	# Icon
	var icon := TextureRect.new()
	icon.name = "Icon"
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.custom_minimum_size = Vector2(40, 40)
	icon.position = Vector2(4, 4)
	slot.add_child(icon)

	# Count label
	var count := Label.new()
	count.name = "Count"
	count.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	count.vertical_alignment = VERTICAL_ALIGNMENT_BOTTOM
	count.position = Vector2(4, 28)
	count.size = Vector2(40, 16)
	count.add_theme_font_size_override("font_size", 12)
	slot.add_child(count)

	# Key hint
	var key := Label.new()
	key.name = "KeyHint"
	key.text = str((index + 1) % 10)
	key.position = Vector2(2, 2)
	key.add_theme_font_size_override("font_size", 10)
	key.add_theme_color_override("font_color", Constants.UI_TEXT_DIM)
	slot.add_child(key)

	return slot


func _setup_resource_display() -> void:
	# This will show key resource counts
	_update_resource_display()


func _connect_signals() -> void:
	InventoryManager.inventory_changed.connect(_on_inventory_changed)
	InventoryManager.hotbar_changed.connect(_update_hotbar)
	InventoryManager.slot_selected.connect(_on_slot_selected)
	CraftingManager.craft_progress.connect(_on_craft_progress)
	CraftingManager.craft_completed.connect(_on_craft_completed)
	CraftingManager.queue_changed.connect(_on_craft_queue_changed)


func _on_inventory_changed() -> void:
	_update_hotbar()
	_update_resource_display()


func _update_hotbar() -> void:
	for i in range(hotbar_slots.size()):
		var slot := hotbar_slots[i]
		var stack := InventoryManager.hotbar[i] if i < InventoryManager.hotbar.size() else null

		var icon: TextureRect = slot.get_node("Icon")
		var count: Label = slot.get_node("Count")

		if stack and not stack.is_empty():
			# TODO: Get proper icon from item
			icon.texture = SpriteGenerator.generate_plate(stack.item.sprite_color)
			count.text = str(stack.count) if stack.count > 1 else ""
			count.visible = true
		else:
			icon.texture = null
			count.visible = false

		# Highlight selected slot
		var style: StyleBoxFlat = slot.get_theme_stylebox("panel").duplicate()
		if i == InventoryManager.selected_hotbar_slot:
			style.border_color = Constants.UI_HIGHLIGHT
		else:
			style.border_color = Constants.UI_BORDER
		slot.add_theme_stylebox_override("panel", style)


func _update_resource_display() -> void:
	# Clear existing
	for child in resource_panel.get_children():
		child.queue_free()

	# Show counts for key resources
	var key_items := ["iron_ore", "copper_ore", "iron_plate", "copper_plate"]

	for item_id in key_items:
		var item := InventoryManager.get_item(item_id)
		if item:
			var count := InventoryManager.get_item_count(item)
			var row := _create_resource_row(item, count)
			resource_panel.add_child(row)


func _create_resource_row(item: ItemResource, count: int) -> HBoxContainer:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 8)

	# Icon
	var icon := TextureRect.new()
	icon.custom_minimum_size = Vector2(20, 20)
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.texture = SpriteGenerator.generate_plate(item.sprite_color)
	row.add_child(icon)

	# Count
	var label := Label.new()
	label.text = str(count)
	label.add_theme_font_size_override("font_size", 14)
	row.add_child(label)

	return row


func _on_slot_selected(index: int) -> void:
	_update_hotbar()


func _on_craft_progress(recipe: RecipeResource, progress: float) -> void:
	crafting_progress.visible = true
	crafting_progress.value = progress * 100


func _on_craft_completed(_recipe: RecipeResource) -> void:
	crafting_progress.visible = CraftingManager.craft_queue.size() > 0


func _on_craft_queue_changed() -> void:
	crafting_progress.visible = CraftingManager.craft_queue.size() > 0


func show_tooltip(text: String, position: Vector2) -> void:
	tooltip_label.text = text
	tooltip.position = position + Vector2(16, 16)
	tooltip.visible = true


func hide_tooltip() -> void:
	tooltip.visible = false


func _input(event: InputEvent) -> void:
	# Number keys for hotbar selection
	if event is InputEventKey and event.pressed:
		var key := event.keycode
		if key >= KEY_1 and key <= KEY_9:
			InventoryManager.select_hotbar(key - KEY_1)
		elif key == KEY_0:
			InventoryManager.select_hotbar(9)
