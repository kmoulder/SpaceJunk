extends CanvasLayer

## BuildMenuUI - UI for selecting buildings to place
##
## Shows available buildings organized by category.
## Toggle with 'B' key.

@onready var panel: PanelContainer = $Panel
@onready var categories_container: HBoxContainer = $Panel/MarginContainer/VBoxContainer/Categories
@onready var buildings_container: GridContainer = $Panel/MarginContainer/VBoxContainer/Buildings
@onready var info_panel: PanelContainer = $Panel/MarginContainer/VBoxContainer/InfoPanel
@onready var info_label: Label = $Panel/MarginContainer/VBoxContainer/InfoPanel/InfoLabel

## Currently selected category
var selected_category: Enums.BuildingCategory = Enums.BuildingCategory.PROCESSING

## Category buttons
var category_buttons: Dictionary = {}

## Building buttons
var building_buttons: Array[Button] = []


func _ready() -> void:
	_setup_ui()
	_connect_signals()
	visible = false


func _setup_ui() -> void:
	# Create main panel if not in scene
	if panel == null:
		_create_ui_structure()

	_setup_categories()
	_update_buildings_display()


func _create_ui_structure() -> void:
	# Create the UI programmatically
	panel = PanelContainer.new()
	panel.name = "Panel"
	add_child(panel)

	# Style the panel
	var style := StyleBoxFlat.new()
	style.bg_color = Constants.UI_BACKGROUND
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(8)
	panel.add_theme_stylebox_override("panel", style)

	# Position panel
	panel.position = Vector2(20, 100)
	panel.custom_minimum_size = Vector2(300, 400)

	# Main margin container
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 10)
	margin.add_theme_constant_override("margin_right", 10)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	# Main VBox
	var vbox := VBoxContainer.new()
	vbox.name = "VBoxContainer"
	vbox.add_theme_constant_override("separation", 10)
	margin.add_child(vbox)

	# Title
	var title := Label.new()
	title.text = "Build Menu (B)"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 18)
	vbox.add_child(title)

	# Categories
	categories_container = HBoxContainer.new()
	categories_container.name = "Categories"
	categories_container.add_theme_constant_override("separation", 5)
	vbox.add_child(categories_container)

	# Separator
	var sep := HSeparator.new()
	vbox.add_child(sep)

	# Buildings grid
	buildings_container = GridContainer.new()
	buildings_container.name = "Buildings"
	buildings_container.columns = 4
	buildings_container.add_theme_constant_override("h_separation", 8)
	buildings_container.add_theme_constant_override("v_separation", 8)
	vbox.add_child(buildings_container)

	# Info panel
	info_panel = PanelContainer.new()
	info_panel.name = "InfoPanel"
	info_panel.custom_minimum_size = Vector2(0, 80)
	vbox.add_child(info_panel)

	var info_style := StyleBoxFlat.new()
	info_style.bg_color = Color(0.1, 0.1, 0.15, 0.9)
	info_style.set_corner_radius_all(4)
	info_panel.add_theme_stylebox_override("panel", info_style)

	info_label = Label.new()
	info_label.name = "InfoLabel"
	info_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	info_label.add_theme_font_size_override("font_size", 12)
	info_panel.add_child(info_label)


func _setup_categories() -> void:
	# Clear existing
	for child in categories_container.get_children():
		child.queue_free()
	category_buttons.clear()

	# Add category buttons for categories that have buildings
	var categories_to_show := [
		Enums.BuildingCategory.PROCESSING,
		Enums.BuildingCategory.STORAGE,
		Enums.BuildingCategory.TRANSPORT,
		Enums.BuildingCategory.POWER,
	]

	for category in categories_to_show:
		var btn := Button.new()
		btn.text = _get_category_name(category)
		btn.toggle_mode = true
		btn.button_pressed = (category == selected_category)
		btn.pressed.connect(_on_category_selected.bind(category))

		var btn_style := StyleBoxFlat.new()
		btn_style.bg_color = Color(0.2, 0.2, 0.25)
		btn_style.set_corner_radius_all(4)
		btn.add_theme_stylebox_override("normal", btn_style)

		var btn_style_pressed := StyleBoxFlat.new()
		btn_style_pressed.bg_color = Constants.UI_HIGHLIGHT
		btn_style_pressed.set_corner_radius_all(4)
		btn.add_theme_stylebox_override("pressed", btn_style_pressed)

		categories_container.add_child(btn)
		category_buttons[category] = btn


