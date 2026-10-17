extends SceneTree

var failed := false

func _initialize() -> void:
	call_deferred("validate")

func validate() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() >= 1:
		if not ProjectSettings.load_resource_pack(args[0]):
			push_error("Cannot mount Wylder test pack")
			quit(1)
			return
	var manifest_path := args[1] if args.size() >= 2 else "res://design/wylder/asset_report.json"
	var report = JSON.parse_string(FileAccess.get_file_as_string(manifest_path))
	for path in report:
		if args.size() >= 1 and path.begins_with("design/"):
			continue
		var texture = load("res://" + path)
		if not texture is Texture2D:
			push_error("Missing Wylder texture: " + path)
			failed = true
		elif path.begins_with("images/powers/"):
			var image: Image = texture.get_image()
			if image.get_size() != Vector2i(256, 256):
				failed = true
			for point in [Vector2i(0, 0), Vector2i(255, 0), Vector2i(0, 255), Vector2i(255, 255)]:
				if image.get_pixelv(point).a != 0.0:
					push_error("Nontransparent icon corner: " + path)
					failed = true
			var atlas_path: String = "res://images/atlases/power_atlas.sprites/" + path.get_file().get_basename() + ".tres"
			if not load(atlas_path) is AtlasTexture:
				failed = true
	for path in ["res://materials/cards/frames/card_frame_wylder_mat.tres", "res://materials/transitions/wylder_transition_mat.tres", "res://images/atlases/ui_atlas.sprites/card/energy_wylder.tres"]:
		if load(path) == null:
			failed = true
	for path in ["res://wylder_assets/character_icon_wylder.tscn", "res://wylder_assets/combat_rig/wylder_combat_visuals.tscn", "res://wylder_assets/rest_site/wylder_rest_site.tscn", "res://wylder_assets/merchant/wylder_merchant.tscn", "res://wylder_assets/char_select_bg_wylder.tscn", "res://wylder_assets/card_trail_wylder.tscn"]:
		var scene = load(path)
		if not scene is PackedScene:
			failed = true
			continue
		var node = scene.instantiate()
		root.add_child(node)
		if path.contains("combat_visuals"):
			var sprite: Sprite2D = node.get_node("Visuals/Prototype/PoseRoot/Idle")
			var used := sprite.texture.get_image().get_used_rect()
			var height: float = used.size.y * sprite.global_scale.y
			var bottom: float = sprite.to_global(Vector2(0, used.end.y - sprite.texture.get_height() / 2.0)).y
			print("Wylder final height=", height, " baseline=", bottom)
			if abs(height - 299.2) > 1.0 or abs(bottom) > 0.5:
				push_error("Wylder combat calibration failed")
				failed = true
			var rig = node.get_node("Visuals/Prototype")
			for trigger in ["Attack", "Hit", "Cast", "Block", "Dead", "Revive"]:
				rig.play_trigger(trigger)
				rig.animation_player.advance(0.9)
				if trigger == "Dead" and not rig.get_node("PoseRoot/Defeated").visible:
					failed = true
			rig.play_trigger("Revive")
			if not sprite.visible or rig.get_node("PoseRoot/Defeated").visible:
				push_error("Wylder revive pose did not reset")
				failed = true
		node.free()
	print("Wylder resource validation: ", "FAIL" if failed else "PASS")
	quit(1 if failed else 0)
