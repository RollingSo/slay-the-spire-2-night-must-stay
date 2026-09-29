extends SceneTree

var failed := false

func _initialize() -> void:
	call_deferred("validate")

func validate() -> void:
	var report = JSON.parse_string(FileAccess.get_file_as_string("res://design/duchess/asset_report.json"))
	var args = OS.get_cmdline_user_args()
	if args.size() == 2 and args[0] == "--pack":
		if not ProjectSettings.load_resource_pack(args[1]):
			push_error("Failed to mount exported Duchess test pack")
			quit(1)
			return
	for path in report:
		var texture = load("res://" + path)
		if not texture is Texture2D:
			push_error("Duchess texture failed to import: " + path)
			failed = true
		if path.begins_with("images/powers/duchess_") or path.begins_with("duchess_assets/potions/"):
			var category = "power" if path.begins_with("images/powers/") else "potion"
			var atlas_path = "res://images/atlases/" + category + "_atlas.sprites/" + path.get_file().get_basename() + ".tres"
			if not load(atlas_path) is AtlasTexture:
				push_error("Missing packaged atlas mapping: " + atlas_path)
				failed = true
	for path in ["res://images/atlases/ui_atlas.sprites/card/energy_duchess.tres", "res://materials/cards/frames/card_frame_duchess_mat.tres", "res://materials/transitions/duchess_transition_mat.tres", "res://duchess_assets/character_icon_duchess.tscn"]:
		if load(path) == null:
			failed = true
	for directory in ["res://images/atlases/power_atlas.sprites", "res://images/atlases/potion_atlas.sprites"]:
		for name in DirAccess.get_files_at(directory):
			if name.begins_with("duchess_") and name.ends_with(".tres"):
				if not load(directory + "/" + name) is AtlasTexture:
					failed = true
	for path in ["res://duchess_assets/combat_rig/duchess_combat_visuals.tscn", "res://duchess_assets/rest_site/duchess_rest_site.tscn", "res://duchess_assets/merchant/duchess_merchant.tscn", "res://duchess_assets/char_select_bg_duchess.tscn", "res://duchess_assets/card_trail_duchess.tscn"]:
		var scene = load(path)
		if not scene is PackedScene:
			failed = true
			continue
		var node = scene.instantiate()
		root.add_child(node)
		if path.contains("combat_visuals"):
			var sprite: Sprite2D = node.get_node("Visuals/Prototype/PoseRoot/Idle")
			var used: Rect2i = sprite.texture.get_image().get_used_rect()
			var height: float = used.size.y * sprite.global_scale.y
			var bottom: float = sprite.to_global(Vector2(0, used.end.y - sprite.texture.get_height() / 2.0)).y
			print("Duchess imported bounds=", used, " final_height=", height, " baseline=", bottom)
			if abs(height - 308.331) > 0.308331 or abs(bottom) > 0.35:
				push_error("Duchess combat calibration failed")
				failed = true
			var rig = node.get_node("Visuals/Prototype")
			for trigger in ["Attack", "Hit", "Cast", "Dead", "Revive"]:
				rig.play_trigger(trigger)
				rig.animation_player.advance(0.9)
			rig.play_trigger("Revive")
			if rig.get_node("PoseRoot").modulate.a != 1.0:
				push_error("Duchess revive left a transparent rig")
				failed = true
		node.free()
	print("Duchess imported resource validation: ", "FAIL" if failed else "PASS")
	quit(1 if failed else 0)
