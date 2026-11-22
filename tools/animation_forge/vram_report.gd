extends SceneTree

func _collect_res(path: String, out: Array) -> void:
	var d := DirAccess.open(path)
	if d == null: return
	d.list_dir_begin()
	while true:
		var n := d.get_next()
		if n == "": break
		if n.begins_with("."): continue
		var p := path.path_join(n)
		if d.current_is_dir():
			_collect_res(p, out)
		elif n.ends_with(".res"):
			out.append(p)

func _initialize() -> void:
	var root := OS.get_cmdline_user_args()[0]
	var files: Array = []
	_collect_res(root, files)
	files.sort()
	var rows: Array = []
	var total_vram := 0
	var total_bytes := 0
	for f in files:
		var sf := ResourceLoader.load(f, "SpriteFrames", ResourceLoader.CACHE_MODE_IGNORE) as SpriteFrames
		if sf == null:
			rows.append({"file":f,"error":"load"}); continue
		var bases := {}
		var anims := sf.get_animation_names()
		for a in anims:
			for i in range(sf.get_frame_count(a)):
				var t := sf.get_frame_texture(a,i)
				if t is AtlasTexture:
					var at := t as AtlasTexture
					if at.atlas: bases[at.atlas.get_instance_id()] = at.atlas.get_size()
				else:
					bases[t.get_instance_id()] = t.get_size()
		var vram := 0
		for s in bases.values():
			vram += int(s.x) * int(s.y) * 4
		var bytes := FileAccess.get_file_as_bytes(f).size()
		total_vram += vram; total_bytes += bytes
		rows.append({"file":f,"bytes":bytes,"pages":bases.size(),"vram_bytes":vram,"vram_mb":round(vram/1048576.0*100.0)/100.0,"anims":anims})
		print("%-70s pages=%2d vram=%7.1fMB file=%6.1fMB" % [f, bases.size(), vram/1048576.0, bytes/1048576.0])
	var result := {"files":rows,"total_vram_bytes":total_vram,"total_vram_mb":round(total_vram/1048576.0*100.0)/100.0,"total_file_bytes":total_bytes,"total_file_mb":round(total_bytes/1048576.0*100.0)/100.0}
	FileAccess.open("res://animations/animation-vram-report.json",FileAccess.WRITE).store_string(JSON.stringify(result,"  "))
	print(JSON.stringify({"total_vram_mb":result.total_vram_mb,"total_file_mb":result.total_file_mb,"files":rows.size()}))
	quit(0)
