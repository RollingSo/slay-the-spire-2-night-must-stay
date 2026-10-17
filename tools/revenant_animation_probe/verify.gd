extends SceneTree
func _initialize():
	var args = OS.get_cmdline_user_args()
	var data = JSON.parse_string(FileAccess.get_file_as_string(args[0]))
	var frames = ResourceLoader.load(data.output_resource,"SpriteFrames",ResourceLoader.CACHE_MODE_IGNORE)
	var checks = []
	checks.append({"name":"sprite_frames_loaded","passed":frames is SpriteFrames})
	if not frames is SpriteFrames: quit(1); return
	checks.append({"name":"two_actions","passed":frames.get_animation_names().size()==2})
	for name in data.actions:
		checks.append({"name":name+"_frame_count","passed":frames.get_frame_count(name)==data.actions[name].size()})
		checks.append({"name":name+"_fps_and_oneshot","passed":frames.get_animation_speed(name)==24 and not frames.get_animation_loop(name)})
		var first = frames.get_frame_texture(name,0).get_image()
		var last = frames.get_frame_texture(name,frames.get_frame_count(name)-1).get_image()
		checks.append({"name":name+"_endpoints","passed":first.get_data()==last.get_data()})
		var corner_clear = true
		for n in range(frames.get_frame_count(name)):
			var im = frames.get_frame_texture(name,n).get_image()
			for xy in [Vector2i(0,0),Vector2i(im.get_width()-1,0),Vector2i(0,im.get_height()-1),Vector2i(im.get_width()-1,im.get_height()-1)]:
				corner_clear = corner_clear and im.get_pixelv(xy).a==0
		checks.append({"name":name+"_alpha_corners","passed":corner_clear})
		var sprite = AnimatedSprite2D.new()
		sprite.sprite_frames = frames
		sprite.play(name)
		checks.append({"name":name+"_playback_start","passed":sprite.animation==name and sprite.frame==0})
		sprite.free()
	var failed = checks.filter(func(c):return not c.passed)
	FileAccess.open(args[1],FileAccess.WRITE).store_string(JSON.stringify({"checks":checks,"passed":checks.size()-failed.size(),"failed":failed.size()},"\t"))
	for c in checks:print("PASS " if c.passed else "FAIL ",c.name)
	quit(0 if failed.is_empty() else 1)
