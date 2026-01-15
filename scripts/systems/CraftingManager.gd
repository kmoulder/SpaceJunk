extends Node

## CraftingManager - Handles crafting recipes and hand-crafting queue
##
## Manages available recipes, validates crafting requirements,
## and processes the hand-crafting queue.

signal recipe_registered(recipe: RecipeResource)
signal craft_started(recipe: RecipeResource)
signal craft_progress(recipe: RecipeResource, progress: float)
signal craft_completed(recipe: RecipeResource)
signal craft_cancelled(recipe: RecipeResource)
signal queue_changed

## All registered recipes by ID
var _recipe_registry: Dictionary = {}

## Hand-crafting queue
var craft_queue: Array[RecipeResource] = []

## Current crafting progress (0.0 to 1.0)
var current_progress: float = 0.0

## Whether we're currently crafting
var is_crafting: bool = false


func _ready() -> void:
	_register_default_recipes()


func _process(delta: float) -> void:
	if not GameManager.is_playing():
		return

	_process_craft_queue(delta * GameManager.game_speed)


func _process_craft_queue(delta: float) -> void:
	if craft_queue.is_empty():
		is_crafting = false
		return

	var recipe := craft_queue[0]

	# Start crafting if not already
	if not is_crafting:
		# Check if we still have ingredients
		if not can_craft(recipe):
			# Remove from queue if we can't craft
			craft_queue.pop_front()
			craft_cancelled.emit(recipe)
			queue_changed.emit()
			return

		# Consume ingredients
		_consume_ingredients(recipe)
		is_crafting = true
		craft_started.emit(recipe)

	# Update progress
	var craft_time := recipe.crafting_time * Constants.HAND_CRAFT_SPEED_MULTIPLIER
	current_progress += delta / craft_time
	craft_progress.emit(recipe, current_progress)

	# Check completion
	if current_progress >= 1.0:
		_complete_craft(recipe)


func _complete_craft(recipe: RecipeResource) -> void:
	# Give results to player
	for result in recipe.get_results():
		var item := InventoryManager.get_item(result.item_id)
		if item:
			InventoryManager.add_item(item, result.count)

	craft_completed.emit(recipe)

	# Remove from queue and reset
	craft_queue.pop_front()
	current_progress = 0.0
	is_crafting = false
	queue_changed.emit()


func _consume_ingredients(recipe: RecipeResource) -> void:
	for ingredient in recipe.get_ingredients():
		var item := InventoryManager.get_item(ingredient.item_id)
		if item:
			InventoryManager.remove_item(item, ingredient.count)


## Register a recipe
func register_recipe(recipe: RecipeResource) -> void:
	if recipe and not recipe.id.is_empty():
		_recipe_registry[recipe.id] = recipe
		recipe_registered.emit(recipe)


## Get a recipe by ID
func get_recipe(recipe_id: String) -> RecipeResource:
	return _recipe_registry.get(recipe_id, null)


## Get all registered recipes
func get_all_recipes() -> Array[RecipeResource]:
	var result: Array[RecipeResource] = []
	for recipe in _recipe_registry.values():
		result.append(recipe)
	return result


## Get recipes that can be hand-crafted
func get_hand_recipes() -> Array[RecipeResource]:
	var result: Array[RecipeResource] = []
	for recipe in _recipe_registry.values():
		if recipe.crafting_type == Enums.CraftingType.HAND:
			result.append(recipe)
	return result


## Get recipes available for a building type
func get_recipes_for_building(crafting_type: Enums.CraftingType) -> Array[RecipeResource]:
	var result: Array[RecipeResource] = []
	for recipe in _recipe_registry.values():
		if recipe.crafting_type == crafting_type:
			result.append(recipe)
	return result


## Check if a recipe can be crafted (has ingredients)
func can_craft(recipe: RecipeResource) -> bool:
	if recipe == null:
		return false

	# Check if unlocked
	if not is_recipe_unlocked(recipe):
		return false

	# Check ingredients
	for ingredient in recipe.get_ingredients():
		var item := InventoryManager.get_item(ingredient.item_id)
		if item == null or not InventoryManager.has_item(item, ingredient.count):
			return false

	return true


## Check if a recipe is unlocked
func is_recipe_unlocked(recipe: RecipeResource) -> bool:
	if recipe.enabled_by_default:
		return true

	if recipe.required_technology.is_empty():
		return true

	# Check with ResearchManager if technology is unlocked
	if ResearchManager and ResearchManager.has_method("is_technology_unlocked"):
		return ResearchManager.is_technology_unlocked(recipe.required_technology)

	return false


