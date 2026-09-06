extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 2 or not ProjectSettings.load_resource_pack(args[0], false):
		quit(1)
		return
	DirAccess.make_dir_recursive_absolute(args[1])
	for file in ["ironclad/defend_ironclad.png", "necrobinder/strike_necrobinder.png"]:
		var texture := load("res://images/packed/card_portraits/" + file) as Texture2D
		if texture != null:
			var result := texture.get_image().save_png(args[1].path_join(file.get_file()))
			print(file, ": ", result)
	quit()
