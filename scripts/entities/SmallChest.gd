class_name SmallChest
extends BuildingEntity

## SmallChest - A 1x1 storage building with 16 slots
##
## Inserters can put and take items from all sides.

const CHEST_SLOTS: int = 16


func _ready() -> void:
	super._ready()


func _generate_texture() -> Texture2D:
	return SpriteGenerator.generate_chest(Color(0.6, 0.5, 0.3))


func _setup_inventory() -> void:
	internal_inventory.clear()
	for _i in range(CHEST_SLOTS):
		internal_inventory.append(ItemStack.new())


## Override: Chests can accept any non-fluid item
func can_accept_item(item: ItemResource, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if item.is_fluid:
		return false

	for slot in internal_inventory:
		if slot.is_empty():
			return true
		if slot.item == item and not slot.is_full():
			return true

	return false


## Override: Insert item into chest
func insert_item(item: ItemResource, count: int = 1, _from_direction: Enums.Direction = Enums.Direction.NORTH) -> bool:
	if item.is_fluid:
		return false

	var remaining := count

	# Try to add to existing stacks first
	for slot in internal_inventory:
		if slot.item == item and not slot.is_full():
			remaining = slot.add(remaining)
			if remaining <= 0:
				return true

	# Try to add to empty slots
	for slot in internal_inventory:
		if slot.is_empty():
			slot.item = item
			remaining = slot.add(remaining)
			if remaining <= 0:
				return true

	return remaining < count


## Override: Check for items to extract
func has_output_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	# Return the first non-empty slot's item
	for slot in internal_inventory:
		if not slot.is_empty():
			return slot.item
	return null


## Override: Extract item from chest
func extract_item(_to_direction: Enums.Direction = Enums.Direction.NORTH) -> ItemResource:
	# Extract from first non-empty slot
	for slot in internal_inventory:
		if not slot.is_empty():
			var item := slot.item
			slot.remove(1)
			if slot.count <= 0:
				slot.item = null
			return item
	return null


## Get a specific slot
func get_slot(index: int) -> ItemStack:
	if index >= 0 and index < internal_inventory.size():
		return internal_inventory[index]
	return null


## Get total number of a specific item
func get_item_count(item: ItemResource) -> int:
	var total := 0
	for slot in internal_inventory:
		if slot.item == item:
			total += slot.count
	return total


## Get total number of items in chest
func get_total_item_count() -> int:
	var total := 0
	for slot in internal_inventory:
		total += slot.count
	return total


## Check how many empty slots remain
func get_empty_slot_count() -> int:
	var empty := 0
	for slot in internal_inventory:
		if slot.is_empty():
			empty += 1
	return empty


## Check if chest is completely empty
func is_empty() -> bool:
	for slot in internal_inventory:
		if not slot.is_empty():
			return false
	return true


## Check if chest is completely full
func is_full() -> bool:
	for slot in internal_inventory:
		if slot.is_empty() or not slot.is_full():
			return false
	return true
