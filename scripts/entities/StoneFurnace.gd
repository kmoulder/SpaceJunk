class_name StoneFurnace
extends BuildingEntity

## StoneFurnace - A 2x2 building that smelts ores into plates using coal
##
## Input: 1 ore + fuel (coal)
## Output: 1 plate (based on recipe)

signal smelting_started(recipe: RecipeResource)
signal smelting_progress_changed(progress: float)
signal smelting_completed(recipe: RecipeResource)

## Fuel slot (separate from internal inventory)
var fuel_slot: ItemStack

## Input slot for ore
var input_slot: ItemStack

## Output slot for smelted items
var output_slot: ItemStack

## Current recipe being processed
var current_recipe: RecipeResource = null

## Remaining fuel burn time
var fuel_burn_remaining: float = 0.0

## Is the furnace currently burning?
var is_burning: bool = false

## Ticks per second for timing
const TICKS_PER_SECOND: float = 60.0


func _ready() -> void:
	super._ready()
	_init_furnace_slots()


func _init_furnace_slots() -> void:
	fuel_slot = ItemStack.new()
	input_slot = ItemStack.new()
	output_slot = ItemStack.new()


func _generate_texture() -> Texture2D:
	return SpriteGenerator.generate_furnace(false)


func _process_building() -> void:
	# Check if we should start a new recipe
	if current_recipe == null:
		_try_start_smelting()

	# Process burning and smelting
	if current_recipe != null:
		_process_smelting()


func _try_start_smelting() -> void:
	if input_slot.is_empty():
		return

	# Find a matching furnace recipe for the input
	var recipes := CraftingManager.get_recipes_for_building(Enums.CraftingType.FURNACE)
	for recipe in recipes:
		if _can_start_recipe(recipe):
			current_recipe = recipe
			crafting_progress = 0.0
			smelting_started.emit(recipe)
			return


func _can_start_recipe(recipe: RecipeResource) -> bool:
	# Check if we have the required input
	var ingredients := recipe.get_ingredients()
	if ingredients.is_empty():
		return false

	var required_item_id: String = ingredients[0]["item_id"]
	var required_count: int = ingredients[0]["count"]

	var required_item := InventoryManager.get_item(required_item_id)
	if required_item == null:
		return false

	if input_slot.item != required_item or input_slot.count < required_count:
		return false

	# Check if output can accept the result
	var results := recipe.get_results()
	if results.is_empty():
		return false

	var result_item_id: String = results[0]["item_id"]
	var result_item := InventoryManager.get_item(result_item_id)
	if result_item == null:
		return false

	if not output_slot.is_empty() and output_slot.item != result_item:
		return false
	if not output_slot.is_empty() and output_slot.is_full():
		return false

	return true


func _process_smelting() -> void:
	# Make sure we have fuel
	if not _ensure_fuel():
		return

	# Progress the smelting
	var tick_progress := 1.0 / (current_recipe.crafting_time * TICKS_PER_SECOND)
	tick_progress *= definition.crafting_speed if definition else 1.0
	crafting_progress += tick_progress

	# Consume fuel
	fuel_burn_remaining -= 1.0 / TICKS_PER_SECOND

	smelting_progress_changed.emit(crafting_progress)

	# Check if smelting is complete
	if crafting_progress >= 1.0:
		_complete_smelting()


func _ensure_fuel() -> bool:
	# If we have burn time remaining, we're good
	if fuel_burn_remaining > 0:
		is_burning = true
		return true

	# Try to consume fuel from fuel slot
	if fuel_slot.is_empty():
		is_burning = false
		return false

	if fuel_slot.item.fuel_value <= 0:
		is_burning = false
		return false

	# Consume one fuel item
	fuel_burn_remaining = fuel_slot.item.fuel_value / 1000.0  # Convert kJ to seconds
	fuel_slot.remove(1)
	if fuel_slot.count <= 0:
		fuel_slot.item = null
	is_burning = true
	return true


func _complete_smelting() -> void:
	if current_recipe == null:
		return

	# Consume input
	var ingredients := current_recipe.get_ingredients()
	if not ingredients.is_empty():
		var consume_count: int = ingredients[0]["count"]
		input_slot.remove(consume_count)
		if input_slot.count <= 0:
			input_slot.item = null

	# Produce output
	var results := current_recipe.get_results()
	if not results.is_empty():
		var result_item_id: String = results[0]["item_id"]
		var result_count: int = results[0]["count"]
		var result_item := InventoryManager.get_item(result_item_id)

		if result_item:
			if output_slot.is_empty():
				output_slot.item = result_item
				output_slot.count = 0
			output_slot.add(result_count)

	smelting_completed.emit(current_recipe)
	current_recipe = null
	crafting_progress = 0.0


## Override: Check if building can accept items
func can_accept_item(item: ItemResource, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	# Accept fuel
	if item.fuel_value > 0:
		if fuel_slot.is_empty() or (fuel_slot.item == item and not fuel_slot.is_full()):
			return true

	# Accept smeltable items
	var recipes := CraftingManager.get_recipes_for_building(Enums.CraftingType.FURNACE)
	for recipe in recipes:
		var ingredients := recipe.get_ingredients()
		if not ingredients.is_empty():
			var required_item_id: String = ingredients[0]["item_id"]
			if item.id == required_item_id:
				if input_slot.is_empty() or (input_slot.item == item and not input_slot.is_full()):
					return true

	return false


## Override: Insert item into furnace
func insert_item(item: ItemResource, count: int = 1, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	# Fuel goes to fuel slot
	if item.fuel_value > 0:
		if fuel_slot.is_empty():
			fuel_slot.item = item
			fuel_slot.count = 0
		if fuel_slot.item == item:
			var overflow := fuel_slot.add(count)
			return overflow < count

	# Ore goes to input slot
	if input_slot.is_empty():
		input_slot.item = item
		input_slot.count = 0
	if input_slot.item == item:
		var overflow := input_slot.add(count)
		return overflow < count

	return false


## Override: Check for output items
func has_output_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	if not output_slot.is_empty():
		return output_slot.item
	return null


## Override: Extract item from output
func extract_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	if output_slot.is_empty():
		return null

	var item := output_slot.item
	output_slot.remove(1)
	if output_slot.count <= 0:
		output_slot.item = null
	return item


## Get current smelting progress (0.0 to 1.0)
func get_smelting_progress() -> float:
	return crafting_progress


## Get current fuel level (0.0 to 1.0)
func get_fuel_level() -> float:
	if fuel_slot.is_empty():
		return 0.0
	return float(fuel_slot.count) / float(fuel_slot.item.stack_size)


## Check if furnace is currently active
func is_active() -> bool:
	return is_burning and current_recipe != null
