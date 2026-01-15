extends Node

## InventoryManager - Handles player inventory and item management
##
## Manages the player's inventory slots, hotbar, and provides
## utilities for adding/removing/querying items.

signal inventory_changed
signal item_added(item: ItemResource, count: int, slot: int)
signal item_removed(item: ItemResource, count: int, slot: int)
signal hotbar_changed
signal slot_selected(slot_index: int)

## Player inventory slots
var inventory: Array[ItemStack] = []

## Hotbar slots (references to inventory or separate)
var hotbar: Array[ItemStack] = []

## Currently selected hotbar slot
var selected_hotbar_slot: int = 0

## All registered item resources by ID
var _item_registry: Dictionary = {}


func _ready() -> void:
	_initialize_inventory()
	_register_default_items()


func _initialize_inventory() -> void:
	# Create empty inventory slots
	inventory.clear()
	for _i in range(Constants.PLAYER_INVENTORY_SLOTS):
		inventory.append(ItemStack.new())

	# Create empty hotbar slots
	hotbar.clear()
	for _i in range(Constants.HOTBAR_SLOTS):
		hotbar.append(ItemStack.new())


## Register an item resource for lookup
func register_item(item: ItemResource) -> void:
	if item and not item.id.is_empty():
		_item_registry[item.id] = item


## Get an item resource by ID
func get_item(item_id: String) -> ItemResource:
	return _item_registry.get(item_id, null)


## Add item to inventory, returns overflow count
func add_item(item: ItemResource, count: int = 1) -> int:
	if item == null or count <= 0:
		return count

	var remaining := count

	# First, try to add to existing stacks
	for i in range(inventory.size()):
		if remaining <= 0:
			break
		var slot := inventory[i]
		if slot.item == item and not slot.is_full():
			var overflow := slot.add(remaining)
			var added := remaining - overflow
			remaining = overflow
			if added > 0:
				item_added.emit(item, added, i)

	# Then, try to add to empty slots
	for i in range(inventory.size()):
		if remaining <= 0:
			break
		var slot := inventory[i]
		if slot.is_empty():
			slot.item = item
			slot.count = 0
			var overflow := slot.add(remaining)
			var added := remaining - overflow
			remaining = overflow
			if added > 0:
				item_added.emit(item, added, i)

	if remaining < count:
		inventory_changed.emit()

	return remaining


## Remove item from inventory, returns true if successful
func remove_item(item: ItemResource, count: int = 1) -> bool:
	if not has_item(item, count):
		return false

	var remaining := count

	# Remove from slots (prefer non-full stacks first for efficiency)
	for i in range(inventory.size()):
		if remaining <= 0:
			break
		var slot := inventory[i]
		if slot.item == item:
			var removed := slot.remove(remaining)
			remaining -= removed
			if removed > 0:
				item_removed.emit(item, removed, i)
				if slot.count <= 0:
					slot.item = null

	inventory_changed.emit()
	return remaining <= 0


## Check if player has at least count of item
func has_item(item: ItemResource, count: int = 1) -> bool:
	return get_item_count(item) >= count


## Get total count of an item in inventory
func get_item_count(item: ItemResource) -> int:
	if item == null:
		return 0

	var total := 0
	for slot in inventory:
		if slot.item == item:
			total += slot.count
	return total


## Check if inventory has space for an item
func has_space_for(item: ItemResource, count: int = 1) -> bool:
	var remaining := count

	# Check existing stacks
	for slot in inventory:
		if slot.item == item:
			remaining -= slot.get_space()
		elif slot.is_empty():
			remaining -= item.stack_size
		if remaining <= 0:
			return true

	return remaining <= 0


## Get the first slot containing an item
func find_item_slot(item: ItemResource) -> int:
	for i in range(inventory.size()):
		if inventory[i].item == item:
			return i
	return -1


## Get inventory slot at index
func get_slot(index: int) -> ItemStack:
	if index >= 0 and index < inventory.size():
		return inventory[index]
	return null


## Set inventory slot (for drag-drop, etc.)
func set_slot(index: int, stack: ItemStack) -> void:
	if index >= 0 and index < inventory.size():
		inventory[index] = stack
		inventory_changed.emit()


## Swap two inventory slots
func swap_slots(index_a: int, index_b: int) -> void:
	if index_a < 0 or index_a >= inventory.size():
		return
	if index_b < 0 or index_b >= inventory.size():
		return

	var temp := inventory[index_a]
	inventory[index_a] = inventory[index_b]
	inventory[index_b] = temp
	inventory_changed.emit()


## Select a hotbar slot
func select_hotbar(index: int) -> void:
	if index >= 0 and index < Constants.HOTBAR_SLOTS:
		selected_hotbar_slot = index
		slot_selected.emit(index)


