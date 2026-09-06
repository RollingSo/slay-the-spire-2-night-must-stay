extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], false):
		push_error("Pass the installed SlayTheSpire2.pck path after --")
		quit(1)
		return
	var found := 0
	for file in DirAccess.get_files_at("res://debug_audio"):
		if "heavy_attack" in file or "slash" in file or "sword" in file or "wind" in file:
			print("NATIVE_AUDIO: ", file)
			if file == "heavy_attack.mp3" or file == "slash_attack.mp3":
				var audio := load("res://debug_audio/" + file) as AudioStream
				if audio != null:
					print("SAMPLE_SECONDS: ", file, " ", audio.get_length())
					found += 1
	quit(0 if found == 2 else 1)
