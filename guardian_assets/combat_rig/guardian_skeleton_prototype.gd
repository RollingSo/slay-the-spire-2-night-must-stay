extends Node2D

@export var auto_demo := true
@onready var animation_player: AnimationPlayer = $AnimationPlayer
var _return_to_idle := false

func _enter_tree() -> void:
	# Godot's terminal Bone2D nodes default to autocalculation and otherwise
	# warn when they have no child bone. Disable it before children enter tree.
	for bone in find_children("*", "Bone2D", true, false):
		bone.set_autocalculate_length_and_angle(false)

func _ready() -> void:
	animation_player.animation_finished.connect(_on_animation_finished)
	var capture_name := OS.get_environment("GUARDIAN_RIG_CAPTURE")
	var arguments := OS.get_cmdline_args() + OS.get_cmdline_user_args()
	for argument in arguments:
		if argument.begins_with("capture="):
			capture_name = argument.trim_prefix("capture=")
	print("Guardian Skeleton2D prototype ready; capture=", capture_name)

	if capture_name != "":
		await _capture_pose(capture_name)
	elif auto_demo:
		_run_demo_loop()


func play_trigger(trigger: String) -> void:
	match trigger:
		"Attack":
			_play_one_shot("attack")
		"GuardCounter":
			_play_one_shot("counter_attack")
		"Cast", "Hit":
			_play_one_shot("guard")
		"Dead":
			_return_to_idle = false
			animation_player.speed_scale = 0.7
			animation_player.play("guard")
		"Idle", "Relaxed", "Revive":
			_play_idle()


func _play_one_shot(animation_name: StringName) -> void:
	_return_to_idle = true
	animation_player.speed_scale = 1.0
	animation_player.play(animation_name)


func _play_idle() -> void:
	_return_to_idle = false
	animation_player.speed_scale = 1.0
	animation_player.play("idle_loop")


func _on_animation_finished(animation_name: StringName) -> void:
	if _return_to_idle and animation_name in [&"attack", &"counter_attack", &"guard"]:
		_play_idle()


func _run_demo_loop() -> void:
	while is_inside_tree():
		animation_player.play("idle_loop")
		await get_tree().create_timer(2.0).timeout
		animation_player.play("guard")
		await animation_player.animation_finished
		await get_tree().create_timer(0.35).timeout
		animation_player.play_backwards("guard")
		await animation_player.animation_finished
		animation_player.play("attack")
		await animation_player.animation_finished
		animation_player.play("counter_attack")
		await animation_player.animation_finished
		await get_tree().create_timer(0.4).timeout


func _capture_pose(animation_name: String) -> void:
	var sample_times := {
		"idle_loop": 0.75,
		"guard": 0.5,
		"attack": 0.45,
		"counter_attack": 0.48,
	}
	if not animation_player.has_animation(animation_name):
		push_error("Unknown Guardian rig animation: %s" % animation_name)
		get_tree().quit(2)
		return

	animation_player.play(animation_name)
	animation_player.seek(sample_times.get(animation_name, 0.0), true)
	await get_tree().process_frame
	await get_tree().process_frame
	# `frame_post_draw` is not emitted by Godot's dummy headless renderer.
	# Two process frames are enough for deterministic pose validation; a normal
	# rendering driver will additionally produce the visual capture below.
	if DisplayServer.get_name() == "headless":
		print("Guardian rig pose validated without a render target: ", animation_name)
		get_tree().quit()
		return

	var output_dir := "res://design/骨骼动画预览"
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(output_dir))
	var image := get_viewport().get_texture().get_image()
	var error := image.save_png("%s/guardian_rig_%s.png" % [output_dir, animation_name])
	if error != OK:
		push_error("Failed to save Guardian rig capture: %s" % error_string(error))
		get_tree().quit(3)
		return
	get_tree().quit()
