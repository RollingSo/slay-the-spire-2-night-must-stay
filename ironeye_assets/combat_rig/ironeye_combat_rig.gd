extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false

func _ready() -> void:
	animation_player.animation_finished.connect(_on_animation_finished)
	animation_player.play("idle_loop")

func play_trigger(trigger: String) -> void:
	match trigger:
		"Attack":
			_play_one_shot("attack")
		"Hit", "Cast":
			_play_one_shot("hit")
		"Dead":
			_return_to_idle = false
			animation_player.play("death")
		"Idle", "Relaxed", "Revive":
			_play_idle()

func _play_one_shot(animation_name: StringName) -> void:
	_return_to_idle = true
	animation_player.play(animation_name)

func _play_idle() -> void:
	_return_to_idle = false
	animation_player.play("idle_loop")

func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"hit"]:
		_play_idle()