## Get currently selected hotbar item
func get_selected_item() -> ItemStack:
	return hotbar[selected_hotbar_slot]


## Assign an inventory slot to a hotbar slot
func assign_to_hotbar(inventory_index: int, hotbar_index: int) -> void:
	if inventory_index < 0 or inventory_index >= inventory.size():
		return
	if hotbar_index < 0 or hotbar_index >= Constants.HOTBAR_SLOTS:
		return

	# For now, hotbar is just a reference copy
	hotbar[hotbar_index] = inventory[inventory_index].duplicate_stack()
	hotbar_changed.emit()


## Clear the entire inventory
func clear_inventory() -> void:
	for slot in inventory:
		slot.item = null
		slot.count = 0
	for slot in hotbar:
		slot.item = null
		slot.count = 0
	inventory_changed.emit()
	hotbar_changed.emit()


## Get all items as a list of {item, count} dictionaries
func get_all_items() -> Array[Dictionary]:
	var result: Array[Dictionary] = []
	var counted := {}

	for slot in inventory:
		if not slot.is_empty():
			var id := slot.item.id
			if counted.has(id):
				counted[id] += slot.count
			else:
				counted[id] = slot.count

	for id in counted:
		result.append({
			"item": get_item(id),
			"count": counted[id]
		})

	return result


## Register default items (called on ready)
func _register_default_items() -> void:
	# These will be replaced with loaded resources later
	# For now, create basic items programmatically
	_create_and_register_item("iron_ore", "Iron Ore", Enums.ItemCategory.RAW_MATERIAL,
		Constants.COLOR_IRON_ORE, 50)
	_create_and_register_item("copper_ore", "Copper Ore", Enums.ItemCategory.RAW_MATERIAL,
		Constants.COLOR_COPPER_ORE, 50)
	_create_and_register_item("stone", "Stone", Enums.ItemCategory.RAW_MATERIAL,
		Constants.COLOR_STONE, 50)
	_create_and_register_item("coal", "Coal", Enums.ItemCategory.RAW_MATERIAL,
		Constants.COLOR_COAL, 50, 4000.0)  # 4 MJ fuel value

	_create_and_register_item("iron_plate", "Iron Plate", Enums.ItemCategory.PROCESSED,
		Constants.COLOR_IRON_PLATE, 100)
	_create_and_register_item("copper_plate", "Copper Plate", Enums.ItemCategory.PROCESSED,
		Constants.COLOR_COPPER_PLATE, 100)
	_create_and_register_item("stone_brick", "Stone Brick", Enums.ItemCategory.PROCESSED,
		Constants.COLOR_STONE_BRICK, 100)
	_create_and_register_item("steel_plate", "Steel Plate", Enums.ItemCategory.PROCESSED,
		Constants.COLOR_STEEL, 100)

	_create_and_register_item("iron_gear", "Iron Gear Wheel", Enums.ItemCategory.COMPONENT,
		Constants.COLOR_IRON_PLATE, 100)
	_create_and_register_item("copper_cable", "Copper Cable", Enums.ItemCategory.COMPONENT,
		Constants.COLOR_COPPER_PLATE, 200)
	_create_and_register_item("electronic_circuit", "Electronic Circuit", Enums.ItemCategory.COMPONENT,
		Constants.COLOR_CIRCUIT_GREEN, 200)

	_create_and_register_item("scrap_metal", "Scrap Metal", Enums.ItemCategory.RAW_MATERIAL,
		Color(0.5, 0.5, 0.55), 50)
	_create_and_register_item("ice", "Ice", Enums.ItemCategory.RAW_MATERIAL,
		Color(0.7, 0.85, 0.95), 50)


func _create_and_register_item(id: String, item_name: String, category: Enums.ItemCategory,
		color: Color, stack_size: int, fuel_value: float = 0.0) -> void:
	var item := ItemResource.new()
	item.id = id
	item.name = item_name
	item.category = category
	item.sprite_color = color
	item.stack_size = stack_size
	item.fuel_value = fuel_value
	register_item(item)


## Convert inventory to save data
func to_save_data() -> Dictionary:
	var slots := []
	for slot in inventory:
		slots.append(slot.to_dict())
	return {
		"inventory": slots,
		"selected_hotbar": selected_hotbar_slot
	}


## Load inventory from save data
func from_save_data(data: Dictionary) -> void:
	_initialize_inventory()
	var slots: Array = data.get("inventory", [])
	for i in range(mini(slots.size(), inventory.size())):
		inventory[i] = ItemStack.from_dict(slots[i], get_item)
	selected_hotbar_slot = data.get("selected_hotbar", 0)
	inventory_changed.emit()
