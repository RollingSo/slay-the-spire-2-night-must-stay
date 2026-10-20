extends SceneTree
func _initialize():
	var a = OS.get_cmdline_user_args()
	var data = JSON.parse_string(FileAccess.get_file_as_string(a[0]))
	var cw = int(data.cell[0]); var ch = int(data.cell[1])
	var texs = []
	for p in data.pages:
		texs.append(ImageTexture.create_from_image(Image.load_from_file(p)))
	var sf = SpriteFrames.new()
	sf.remove_animation("default")
	sf.set_meta("canvas", Vector2i(cw, ch))
	sf.set_meta("anchor", Vector2(float(data.anchor[0]), float(data.anchor[1])))
	sf.set_meta("canvas_scale", float(data.canvas_scale))
	for nm in data.animations:
		var ad = data.animations[nm]
		sf.add_animation(nm)
		sf.set_animation_speed(nm, float(ad.speed))
		sf.set_animation_loop(nm, bool(ad.loop))
		for f in ad.frames:
			var t = AtlasTexture.new()
			t.atlas = texs[int(f[0])]
			t.region = Rect2(f[1], f[2], cw, ch)
			t.filter_clip = true
			sf.add_frame(nm, t)
	var err = ResourceSaver.save(sf, data.output, ResourceSaver.FLAG_COMPRESS)
	print("save_result=", err)
	quit(0 if err == OK else 1)
