extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var sequence_sprite: AnimatedSprite2D = $SequenceSprite
@onready var legacy_root: Node2D = $PoseRoot
var _state := "idle"
var _dead := false
var _mode := "Normal"
var _clock := 0.0
var _pending_trigger := ""
var _pending_wait := -1.0
var _start_wait := -1.0
const FPS := 24.0
const MARKER_FRAMES := {"attack":11,"dagger":6,"cast":9}

func _ready() -> void:
	animation_player.stop()
	sequence_sprite.stop()
	sequence_sprite.centered = false
	sequence_sprite.offset = sequence_sprite.sprite_frames.get_meta("canvas_origin",Vector2(-89,-332))
	sequence_sprite.scale = Vector2.ONE * (0.9991776666666666 / 0.74319)
	sequence_sprite.position = Vector2(0,4.161864 / 0.74319)
	_start("idle")

func set_playback_mode(mode: String) -> void:
	_mode = mode

func set_action_timing(trigger: String, wait_time: float) -> void:
	_pending_trigger = trigger
	_pending_wait = wait_time

func get_shot_origin() -> Vector2:
	# Canvas-space bow grip in the release pose; convert through every parent scale.
	var point := Vector2(391,183) if _state == "attack" else Vector2(310,285)
	return sequence_sprite.to_global(point-Vector2(260,400))

func play_trigger(trigger: String) -> void:
	if trigger == "Revive":
		_dead = false
		_reset_legacy()
		_start("idle")
		return
	if _dead:
		return
	var wait_time := -1.0
	var timing_trigger := "Attack" if trigger == "AttackDagger" else trigger
	if timing_trigger == _pending_trigger:
		wait_time = _pending_wait
	_pending_trigger = ""
	_pending_wait = -1.0
	match trigger:
		"Attack":
			_start("attack",wait_time if wait_time >= 0 else 0.15)
		"AttackDagger":
			_start("dagger",wait_time if wait_time >= 0 else 0.15)
		"Cast", "PowerUp":
			_start("cast",wait_time if wait_time >= 0 else 0.25)
		"Hit":
			_start("hurt")
		"Dead":
			_dead = true
			_clock = 0
			sequence_sprite.visible = false
			legacy_root.visible = true
			_reset_legacy()
			animation_player.play("death")
		"Idle", "Relaxed", "LowHealth":
			_start("idle")

func _reset_legacy() -> void:
	animation_player.play("RESET")
	animation_player.advance(0)
	animation_player.stop()

func _start(name: String, wait_time := -1.0) -> void:
	animation_player.stop()
	legacy_root.visible = false
	sequence_sprite.visible = true
	_state = name
	_clock = 0
	_start_wait = wait_time
	sequence_sprite.animation = name
	sequence_sprite.frame = 0
	sequence_sprite.frame_progress = 0
	if _mode == "Instant" and name != "idle":
		_state = "idle"
		sequence_sprite.animation = "idle"
		sequence_sprite.frame = 0

func _process(delta: float) -> void:
	if _dead:
		return
	_clock += maxf(delta,0)
	var speed := 2.0 if _mode == "Fast" else 1.0
	var animation_time := _clock * speed
	if _state in MARKER_FRAMES and _start_wait >= 0:
		var marker_time: float = MARKER_FRAMES[_state] / FPS
		var real_wait := minf(_start_wait * 0.5,0.25) if _mode == "Fast" else _start_wait
		if real_wait > 0 and _clock < real_wait:
			animation_time = _clock * marker_time / real_wait
		else:
			animation_time = marker_time + maxf(0,_clock-real_wait) * speed
	var frame_count := sequence_sprite.sprite_frames.get_frame_count(_state)
	var frame_index := floori(animation_time * FPS + 0.00001)
	if frame_index >= frame_count:
		if _state == "idle":
			frame_index = frame_index % frame_count
		else:
			_start("idle")
			return
	sequence_sprite.frame = frame_index
