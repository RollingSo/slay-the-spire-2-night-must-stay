extends SceneTree
func _initialize():
	var dir = OS.get_cmdline_user_args()[0]+"/design/动画预览/revenant/"
	var result = []
	for spec in [["HE01_待机","idle",96,true],["RV07_死亡","death",78,false]]:
		var frames = ResourceLoader.load(dir+spec[0]+"_SpriteFrames_v01.res","SpriteFrames",ResourceLoader.CACHE_MODE_IGNORE)
		if not frames is SpriteFrames:quit(1);return
		var passed = frames.get_frame_count(spec[1])==spec[2] and frames.get_animation_loop(spec[1])==spec[3] and frames.get_animation_speed(spec[1])==24
		for i in range(spec[2]):
			var image = frames.get_frame_texture(spec[1],i).get_image()
			passed = passed and image.get_width()==512 and image.get_height()==512
			for xy in [Vector2i(0,0),Vector2i(511,0),Vector2i(0,511),Vector2i(511,511)]:passed = passed and image.get_pixelv(xy).a==0
		var sprite = AnimatedSprite2D.new()
		sprite.sprite_frames = frames
		sprite.play(spec[1])
		passed = passed and sprite.animation==spec[1] and sprite.frame==0
		sprite.free()
		result.append({"action":spec[0],"passed":passed,"frames":spec[2],"separate_process":true,"embedded_atlas":true})
		if not passed:quit(1);return
	FileAccess.open(dir+"RV07_HE01_独立资源复载检查_v01.json",FileAccess.WRITE).store_string(JSON.stringify(result,"\t"))
	print(JSON.stringify(result))
	quit()
