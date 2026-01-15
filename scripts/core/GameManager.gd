extends Node

## GameManager - Central game state management singleton
##
## Handles game state, pause/resume, time management, and coordinates
## between other manager singletons.

signal game_state_changed(new_state: Enums.GameState)
signal game_paused
signal game_resumed
signal game_tick(tick: int)

## Current game state
var game_state: Enums.GameState = Enums.GameState.MENU

## Whether the game is paused
var is_paused: bool = false

## Game speed multiplier (1.0 = normal)
var game_speed: float = 1.0

## Current game tick (increments each tick)
var current_tick: int = 0

## Ticks per second
const TICKS_PER_SECOND: int = 60

## Time accumulator for tick processing
var _tick_accumulator: float = 0.0

## Seconds per tick
var _tick_interval: float = 1.0 / TICKS_PER_SECOND


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS


func _process(delta: float) -> void:
	if is_paused or game_state != Enums.GameState.PLAYING:
		return

	# Accumulate time and process ticks
	_tick_accumulator += delta * game_speed
	while _tick_accumulator >= _tick_interval:
		_tick_accumulator -= _tick_interval
		_process_tick()


func _process_tick() -> void:
	current_tick += 1
	game_tick.emit(current_tick)


## Start a new game
func start_new_game() -> void:
	current_tick = 0
	_tick_accumulator = 0.0
	is_paused = false
	set_game_state(Enums.GameState.PLAYING)


## Set the game state
func set_game_state(new_state: Enums.GameState) -> void:
	if game_state == new_state:
		return
	game_state = new_state
	game_state_changed.emit(new_state)


## Pause the game
func pause() -> void:
	if is_paused:
		return
	is_paused = true
	get_tree().paused = true
	game_paused.emit()


## Resume the game
func resume() -> void:
	if not is_paused:
		return
	is_paused = false
	get_tree().paused = false
	game_resumed.emit()


## Toggle pause state
func toggle_pause() -> void:
	if is_paused:
		resume()
	else:
		pause()


## Set game speed (0.5 to 4.0)
func set_game_speed(speed: float) -> void:
	game_speed = clampf(speed, 0.5, 4.0)


## Get current game time in seconds
func get_game_time() -> float:
	return current_tick * _tick_interval


## Check if we're in a playable state
func is_playing() -> bool:
	return game_state == Enums.GameState.PLAYING and not is_paused


## Handle input for global actions
func _input(event: InputEvent) -> void:
	if event.is_action_pressed("cancel"):
		if game_state == Enums.GameState.PLAYING:
			toggle_pause()
