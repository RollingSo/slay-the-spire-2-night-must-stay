extends SceneTree

func _initialize() -> void:
	var args = OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], true):
		push_error("Cannot mount exported mod PCK")
		quit(1)
		return
	var themes = {"guardian": "6F4A2F", "ironeye": "68734A", "revenant": "5C7689", "duchess": "48B9ED"}
	var failed = false
	for character in themes:
		var material = load("res://materials/cards/frames/card_frame_%s_mat.tres" % character) as ShaderMaterial
		if material == null:
			failed = true
			continue
		var actual: Color = material.get_shader_parameter("frame_mid")
		var valid = actual.is_equal_approx(Color(themes[character]))
		print("CHARACTER_THEME ", character, " frame_mid=", actual.to_html(false), " valid=", valid)
		failed = failed or not valid
	for gesture in ["point", "rock", "paper", "scissors"]:
		var path = "res://duchess_assets/multiplayer_hands/multiplayer_hand_duchess_%s.png" % gesture
		var texture = load(path) as Texture2D
		var valid = texture != null
		if valid:
			var image = texture.get_image()
			valid = image != null and not image.is_empty() and image.get_used_rect().has_area()
			if gesture == "point":
				valid = valid and image.get_size() == Vector2i(422, 1200)
				for corner in [Vector2i.ZERO, Vector2i(image.get_width()-1,0), Vector2i(0,image.get_height()-1), image.get_size()-Vector2i.ONE]:
					valid = valid and image.get_pixelv(corner).a == 0.0
		print("DUCHESS_MULTIPLAYER_HAND ", gesture, " valid=", valid)
		failed = failed or not valid
	quit(1 if failed else 0)
