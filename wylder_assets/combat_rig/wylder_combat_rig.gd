extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false
var _block_tween: Tween
var _block_sprite: Sprite2D
var _claw_sprite: Sprite2D
var _ultimate_sprite: Sprite2D
var _action_tween: Tween
var _death_sprite: Sprite2D
var _defeated := false
var _pursuit_active := false
@onready var _normal_idle_texture: Texture2D = $PoseRoot/Idle.texture
@onready var _pursuit_idle_texture: Texture2D = load("res://wylder_assets/combat_rig/wylder_pursuit_idle.png")
@onready var _normal_attack_texture: Texture2D = $PoseRoot/Attack.texture
@onready var _pursuit_attack_texture: Texture2D = load("res://wylder_assets/combat_rig/wylder_pursuit_attack.png")

func _ready() -> void:
	_block_sprite = Sprite2D.new()
	_block_sprite.name = "Block"
	_block_sprite.texture = load("res://wylder_assets/combat_rig/wylder_block.png")
	_block_sprite.scale = $PoseRoot/Idle.scale
	_block_sprite.visible = false
	$PoseRoot.add_child(_block_sprite)
	_claw_sprite = Sprite2D.new()
	_claw_sprite.name = "ClawShot"
	_claw_sprite.texture = load("res://wylder_assets/combat_rig/wylder_claw_shot.png")
	_claw_sprite.scale = Vector2(0.344, 0.344)
	_claw_sprite.visible = false
	$PoseRoot.add_child(_claw_sprite)
	_ultimate_sprite = Sprite2D.new()
	_ultimate_sprite.name = "Ultimate"
	_ultimate_sprite.texture = load("res://wylder_assets/combat_rig/wylder_ultimate.png")
	_ultimate_sprite.scale = Vector2(0.294, 0.294)
	_ultimate_sprite.visible = false
	$PoseRoot.add_child(_ultimate_sprite)
	_death_sprite = Sprite2D.new()
	_death_sprite.name = "Defeated"
	_death_sprite.texture = load("res://wylder_assets/combat_rig/wylder_death.png")
	_death_sprite.scale = $PoseRoot/Idle.scale
	_death_sprite.visible = false
	$PoseRoot.add_child(_death_sprite)
	animation_player.animation_finished.connect(_on_animation_finished)
	animation_player.play("idle_loop")

func play_trigger(trigger: String) -> void:
	if _defeated and trigger != "Revive":
		return
	if _block_tween != null and _block_tween.is_running():
		if trigger in ["Cast", "Idle", "Relaxed"]:
			return
		_block_tween.kill()
		_block_sprite.visible = false
	match trigger:
		"Block", "BucklerBlock":
			_play_block()
		"Attack", "AttackSingle", "AttackTwoHand":
			# Latch the state at attack start. Consuming pursuit during this
			# one-shot must not replace its already-selected two-handed pose.
			var two_handed := trigger == "AttackTwoHand" or (trigger == "Attack" and _pursuit_active)
			$PoseRoot/Attack.texture = _pursuit_attack_texture if two_handed else _normal_attack_texture
			_play_one_shot("attack")
		"ClawShot":
			_play_device_pose(_claw_sprite, false)
		"Ultimate":
			_play_device_pose(_ultimate_sprite, true)
		"Hit":
			_play_one_shot("hit")
		"Cast":
			_play_one_shot("cast")
		"Dead":
			_defeated = true
			_return_to_idle = false
			animation_player.play("RESET")
			animation_player.advance(0)
			animation_player.stop()
			for sprite in $PoseRoot.get_children():
				if sprite is Sprite2D:
					sprite.visible = sprite == _death_sprite
		"Idle", "Relaxed", "Revive":
			_defeated = false
			_play_idle()

func set_pursuit_active(active: bool) -> void:
	_pursuit_active = active
	# Ongoing one-shots retain their attack-start texture; death stays untouched.
	$PoseRoot/Idle.texture = _pursuit_idle_texture if active else _normal_idle_texture

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
	if _death_sprite != null:
		_death_sprite.visible = false
	if _claw_sprite != null:
		_claw_sprite.visible = false
	if _ultimate_sprite != null:
		_ultimate_sprite.visible = false

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

func _play_device_pose(sprite: Sprite2D, ultimate: bool) -> void:
	animation_player.play("RESET")
	animation_player.advance(0)
	animation_player.stop()
	if _action_tween != null and _action_tween.is_running():
		_action_tween.kill()
	for child in $PoseRoot.get_children():
		if child is Sprite2D:
			child.visible = child == sprite
	$PoseRoot.position = Vector2(-8, -154)
	$PoseRoot.rotation = -0.018
	_action_tween = create_tween()
	_action_tween.tween_property($PoseRoot, "position", Vector2(10, -157), 0.16)
	_action_tween.tween_interval(0.08 if ultimate else 0.03)
	_action_tween.tween_property($PoseRoot, "position", Vector2(-34 if ultimate else -18, -153), 0.13)
	_action_tween.parallel().tween_property($PoseRoot, "rotation", -0.045 if ultimate else -0.03, 0.13)
	_action_tween.tween_property($PoseRoot, "position", Vector2(0, -154), 0.27)
	_action_tween.parallel().tween_property($PoseRoot, "rotation", 0.0, 0.27)
	_action_tween.tween_callback(_play_idle)

func set_phase_visual(opacity: float, shimmer: float) -> void:
	for sprite in $PoseRoot.get_children():
		if sprite is Sprite2D:
			sprite.self_modulate = Color(1.0-0.22*shimmer, 1.0-0.08*shimmer, 1.0, opacity)

func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"hit", &"cast"]:
		_play_idle()
