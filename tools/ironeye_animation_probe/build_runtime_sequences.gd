extends SceneTree
func _initialize():
	var args = OS.get_cmdline_user_args()
	var data = JSON.parse_string(FileAccess.get_file_as_string(args[0]))
	var frames = SpriteFrames.new()
	frames.remove_animation("default")
	frames.set_meta("canvas_origin",Vector2(data.sprite_offset[0],data.sprite_offset[1]))
	for name in data.inputs:
		frames.add_animation(name)
		frames.set_animation_speed(name,24)
		frames.set_animation_loop(name,name == "idle")
		for path in data.inputs[name]:
			var image = Image.load_from_file(path)
			if image == null: quit(1); return
			frames.add_frame(name,ImageTexture.create_from_image(image))
	var error = ResourceSaver.save(frames,data.output_resource,ResourceSaver.FLAG_COMPRESS)
	print("ResourceSaver result ",error)
	quit(0 if error == OK else 2)
