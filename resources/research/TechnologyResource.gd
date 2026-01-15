@tool
class_name TechnologyResource
extends Resource

## Unique identifier for this technology
@export var id: String = ""

## Display name
@export var name: String = ""

## Description shown in tooltips
@export_multiline var description: String = ""

## Icon for the tech tree
@export var icon: Texture2D

## Prerequisite technology IDs
@export var prerequisites: Array[String] = []

## Science pack requirements (pack_id -> count)
@export var science_pack_ids: Array[String] = []
@export var science_pack_counts: Array[int] = []

## Recipe IDs unlocked by this technology
@export var unlocks_recipe_ids: Array[String] = []

## Building IDs unlocked by this technology
@export var unlocks_building_ids: Array[String] = []

## Position in tech tree for display (optional)
@export var tree_position: Vector2 = Vector2.ZERO

## Get science cost as a dictionary
func get_science_cost() -> Dictionary:
	var result := {}
	for i in range(mini(science_pack_ids.size(), science_pack_counts.size())):
		result[science_pack_ids[i]] = science_pack_counts[i]
	return result

## Get total science packs required
func get_total_science_cost() -> int:
	var total := 0
	for count in science_pack_counts:
		total += count
	return total

func _to_string() -> String:
	return "Technology<%s>" % id
