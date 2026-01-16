class_name DebrisEntity
extends Area2D

## DebrisEntity - A piece of floating space debris that can be collected
##
## Drifts across the screen and can be clicked to collect resources.

## Type of debris (iron_asteroid, copper_asteroid, etc.)
var debris_type: String = ""

## Drift velocity in pixels per second
var drift_velocity: Vector2 = Vector2.ZERO

## Contents when collected (array of {item_id, count})
var contents: Array = []

## Sprite reference
var sprite: Sprite2D


func _ready() -> void:
	# Set up collision
	collision_layer = 2  # debris layer
	collision_mask = 0
	input_pickable = true


## Initialize the debris with type and contents
func initialize(type: String, item_contents: Array, variation_seed: int = 0) -> void:
	debris_type = type
	contents = item_contents

	# Create collision shape if not exists
	if get_node_or_null("CollisionShape2D") == null:
		var collision := CollisionShape2D.new()
		var shape := CircleShape2D.new()
		shape.radius = Constants.DEBRIS_CLICK_RADIUS
		collision.shape = shape
		add_child(collision)

	# Create sprite if not exists
	if sprite == null:
		sprite = Sprite2D.new()
		add_child(sprite)

	sprite.texture = SpriteGenerator.generate_debris(debris_type, variation_seed)

	# Set z-index
	z_index = Constants.Z_DEBRIS


## Set the drift velocity
func set_drift_velocity(velocity: Vector2) -> void:
	drift_velocity = velocity


## Update movement - called by DebrisManager
func update_movement(delta: float) -> void:
	global_position += drift_velocity * delta


## Get the debris contents
func get_contents() -> Array:
	return contents


## Get the debris type
func get_debris_type() -> String:
	return debris_type
