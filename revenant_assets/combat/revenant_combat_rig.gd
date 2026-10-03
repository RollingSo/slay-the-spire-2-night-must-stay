extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false
var _attack_motion := ""
var _awaiting_native_attack := false
var _dead := false
var _card_motion_active := false
var _base_position: Vector2
var _base_rotation: float

func _ready() -> void:
	_base_position = $PoseRoot.position
	_base_rotation = $PoseRoot.rotation
	animation_player.animation_finished.connect(_on_animation_finished)
	_play_idle()

func play_trigger(trigger: String) -> void:
	if _dead and trigger != "Revive":
		return
	match trigger:
		"Attack", "Cast":
			# Card routing already started this action; acknowledge the native trigger
			# without restarting it or hiding the currently displayed pose.
			if _awaiting_native_attack:
				_awaiting_native_attack = false
				return
			_play_one_shot(_attack_animation(_attack_motion))
		"Call", "Resonance":
			_play_one_shot("attack")
		"Prayer":
			_play_one_shot("prayer_attack")
		"Hit":
			_play_one_shot("hit")
		"Dead":
			_dead = true
			_return_to_idle = false
			_attack_motion = ""
			_card_motion_active = false
			_awaiting_native_attack = false
			_start_animation("death")
		"Revive":
			_dead = false
			clear_card_motion()
			_play_idle()
		"Idle", "Relaxed":
			_play_idle()

func queue_attack_motion(motion: String) -> void:
	_attack_motion = motion

func play_card_motion(motion: String) -> void:
	if _dead:
		return
	_attack_motion = motion
	_card_motion_active = true
	_awaiting_native_attack = true
	_play_one_shot(_attack_animation(motion))

func clear_card_motion() -> void:
	_attack_motion = ""
	_card_motion_active = false
	_awaiting_native_attack = false

func _attack_animation(motion: String) -> StringName:
	if motion == "Prayer":
		return &"prayer_attack"
	if motion == "Call" or motion == "Resonance":
		return &"attack"
	return &"claw_attack"

func _play_one_shot(animation_name: StringName) -> void:
	_return_to_idle = true
	_start_animation(animation_name)

func _start_animation(animation_name: StringName) -> void:
	# Stop resets playback even when the same animation is triggered twice.
	# Reset all shared properties before synchronously applying time-zero keys.
	animation_player.stop()
	$PoseRoot.position = _base_position
	$PoseRoot.rotation = _base_rotation
	$PoseRoot.modulate = Color.WHITE
	_hide_special_poses()
	$PoseRoot/Idle.visible = true
	animation_player.play(animation_name)
	animation_player.advance(0)

func _play_idle() -> void:
	_return_to_idle = false
	if not _card_motion_active:
		_attack_motion = ""
	_awaiting_native_attack = false
	_start_animation("idle_loop")

func _hide_special_poses() -> void:
	$PoseRoot/Attack.visible = false
	$PoseRoot/Hit.visible = false
	$PoseRoot/ClawAttack.visible = false
	$PoseRoot/PrayerAttack.visible = false
	$PoseRoot/Death.visible = false

func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"claw_attack", &"prayer_attack", &"hit"]:
		_play_idle()