func _get_category_name(category: Enums.BuildingCategory) -> String:
	match category:
		Enums.BuildingCategory.COLLECTION: return "Collect"
		Enums.BuildingCategory.TRANSPORT: return "Transport"
		Enums.BuildingCategory.PROCESSING: return "Process"
		Enums.BuildingCategory.STORAGE: return "Storage"
		Enums.BuildingCategory.POWER: return "Power"
		Enums.BuildingCategory.RESEARCH: return "Research"
		Enums.BuildingCategory.LOGISTICS: return "Logistics"
		Enums.BuildingCategory.FOUNDATION: return "Foundation"
	return "Unknown"


func _update_buildings_display() -> void:
	# Clear existing
	for child in buildings_container.get_children():
		child.queue_free()
	building_buttons.clear()

	# Get buildings for selected category
	var buildings := BuildingManager.get_buildings_by_category(selected_category)

	for building in buildings:
		# Check if technology is unlocked (skip tech-locked buildings)
		if not building.required_technology.is_empty():
			if not ResearchManager.is_technology_unlocked(building.required_technology):
				continue

		var btn := _create_building_button(building)
		buildings_container.add_child(btn)
		building_buttons.append(btn)

	# Update info to show first building or empty
	if building_buttons.size() > 0:
		_update_info(buildings[0])
	else:
		info_label.text = "No buildings available in this category."


func _create_building_button(building: BuildingResource) -> Button:
	var btn := Button.new()
	btn.custom_minimum_size = Vector2(56, 56)
	btn.tooltip_text = building.name

	# Add icon
	var icon := TextureRect.new()
	icon.texture = _get_building_icon(building)
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.custom_minimum_size = Vector2(48, 48)
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	btn.add_child(icon)

	# Style
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.2, 0.2, 0.25)
	style.border_color = Constants.UI_BORDER
	style.set_border_width_all(2)
	style.set_corner_radius_all(4)
	btn.add_theme_stylebox_override("normal", style)

	var hover_style := StyleBoxFlat.new()
	hover_style.bg_color = Color(0.3, 0.3, 0.35)
	hover_style.border_color = Constants.UI_HIGHLIGHT
	hover_style.set_border_width_all(2)
	hover_style.set_corner_radius_all(4)
	btn.add_theme_stylebox_override("hover", hover_style)

	# Connect signals
	btn.pressed.connect(_on_building_selected.bind(building))
	btn.mouse_entered.connect(_on_building_hovered.bind(building))

	return btn


func _get_building_icon(building: BuildingResource) -> Texture2D:
	match building.id:
		"stone_furnace":
			return SpriteGenerator.generate_furnace(false)
		"electric_furnace":
			return SpriteGenerator.generate_furnace(true)
		"small_chest":
			return SpriteGenerator.generate_chest(Color(0.6, 0.5, 0.3))
		"transport_belt":
			return SpriteGenerator.generate_belt(Enums.Direction.EAST)
		"inserter":
			return SpriteGenerator.generate_inserter(false)
		"long_inserter":
			return SpriteGenerator.generate_inserter(true)
		"solar_panel":
			return SpriteGenerator.generate_solar_panel()
		_:
			return SpriteGenerator.generate_building(Color(0.4, 0.4, 0.5), building.size)


func _update_info(building: BuildingResource) -> void:
	var text := "[b]%s[/b]\n" % building.name
	text += building.description + "\n\n"

	# Show build cost
	var cost := building.get_build_cost()
	if not cost.is_empty():
		text += "Cost: "
		var cost_parts: Array[String] = []
		for entry: Dictionary in cost:
			var item := InventoryManager.get_item(entry["item_id"])
			var item_name: String = item.name if item else str(entry["item_id"])
			var has_count: int = InventoryManager.get_item_count(item) if item else 0
			var need_count: int = entry["count"]
			var color: String = "green" if has_count >= need_count else "red"
			cost_parts.append("[color=%s]%d[/color] %s" % [color, need_count, item_name])
		text += ", ".join(cost_parts)

	info_label.text = text


func _connect_signals() -> void:
	BuildingManager.build_mode_changed.connect(_on_build_mode_changed)


func _on_category_selected(category: Enums.BuildingCategory) -> void:
	selected_category = category

	# Update button states
	for cat in category_buttons:
		category_buttons[cat].button_pressed = (cat == selected_category)

	_update_buildings_display()


func _on_building_selected(building: BuildingResource) -> void:
	# Enter build mode with this building
	BuildingManager.enter_build_mode(building)
	visible = false


func _on_building_hovered(building: BuildingResource) -> void:
	_update_info(building)


func _on_build_mode_changed(enabled: bool, _building: BuildingResource) -> void:
	if enabled:
		visible = false


func toggle() -> void:
	visible = not visible
	if visible:
		_update_buildings_display()


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("build_menu"):
		toggle()
		get_viewport().set_input_as_handled()

	if event.is_action_pressed("cancel") and visible:
		visible = false
		get_viewport().set_input_as_handled()
