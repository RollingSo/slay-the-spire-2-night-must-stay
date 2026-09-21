extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false
var _next_attack_motion := ""

func _ready() -> void:
	animation_player.animation_finished.connect(_on_animation_finished)
	animation_player.play("idle_loop")

func play_trigger(trigger: String) -> void:
	match trigger:
		"Attack", "Cast":
			var motion := _next_attack_motion
			_next_attack_motion = ""
			if motion == "Prayer":
				_play_one_shot("prayer_attack")
			elif motion == "Call" or motion == "Resonance":
				_play_one_shot("attack")
			else:
				_play_one_shot("claw_attack")
		"Call", "Resonance":
			_play_one_shot("attack")
		"Prayer":
			_play_one_shot("prayer_attack")
		"Hit":
			_play_one_shot("hit")
		"Dead":
			_return_to_idle = false
			_hide_special_poses()
			animation_player.play("death")
		"Idle", "Relaxed", "Revive":
			_play_idle()

func queue_attack_motion(motion: String) -> void:
	_next_attack_motion = motion

func _play_one_shot(animation_name: StringName) -> void:
	_return_to_idle = true
	_hide_special_poses()
	animation_player.play(animation_name)

func _play_idle() -> void:
	_return_to_idle = false
	_next_attack_motion = ""
	_hide_special_poses()
	$PoseRoot/Idle.visible = true
	$PoseRoot.modulate = Color.WHITE
	animation_player.play("idle_loop")

func _hide_special_poses() -> void:
	$PoseRoot/Attack.visible = false
	$PoseRoot/Hit.visible = false
	$PoseRoot/ClawAttack.visible = false
	$PoseRoot/PrayerAttack.visible = false
	$PoseRoot/Death.visible = false

func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"claw_attack", &"prayer_attack", &"hit"]:
		_play_idle()
