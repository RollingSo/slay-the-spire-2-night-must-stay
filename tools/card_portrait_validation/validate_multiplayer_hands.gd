extends SceneTree

# Run against the source project, or pass a mod PCK after -- to validate export.
func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if not args.is_empty() and not ProjectSettings.load_resource_pack(args[0]):
		quit(1)
		return
	var failed := false
	for role in ["guardian", "ironeye", "duchess", "revenant"]:
		for gesture in ["point", "rock", "paper", "scissors"]:
			var filename := "revenant_%s.png" % gesture if role == "revenant" else "multiplayer_hand_%s_%s.png" % [role, gesture]
			var path := "res://%s_assets/multiplayer_hands/%s" % [role, filename]
			var texture := load(path) as Texture2D
			var valid := texture != null
			if valid:
				var image := texture.get_image()
				if image.is_compressed():
					image.decompress()
				valid = image.get_size() == Vector2i(422, 1200) and image.get_used_rect().has_area()
				for corner in [Vector2i.ZERO, Vector2i(421, 0), Vector2i(0, 1199), Vector2i(421, 1199)]:
					valid = valid and image.get_pixelv(corner).a == 0.0
			print("MULTIPLAYER_HAND ", role, " ", gesture, " valid=", valid)
			failed = failed or not valid
	quit(1 if failed else 0)
