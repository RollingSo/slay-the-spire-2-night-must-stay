extends SceneTree
func _initialize():
	var root = OS.get_cmdline_user_args()[0]
	var dir = root + "/design/动画预览/revenant/"
	var meta = JSON.parse_string(FileAccess.get_file_as_string(dir+"RV07_HE01_动画制作参数_v01.json"))
	var checks = []
	for label in meta:
		var data = meta[label]
		var image = Image.load_from_file(dir+label+"_透明动画图集_v01.png")
		var atlas = ImageTexture.create_from_image(image)
		var frames = SpriteFrames.new()
		frames.remove_animation("default")
		var name = "idle" if data.loop else "death"
		frames.add_animation(name)
		frames.set_animation_speed(name,24)
		frames.set_animation_loop(name,data.loop)
		frames.set_meta("canvas_origin",Vector2(-256,-472))
		for coords in data.regions:
			var texture = AtlasTexture.new()
			texture.atlas = atlas
			texture.region = Rect2(coords[0],coords[1],coords[2],coords[3])
			texture.filter_clip = true
			frames.add_frame(name,texture)
		var path = dir+label+"_SpriteFrames_v01.res"
		var err = ResourceSaver.save(frames,path,ResourceSaver.FLAG_COMPRESS)
		var reloaded = ResourceLoader.load(path,"SpriteFrames",ResourceLoader.CACHE_MODE_IGNORE)
		checks.append({"action":label,"saved":err==OK,"loadable":reloaded is SpriteFrames,"frame_count":reloaded.get_frame_count(name),"loop":reloaded.get_animation_loop(name),"fps":reloaded.get_animation_speed(name)})
		if err!=OK or not reloaded is SpriteFrames or reloaded.get_frame_count(name)!=data.frame_count:quit(1);return
	FileAccess.open(dir+"RV07_HE01_独立资源检查_v01.json",FileAccess.WRITE).store_string(JSON.stringify(checks,"\t"))
	print(JSON.stringify(checks))
	quit()
