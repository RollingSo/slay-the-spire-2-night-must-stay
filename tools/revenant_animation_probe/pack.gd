extends SceneTree
func _initialize():
	var data = JSON.parse_string(FileAccess.get_file_as_string(OS.get_cmdline_user_args()[0]))
	var im = Image.load_from_file(data.atlas_path)
	var atlas = ImageTexture.create_from_image(im)
	var frames = SpriteFrames.new()
	frames.remove_animation("default")
	frames.set_meta("canvas_origin",Vector2(data.canvas_origin[0],data.canvas_origin[1]))
	frames.set_meta("preview_only",true)
	for name in data.actions:
		frames.add_animation(name)
		frames.set_animation_speed(name,24)
		frames.set_animation_loop(name,false)
		for coords in data.actions[name]:
			var texture = AtlasTexture.new()
			texture.atlas = atlas
			texture.region = Rect2(coords[0],coords[1],coords[2],coords[3])
			texture.filter_clip = true
			frames.add_frame(name,texture)
	var result = ResourceSaver.save(frames,data.output_resource,ResourceSaver.FLAG_COMPRESS)
	print("resource save: ",result)
	quit(0 if result == OK else 1)
