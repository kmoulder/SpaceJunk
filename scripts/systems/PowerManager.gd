extends Node

## PowerManager - Handles power production, consumption, and distribution
##
## Tracks all power producers and consumers, calculates satisfaction ratio,
## and manages brownout states.

signal power_changed(production: float, consumption: float)
signal brownout_started
signal brownout_ended
signal producer_registered(building: Node2D, output: float)
signal consumer_registered(building: Node2D, input: float)

## Dictionary of producer buildings -> power output (kW)
var producers: Dictionary = {}

## Dictionary of consumer buildings -> power consumption (kW)
var consumers: Dictionary = {}

## Total power production (kW)
var total_production: float = 0.0

## Total power consumption (kW)
var total_consumption: float = 0.0

## Power satisfaction ratio (0.0 to 1.0)
var satisfaction: float = 1.0

## Whether we're currently in brownout
var is_brownout: bool = false

## Stored energy (for accumulators) in kJ
var stored_energy: float = 0.0

## Maximum energy storage capacity in kJ
var storage_capacity: float = 0.0


func _ready() -> void:
	# Connect to game tick for regular updates
	if GameManager:
		GameManager.game_tick.connect(_on_game_tick)


func _on_game_tick(_tick: int) -> void:
	_update_power_network()


func _update_power_network() -> void:
	# Calculate totals
	var new_production := 0.0
	var new_consumption := 0.0

	for building in producers:
		if is_instance_valid(building):
			new_production += producers[building]
		else:
			producers.erase(building)

	for building in consumers:
		if is_instance_valid(building):
			new_consumption += consumers[building]
		else:
			consumers.erase(building)

	# Check if values changed
	var changed := (new_production != total_production or new_consumption != total_consumption)
	total_production = new_production
	total_consumption = new_consumption

	# Calculate satisfaction
	var old_satisfaction := satisfaction
	if total_consumption <= 0:
		satisfaction = 1.0
	elif total_production >= total_consumption:
		satisfaction = 1.0
		# Charge accumulators with excess
		var excess := total_production - total_consumption
		_charge_storage(excess / 60.0)  # Per tick (60 ticks/sec)
	else:
		# Try to draw from storage
		var deficit := total_consumption - total_production
		var from_storage := _discharge_storage(deficit / 60.0)
		var effective_production := total_production + from_storage * 60.0

		if total_consumption > 0:
			satisfaction = clampf(effective_production / total_consumption, 0.0, 1.0)
		else:
			satisfaction = 1.0

	# Check brownout state
	var was_brownout := is_brownout
	is_brownout = satisfaction < 1.0

	if is_brownout and not was_brownout:
		brownout_started.emit()
	elif not is_brownout and was_brownout:
		brownout_ended.emit()

	if changed:
		power_changed.emit(total_production, total_consumption)


func _charge_storage(amount_kj: float) -> void:
	if storage_capacity <= 0:
		return
	stored_energy = minf(stored_energy + amount_kj, storage_capacity)


func _discharge_storage(amount_kj: float) -> float:
	if stored_energy <= 0:
		return 0.0
	var discharged := minf(amount_kj, stored_energy)
	stored_energy -= discharged
	return discharged


## Register a power producer
func register_producer(building: Node2D, output_kw: float) -> void:
	if building == null or output_kw <= 0:
		return

	producers[building] = output_kw
	producer_registered.emit(building, output_kw)
	_update_power_network()


## Unregister a power producer
func unregister_producer(building: Node2D) -> void:
	if producers.has(building):
		producers.erase(building)
		_update_power_network()


## Update a producer's output
func update_producer(building: Node2D, output_kw: float) -> void:
	if building == null:
		return

	if output_kw <= 0:
		unregister_producer(building)
	else:
		producers[building] = output_kw
		_update_power_network()


## Register a power consumer
func register_consumer(building: Node2D, consumption_kw: float) -> void:
	if building == null or consumption_kw <= 0:
		return

	consumers[building] = consumption_kw
	consumer_registered.emit(building, consumption_kw)
	_update_power_network()


## Unregister a power consumer
func unregister_consumer(building: Node2D) -> void:
	if consumers.has(building):
		consumers.erase(building)
		_update_power_network()


## Update a consumer's consumption
func update_consumer(building: Node2D, consumption_kw: float) -> void:
	if building == null:
		return

	if consumption_kw <= 0:
		unregister_consumer(building)
	else:
		consumers[building] = consumption_kw
		_update_power_network()


## Add storage capacity (when accumulator is placed)
func add_storage_capacity(capacity_kj: float) -> void:
	storage_capacity += capacity_kj


## Remove storage capacity (when accumulator is removed)
func remove_storage_capacity(capacity_kj: float) -> void:
	storage_capacity = maxf(0.0, storage_capacity - capacity_kj)
	stored_energy = minf(stored_energy, storage_capacity)


## Get the effective power for a consumer (accounting for brownout)
func get_effective_power(base_consumption: float) -> float:
	return base_consumption * satisfaction


## Check if there's enough power for a specific consumption
func has_power_for(consumption_kw: float) -> bool:
	return satisfaction >= 1.0 or (total_production - total_consumption) >= consumption_kw


## Get power statistics
func get_stats() -> Dictionary:
	return {
		"production": total_production,
		"consumption": total_consumption,
		"satisfaction": satisfaction,
		"is_brownout": is_brownout,
		"stored_energy": stored_energy,
		"storage_capacity": storage_capacity,
		"producer_count": producers.size(),
		"consumer_count": consumers.size()
	}


## Clear all power network data
func clear() -> void:
	producers.clear()
	consumers.clear()
	total_production = 0.0
	total_consumption = 0.0
	satisfaction = 1.0
	is_brownout = false
	stored_energy = 0.0
	storage_capacity = 0.0


## Save power state
func to_save_data() -> Dictionary:
	return {
		"stored_energy": stored_energy
	}


## Load power state
func from_save_data(data: Dictionary) -> void:
	stored_energy = data.get("stored_energy", 0.0)
