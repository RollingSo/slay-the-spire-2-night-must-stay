extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false
var _block_tween: Tween
var _block_sprite: Sprite2D

func _ready() -> void:
	_block_sprite = Sprite2D.new()
	_block_sprite.name = "Block"
	_block_sprite.texture = load("res://duchess_assets/combat_rig/duchess_block.png")
	_block_sprite.scale = $PoseRoot/Idle.scale
	_block_sprite.visible = false
	$PoseRoot.add_child(_block_sprite)
	animation_player.animation_finished.connect(_on_animation_finished)
	animation_player.play("idle_loop")

func play_trigger(trigger: String) -> void:
	if _block_tween != null and _block_tween.is_running():
		if trigger in ["Cast", "Idle", "Relaxed"]:
			return
		_block_tween.kill()
		_block_sprite.visible = false
	match trigger:
		"Block":
			_play_block()
		"Attack":
			_play_one_shot("attack")
		"Hit":
			_play_one_shot("hit")
		"Cast":
			_play_one_shot("cast")
		"Dead":
			_return_to_idle = false
			animation_player.play("death")
		"Idle", "Relaxed", "Revive":
			_play_idle()

func _play_one_shot(animation_name: StringName) -> void:
	_return_to_idle = true
	animation_player.play("RESET")
	animation_player.advance(0)
	animation_player.play(animation_name)

func _play_idle() -> void:
	_return_to_idle = false
	animation_player.play("RESET")
	animation_player.advance(0)
	animation_player.play("idle_loop")
	if _block_sprite != null:
		_block_sprite.visible = false

func _play_block() -> void:
	animation_player.play("RESET")
	animation_player.advance(0)
	animation_player.stop()
	$PoseRoot/Idle.visible = false
	$PoseRoot/Attack.visible = false
	$PoseRoot/Hit.visible = false
	_block_sprite.visible = true
	_block_sprite.rotation = -0.025
	_block_tween = create_tween()
	_block_tween.tween_property(_block_sprite, "rotation", 0.015, 0.18)
	_block_tween.tween_interval(0.22)
	_block_tween.tween_property(_block_sprite, "rotation", 0.0, 0.15)
	_block_tween.tween_callback(_play_idle)

func set_phase_visual(opacity: float, shimmer: float) -> void:
	for sprite in $PoseRoot.get_children():
		if sprite is Sprite2D:
			sprite.self_modulate = Color(1.0-0.22*shimmer, 1.0-0.08*shimmer, 1.0, opacity)

func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"hit", &"cast"]:
		_play_idle()
