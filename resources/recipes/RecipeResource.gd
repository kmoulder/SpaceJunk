@tool
class_name RecipeResource
extends Resource

## Unique identifier for this recipe
@export var id: String = ""

## Display name
@export var name: String = ""

## Crafting time in seconds
@export var crafting_time: float = 1.0

## What type of building can craft this
@export var crafting_type: Enums.CraftingType = Enums.CraftingType.HAND

## Recipe category for menu organization
@export var category: Enums.RecipeCategory = Enums.RecipeCategory.CRAFTING

## Ingredient item IDs and counts
@export var ingredient_ids: Array[String] = []
@export var ingredient_counts: Array[int] = []

## Result item IDs and counts
@export var result_ids: Array[String] = []
@export var result_counts: Array[int] = []

## Technology required to unlock this recipe (empty = always available)
@export var required_technology: String = ""

## Whether this recipe is enabled by default
@export var enabled_by_default: bool = true

## Get ingredients as an array of dictionaries
func get_ingredients() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for i in range(mini(ingredient_ids.size(), ingredient_counts.size())):
		result.append({
			"item_id": ingredient_ids[i],
			"count": ingredient_counts[i]
		})
	return result

## Get results as an array of dictionaries
func get_results() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	for i in range(mini(result_ids.size(), result_counts.size())):
		result.append({
			"item_id": result_ids[i],
			"count": result_counts[i]
		})
	return result

## Get the number of ingredient types
func get_ingredient_count() -> int:
	return ingredient_ids.size()

func _to_string() -> String:
	return "Recipe<%s>" % id
