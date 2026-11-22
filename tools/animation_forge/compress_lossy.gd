extends SceneTree

func _compress_texture(tex: Texture2D, cache: Dictionary) -> Texture2D:
	if tex is AtlasTexture:
		var atlas_tex := tex as AtlasTexture
		var base := atlas_tex.atlas
		if base == null:
			return tex
		var key := base.get_instance_id()
		if not cache.has(key):
			var img := base.get_image()
			if img == null or img.is_empty():
				return tex
			var pct := PortableCompressedTexture2D.new()
			pct.keep_compressed_buffer = true
			pct.create_from_image(img, PortableCompressedTexture2D.COMPRESSION_MODE_LOSSY, false, 0.85)
			cache[key] = pct
		var out := AtlasTexture.new()
		out.atlas = cache[key]
		out.region = atlas_tex.region
		out.margin = atlas_tex.margin
		out.filter_clip = atlas_tex.filter_clip
		return out
	else:
		var key2 := tex.get_instance_id()
		if cache.has(key2):
			return cache[key2]
		var img2 := tex.get_image()
		if img2 == null or img2.is_empty():
			return tex
		var pct2 := PortableCompressedTexture2D.new()
		pct2.keep_compressed_buffer = true
		pct2.create_from_image(img2, PortableCompressedTexture2D.COMPRESSION_MODE_LOSSY, false, 0.85)
		cache[key2] = pct2
		return pct2

func _initialize() -> void:
	print("compress start")
	var args := OS.get_cmdline_user_args()
	if args.size() < 2:
		push_error("usage: input.res output.res")
		quit(2); return
	var input: String = args[0]
	var output: String = args[1]
	var sf := ResourceLoader.load(input, "SpriteFrames", ResourceLoader.CACHE_MODE_IGNORE) as SpriteFrames
	if sf == null:
		push_error("failed to load " + input); quit(1); return
	var cache := {}
	var animations := sf.get_animation_names()
	for anim in animations:
		var n := sf.get_frame_count(anim)
		for i in range(n):
			if i == 0: print("anim ", anim, " frames ", n)
			var tex := sf.get_frame_texture(anim, i)
			var compressed := _compress_texture(tex, cache)
			if compressed != tex:
				var dur := sf.get_frame_duration(anim, i)
				sf.set_frame(anim, i, compressed, dur)
			if i % 10 == 0: print("frame ", i)
	print("saving")
	var err := ResourceSaver.save(sf, output, ResourceSaver.FLAG_COMPRESS)
	if err != OK:
		push_error("save failed %s" % err); quit(1); return
	var check := ResourceLoader.load(output, "SpriteFrames", ResourceLoader.CACHE_MODE_IGNORE) as SpriteFrames
	if check == null:
		push_error("reload failed"); quit(1); return
	var ok := check.get_animation_names() == animations
	for anim in animations:
		if check.get_frame_count(anim) != sf.get_frame_count(anim): ok = false
		for i in range(sf.get_frame_count(anim)):
			if absf(check.get_frame_duration(anim,i) - sf.get_frame_duration(anim,i)) > 0.0001: ok = false
			if check.get_frame_texture(anim,i) == null: ok = false
	if check.get_meta("canvas", null) != sf.get_meta("canvas", null): ok = false
	if check.get_meta("anchor", null) != sf.get_meta("anchor", null): ok = false
	if not ok:
		push_error("compressed resource validation failed"); quit(1); return
	print(JSON.stringify({"input":input,"output":output,"anims":animations,"frames":sf.get_frame_count(animations[0]),"textures":cache.size(),"ok":true}))
	quit(0)
