extends Node

## ResearchManager - Handles the technology tree and research progress
##
## Manages unlocked technologies, research queue, and science pack consumption.

signal technology_registered(tech: TechnologyResource)
signal research_started(tech: TechnologyResource)
signal research_progress(tech: TechnologyResource, progress: float)
signal research_completed(tech: TechnologyResource)
signal technology_unlocked(tech: TechnologyResource)

## All registered technologies by ID
var _tech_registry: Dictionary = {}

## Set of unlocked technology IDs
var unlocked_technologies: Dictionary = {}

## Currently researching technology
var current_research: TechnologyResource = null

## Progress on current research (science packs consumed)
var research_progress_packs: Dictionary = {}

## Whether research is currently active
var is_researching: bool = false


func _ready() -> void:
	_register_default_technologies()


## Register a technology
func register_technology(tech: TechnologyResource) -> void:
	if tech and not tech.id.is_empty():
		_tech_registry[tech.id] = tech
		technology_registered.emit(tech)


## Get a technology by ID
func get_technology(tech_id: String) -> TechnologyResource:
	return _tech_registry.get(tech_id, null)


## Get all technologies
func get_all_technologies() -> Array[TechnologyResource]:
	var result: Array[TechnologyResource] = []
	for tech in _tech_registry.values():
		result.append(tech)
	return result


## Check if a technology is unlocked
func is_technology_unlocked(tech_id: String) -> bool:
	return unlocked_technologies.has(tech_id)


## Check if a technology can be researched
func can_research(tech: TechnologyResource) -> bool:
	if tech == null:
		return false

	# Already unlocked
	if is_technology_unlocked(tech.id):
		return false

	# Check prerequisites
	for prereq_id in tech.prerequisites:
		if not is_technology_unlocked(prereq_id):
			return false

	return true


## Get available technologies (can be researched now)
func get_available_technologies() -> Array[TechnologyResource]:
	var result: Array[TechnologyResource] = []
	for tech in _tech_registry.values():
		if can_research(tech):
			result.append(tech)
	return result


## Start researching a technology
func start_research(tech: TechnologyResource) -> bool:
	if not can_research(tech):
		return false

	# Cancel current research if any
	if current_research != null:
		cancel_research()

	current_research = tech
	research_progress_packs.clear()

	# Initialize progress for each science pack type
	for pack_id in tech.science_pack_ids:
		research_progress_packs[pack_id] = 0

	is_researching = true
	research_started.emit(tech)
	return true


## Cancel current research
func cancel_research() -> void:
	if current_research == null:
		return

	# Refund consumed science packs
	for pack_id in research_progress_packs:
		var consumed: int = research_progress_packs[pack_id]
		if consumed > 0:
			var item := InventoryManager.get_item(pack_id)
			if item:
				InventoryManager.add_item(item, consumed)

	current_research = null
	research_progress_packs.clear()
	is_researching = false


## Add science packs to current research
## Called by labs when they consume science packs
func add_science(pack_id: String, count: int = 1) -> bool:
	if current_research == null:
		return false

	if not research_progress_packs.has(pack_id):
		return false

	# Get required amount for this pack type
	var required := 0
	var cost := current_research.get_science_cost()
	if cost.has(pack_id):
		required = cost[pack_id]

	# Add progress
	var current: int = research_progress_packs[pack_id]
	var to_add := mini(count, required - current)
	research_progress_packs[pack_id] = current + to_add

	# Emit progress
	var progress := get_research_progress()
	research_progress.emit(current_research, progress)

	# Check if research is complete
	if progress >= 1.0:
		_complete_research()

	return to_add > 0


## Get current research progress (0.0 to 1.0)
func get_research_progress() -> float:
	if current_research == null:
		return 0.0

	var cost := current_research.get_science_cost()
	if cost.is_empty():
		return 1.0

	var total_required := 0
	var total_consumed := 0

	for pack_id in cost:
		total_required += cost[pack_id]
		total_consumed += research_progress_packs.get(pack_id, 0)

	if total_required == 0:
		return 1.0

	return float(total_consumed) / float(total_required)


