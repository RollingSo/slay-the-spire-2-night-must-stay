extends SceneTree

func _initialize() -> void:
	call_deferred("check_assets")

func check_assets() -> void:
	var args = OS.get_cmdline_user_args()
	if not ProjectSettings.load_resource_pack(args[0], true):
		quit(1)
		return
	var scene = load("res://duchess_assets/combat_rig/duchess_combat_rig.tscn") as PackedScene
	if scene == null:
		quit(1)
		return
	var rig = scene.instantiate()
	root.add_child(rig)
	await process_frame
	rig.call("play_trigger", "Block")
	await process_frame
	var block = rig.get_node("PoseRoot/Block") as Sprite2D
	var idle = rig.get_node("PoseRoot/Idle") as Sprite2D
	var valid = block.visible and not idle.visible and block.texture != null
	var block_bounds = block.texture.get_image().get_used_rect()
	var idle_bounds = idle.texture.get_image().get_used_rect()
	valid = valid and block_bounds.size.y == idle_bounds.size.y and block_bounds.end.y == idle_bounds.end.y
	rig.call("set_phase_visual", 0.32, 0.7)
	valid = valid and is_equal_approx(block.self_modulate.a, 0.32)
	rig.call("play_trigger", "Hit")
	valid = valid and not block.visible
	rig.call("set_phase_visual", 1.0, 0.0)
	valid = valid and is_equal_approx(block.self_modulate.a, 1.0)
	print("DUCHESS_BLOCK_AND_PHASE valid=", valid, " height=", block_bounds.size.y)
	quit(0 if valid else 1)
