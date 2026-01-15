class_name ItemStack
extends RefCounted

## The item resource this stack contains
var item: ItemResource

## The count of items in this stack
var count: int

func _init(p_item: ItemResource = null, p_count: int = 0) -> void:
	item = p_item
	count = p_count

## Check if this stack is empty
func is_empty() -> bool:
	return item == null or count <= 0

## Check if this stack is full
func is_full() -> bool:
	if item == null:
		return false
	return count >= item.stack_size

## Get remaining space in this stack
func get_space() -> int:
	if item == null:
		return 0
	return item.stack_size - count

## Try to add items to this stack, returns overflow
func add(amount: int) -> int:
	if item == null:
		return amount

	var space := get_space()
	var to_add := mini(amount, space)
	count += to_add
	return amount - to_add

## Try to remove items from this stack, returns actual removed
func remove(amount: int) -> int:
	var to_remove := mini(amount, count)
	count -= to_remove
	if count <= 0:
		count = 0
	return to_remove

## Check if this stack can merge with another (same item type)
func can_merge_with(other: ItemStack) -> bool:
	if is_empty() or other.is_empty():
		return true
	return item.id == other.item.id

## Merge another stack into this one, returns overflow stack or null
func merge(other: ItemStack) -> ItemStack:
	if other.is_empty():
		return null

	if is_empty():
		item = other.item
		count = other.count
		return null

	if not can_merge_with(other):
		return other

	var overflow := add(other.count)
	if overflow > 0:
		return ItemStack.new(other.item, overflow)
	return null

## Create a copy of this stack
func duplicate_stack() -> ItemStack:
	return ItemStack.new(item, count)

## Split this stack, removing and returning up to amount items
func split(amount: int) -> ItemStack:
	var taken := remove(amount)
	if taken > 0:
		return ItemStack.new(item, taken)
	return null

## Convert to dictionary for saving
func to_dict() -> Dictionary:
	if is_empty():
		return {}
	return {
		"item_id": item.id,
		"count": count
	}

## Create from dictionary (requires item lookup function)
static func from_dict(data: Dictionary, item_lookup: Callable) -> ItemStack:
	if data.is_empty():
		return ItemStack.new()
	var item_res: ItemResource = item_lookup.call(data.get("item_id", ""))
	var cnt: int = data.get("count", 0)
	return ItemStack.new(item_res, cnt)

func _to_string() -> String:
	if is_empty():
		return "ItemStack(empty)"
	return "ItemStack(%s x%d)" % [item.id, count]
