extends SceneTree

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], false):
		quit(1)
		return
	for file in DirAccess.get_files_at("res://debug_audio"):
		if file.ends_with(".mp3") or file.ends_with(".wav") or file.ends_with(".ogg"):
			var audio := load("res://debug_audio/" + file) as AudioStream
			if audio != null:
				print("NATIVE_AUDIO: ", file, " SECONDS: ", audio.get_length())
	quit()