## Complete the current research
func _complete_research() -> void:
	if current_research == null:
		return

	var completed := current_research

	# Mark as unlocked
	unlocked_technologies[completed.id] = true

	# Clear current research
	current_research = null
	research_progress_packs.clear()
	is_researching = false

	research_completed.emit(completed)
	technology_unlocked.emit(completed)


## Unlock a technology directly (for cheats/debugging)
func unlock_technology(tech_id: String) -> void:
	if _tech_registry.has(tech_id):
		unlocked_technologies[tech_id] = true
		technology_unlocked.emit(_tech_registry[tech_id])


## Register default technologies
func _register_default_technologies() -> void:
	# Basic automation
	_create_and_register_tech("automation_1", "Automation",
		"Unlocks Assembler Mk1 and Long Inserter",
		[], ["automation_science"], [10],
		["assembler_mk1", "long_inserter"])

	_create_and_register_tech("logistics_1", "Logistics",
		"Unlocks Underground Belt and Splitter",
		[], ["automation_science"], [10],
		["underground_belt", "splitter"])

	_create_and_register_tech("electronics", "Electronics",
		"Unlocks Electronic Circuit crafting",
		[], ["automation_science"], [15])

	_create_and_register_tech("steel_processing", "Steel Processing",
		"Unlocks Steel Plate smelting",
		["automation_1"], ["automation_science"], [20])

	_create_and_register_tech("automation_2", "Automation 2",
		"Unlocks Assembler Mk2 and Fast Inserter",
		["automation_1", "electronics"], ["automation_science", "logistic_science"], [40, 40],
		["assembler_mk2", "fast_inserter"])

	_create_and_register_tech("logistics_2", "Logistics 2",
		"Unlocks Fast Transport Belt",
		["logistics_1"], ["automation_science", "logistic_science"], [30, 30],
		["fast_belt"])

	_create_and_register_tech("solar_energy", "Solar Energy",
		"Unlocks Solar Panel",
		["electronics"], ["automation_science"], [20],
		["solar_panel"])

	_create_and_register_tech("electric_energy_accumulators", "Electric Energy Accumulators",
		"Unlocks Accumulator",
		["solar_energy"], ["automation_science", "logistic_science"], [30, 30],
		["accumulator"])

	_create_and_register_tech("station_expansion", "Station Expansion",
		"Unlocks Foundation crafting for station expansion",
		["automation_1"], ["automation_science"], [25])


func _create_and_register_tech(id: String, tech_name: String, desc: String,
		prereqs: Array, pack_ids: Array, pack_counts: Array,
		building_unlocks: Array = []) -> void:

	var tech := TechnologyResource.new()
	tech.id = id
	tech.name = tech_name
	tech.description = desc

	for prereq in prereqs:
		tech.prerequisites.append(prereq)

	for pack_id in pack_ids:
		tech.science_pack_ids.append(pack_id)
	for pack_count in pack_counts:
		tech.science_pack_counts.append(pack_count)

	for building_id in building_unlocks:
		tech.unlocks_building_ids.append(building_id)

	register_technology(tech)


## Save research state
func to_save_data() -> Dictionary:
	var unlocked := []
	for tech_id in unlocked_technologies:
		unlocked.append(tech_id)

	return {
		"unlocked": unlocked,
		"current_research": current_research.id if current_research else "",
		"progress": research_progress_packs.duplicate()
	}


## Load research state
func from_save_data(data: Dictionary) -> void:
	unlocked_technologies.clear()
	for tech_id in data.get("unlocked", []):
		unlocked_technologies[tech_id] = true

	var current_id: String = data.get("current_research", "")
	if not current_id.is_empty():
		current_research = get_technology(current_id)
		research_progress_packs = data.get("progress", {})
		is_researching = current_research != null
