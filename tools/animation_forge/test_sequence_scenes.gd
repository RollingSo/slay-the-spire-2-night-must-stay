extends SceneTree
## Smoke test for the shared sequence prototype; no export or game DLL required.
func _initialize() -> void:
	var cases := [
		["res://guardian_assets/combat_rig/guardian_combat_visuals.tscn","guardian",308.331],
		["res://duchess_assets/combat_rig/duchess_combat_visuals.tscn","duchess",307.546886],
		["res://revenant_assets/combat/revenant_combat_visuals.tscn","revenant",215.7527232],
	]
	for c in cases:
		var n := (load(c[0]) as PackedScene).instantiate()
		get_root().add_child(n)
		await process_frame
		var rig := n.get_node("Visuals/Prototype")
		var spr := rig.get_node("Sprite") as AnimatedSprite2D
		var used := spr.sprite_frames.get_frame_texture(spr.animation,0).get_image().get_used_rect().size
		var parent_scale := (n.get_node("Visuals") as Node2D).global_scale.y
		var final_h := used.y * spr.scale.y * parent_scale
		print("%s final=%.4f target=%.4f" % [c[1],final_h,c[2]])
		if absf(final_h-float(c[2])) > 0.5:
			push_error("height mismatch"); quit(1); return
		for trig in ["Attack","Hit","Dead"]:
			rig.call("play_trigger",trig)
			await process_frame
		n.queue_free()
	print("sequence smoke passed")
	quit(0)