## Queue a recipe for hand-crafting
func queue_craft(recipe: RecipeResource, count: int = 1) -> int:
	if recipe == null:
		return 0

	if recipe.crafting_type != Enums.CraftingType.HAND:
		return 0

	var queued := 0
	for _i in range(count):
		if can_craft(recipe) or craft_queue.has(recipe):
			craft_queue.append(recipe)
			queued += 1

	if queued > 0:
		queue_changed.emit()

	return queued


## Cancel a queued craft (removes last instance)
func cancel_craft(recipe: RecipeResource) -> bool:
	var idx := craft_queue.rfind(recipe)
	if idx == -1:
		return false

	# If cancelling the current craft, refund ingredients
	if idx == 0 and is_crafting:
		for ingredient in recipe.get_ingredients():
			var item := InventoryManager.get_item(ingredient.item_id)
			if item:
				InventoryManager.add_item(item, ingredient.count)
		is_crafting = false
		current_progress = 0.0

	craft_queue.remove_at(idx)
	craft_cancelled.emit(recipe)
	queue_changed.emit()
	return true


## Cancel all crafting
func cancel_all() -> void:
	# Refund current craft if in progress
	if is_crafting and not craft_queue.is_empty():
		var recipe := craft_queue[0]
		for ingredient in recipe.get_ingredients():
			var item := InventoryManager.get_item(ingredient.item_id)
			if item:
				InventoryManager.add_item(item, ingredient.count)

	for recipe in craft_queue:
		craft_cancelled.emit(recipe)

	craft_queue.clear()
	is_crafting = false
	current_progress = 0.0
	queue_changed.emit()


## Get how many of a recipe are queued
func get_queue_count(recipe: RecipeResource) -> int:
	return craft_queue.count(recipe)


## Get the current crafting recipe
func get_current_recipe() -> RecipeResource:
	if craft_queue.is_empty():
		return null
	return craft_queue[0]


## Register default recipes
func _register_default_recipes() -> void:
	# Smelting recipes (furnace)
	_create_and_register_recipe("smelt_iron", "Iron Plate",
		["iron_ore"], [1], ["iron_plate"], [1],
		Enums.CraftingType.FURNACE, 3.2)

	_create_and_register_recipe("smelt_copper", "Copper Plate",
		["copper_ore"], [1], ["copper_plate"], [1],
		Enums.CraftingType.FURNACE, 3.2)

	_create_and_register_recipe("smelt_stone", "Stone Brick",
		["stone"], [2], ["stone_brick"], [1],
		Enums.CraftingType.FURNACE, 3.2)

	_create_and_register_recipe("smelt_steel", "Steel Plate",
		["iron_plate"], [5], ["steel_plate"], [1],
		Enums.CraftingType.FURNACE, 16.0)

	# Hand-craftable component recipes
	_create_and_register_recipe("craft_iron_gear", "Iron Gear Wheel",
		["iron_plate"], [2], ["iron_gear"], [1],
		Enums.CraftingType.HAND, 0.5)

	_create_and_register_recipe("craft_copper_cable", "Copper Cable",
		["copper_plate"], [1], ["copper_cable"], [2],
		Enums.CraftingType.HAND, 0.5)

	_create_and_register_recipe("craft_circuit", "Electronic Circuit",
		["iron_plate", "copper_cable"], [1, 3], ["electronic_circuit"], [1],
		Enums.CraftingType.HAND, 0.5)


func _create_and_register_recipe(id: String, recipe_name: String,
		ingredient_ids: Array, ingredient_counts: Array,
		result_ids: Array, result_counts: Array,
		crafting_type: Enums.CraftingType, craft_time: float) -> void:

	var recipe := RecipeResource.new()
	recipe.id = id
	recipe.name = recipe_name
	recipe.crafting_type = crafting_type
	recipe.crafting_time = craft_time

	# Convert arrays to typed arrays
	for ing_id in ingredient_ids:
		recipe.ingredient_ids.append(ing_id)
	for ing_count in ingredient_counts:
		recipe.ingredient_counts.append(ing_count)
	for res_id in result_ids:
		recipe.result_ids.append(res_id)
	for res_count in result_counts:
		recipe.result_counts.append(res_count)

	register_recipe(recipe)
