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
		if texture is Texture2D and path in ["wylder_assets/char_select_wylder.png", "wylder_assets/char_select_wylder_locked.png"]:
			if texture.get_size() != Vector2(132, 195):
				push_error("Wylder selection portrait must match the other four roles: 132x195")
				failed = true
		if texture is Texture2D and path == "wylder_assets/character_select_wylder_bg.png":
			if texture.get_size() != Vector2(2560, 1200):
				push_error("Wylder selection background must be 2560x1200")
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
		if path.ends_with("char_select_bg_wylder.tscn"):
			if node.anchor_left != 0.5 or node.anchor_right != 0.5 or node.offset_left != -1280.0 or node.offset_right != 1280.0:
				push_error("Wylder selection background is not centered")
				failed = true
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
			rig.set_pursuit_active(true)
			if not sprite.texture.resource_path.ends_with("wylder_pursuit_idle.png"):
				failed = true
			var pursuit_used := sprite.texture.get_image().get_used_rect()
			var pursuit_bottom: float = sprite.to_global(Vector2(0, pursuit_used.end.y - sprite.texture.get_height() / 2.0)).y
			var pursuit_body: float = (pursuit_used.end.y - report["wylder_assets/combat_rig/wylder_pursuit_idle.png"]["body_top_px"]) * sprite.global_scale.y
			print("Wylder pursuit body height=", pursuit_body, " baseline=", pursuit_bottom)
			if abs(pursuit_body - height) > 1.0 or abs(pursuit_bottom) > 0.5:
				failed = true
			rig.play_trigger("Attack")
			rig.animation_player.advance(0.2)
			var attack: Sprite2D = rig.get_node("PoseRoot/Attack")
			if not attack.texture.resource_path.ends_with("wylder_pursuit_attack.png"):
				push_error("Pursuit attack did not use approved two-handed pose")
				failed = true
			rig.set_pursuit_active(false)
			if rig.animation_player.current_animation != "attack" or not rig.get_node("PoseRoot/Attack").visible:
				push_error("Pursuit change interrupted attack")
				failed = true
			if not attack.texture.resource_path.ends_with("wylder_pursuit_attack.png"):
				push_error("Pursuit consumption replaced an ongoing attack pose")
				failed = true
			rig.animation_player.advance(1.0)
			if not sprite.texture.resource_path.ends_with("wylder_combat_character.png"):
				failed = true
			# Measure actual imported alpha bounds at the unanimated ground pose.
			rig.animation_player.play("RESET")
			rig.animation_player.advance(0)
			rig.animation_player.stop()
			var attack_used := attack.texture.get_image().get_used_rect()
			var attack_height: float = attack_used.size.y * attack.global_scale.y
			var attack_bottom: float = attack.to_global(Vector2(0, attack_used.end.y - attack.texture.get_height() / 2.0)).y
			print("Wylder pursuit attack height=", attack_height, " baseline=", attack_bottom)
			if abs(attack_height - height) > 1.0 or abs(attack_bottom) > 0.5:
				push_error("Pursuit attack calibration failed")
				failed = true
			# A normal attack must not inherit the previous pursuit texture;
			# entering pursuit mid-swing affects the next attack/idle only.
			rig.play_trigger("Attack")
			rig.animation_player.advance(0.2)
			if not attack.texture.resource_path.ends_with("wylder_attack.png"):
				push_error("Normal attack retained a stale pursuit pose")
				failed = true
			rig.set_pursuit_active(true)
			if not attack.texture.resource_path.ends_with("wylder_attack.png") or not attack.visible:
				push_error("Entering pursuit replaced an ongoing normal attack")
				failed = true
			rig.animation_player.advance(1.0)
			if not sprite.texture.resource_path.ends_with("wylder_pursuit_idle.png"):
				push_error("Attack did not return to current pursuit idle")
				failed = true
			for trigger in ["Attack", "Hit", "Cast", "Block", "Dead", "Revive"]:
				rig.play_trigger(trigger)
				rig.animation_player.advance(0.9)
				if trigger == "Dead" and not rig.get_node("PoseRoot/Defeated").visible:
					failed = true
				if trigger == "Dead":
					rig.set_pursuit_active(true)
					rig.play_trigger("Idle")
					if not rig.get_node("PoseRoot/Defeated").visible or sprite.visible:
						push_error("Pursuit/idle change resurrected defeated pose")
						failed = true
			rig.play_trigger("Revive")
			if not sprite.visible or rig.get_node("PoseRoot/Defeated").visible:
				push_error("Wylder revive pose did not reset")
				failed = true
			rig.set_pursuit_active(false)
			if not sprite.texture.resource_path.ends_with("wylder_combat_character.png"):
				failed = true
		node.free()
	print("Wylder resource validation: ", "FAIL" if failed else "PASS")
	quit(1 if failed else 0)
