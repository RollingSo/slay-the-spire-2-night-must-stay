extends SceneTree
## 复载并测量每个动画的并集包围盒，供 forge.py 做闸门判断。
## 用法: godot --headless --path tools/revenant_animation_probe \
##         --script tools/animation_forge/verify.gd -- <res 目录> <输出 json>

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	var dir: String = args[0]
	var out_path: String = args[1]
	var result := []
	for file in DirAccess.get_files_at(dir):
		if not file.ends_with(".res"):
			continue
		var sf := ResourceLoader.load(dir.path_join(file), "SpriteFrames", ResourceLoader.CACHE_MODE_IGNORE) as SpriteFrames
		var entry := {"file": file, "loadable": sf != null}
		if sf != null:
			var canvas := sf.get_meta("canvas", Vector2i(0, 0)) as Vector2i
			var anchor := sf.get_meta("anchor", Vector2(0, 0)) as Vector2
			entry["canvas"] = [canvas.x, canvas.y]
			entry["anchor"] = [anchor.x, anchor.y]
			var anims := {}
			for nm in sf.get_animation_names():
				var count := sf.get_frame_count(nm)
				var lo := Vector2i(1 << 20, 1 << 20)
				var hi := Vector2i(-1, -1)
				var neutral := 0
				var neutral_bottom := 0
				for i in range(count):
					var rect := sf.get_frame_texture(nm, i).get_image().get_used_rect()
					lo.x = mini(lo.x, rect.position.x)
					lo.y = mini(lo.y, rect.position.y)
					hi.x = maxi(hi.x, rect.end.x - 1)
					hi.y = maxi(hi.y, rect.end.y - 1)
					if i == 0:
						neutral = rect.size.y
						neutral_bottom = rect.end.y - 1
				anims[nm] = {
					"frames": count,
					"fps": sf.get_animation_speed(nm),
					"loop": sf.get_animation_loop(nm),
					"union": [lo.x, lo.y, hi.x, hi.y],
					"neutral_height": neutral,
					"neutral_bottom": neutral_bottom,
				}
			entry["animations"] = anims
		result.append(entry)
	FileAccess.open(out_path, FileAccess.WRITE).store_string(JSON.stringify(result, "  "))
	print("verified ", result.size())
	quit(0)
