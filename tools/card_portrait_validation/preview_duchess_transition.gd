extends SceneTree

func _initialize() -> void:
	call_deferred("render_preview")

func render_preview() -> void:
	var args = OS.get_cmdline_user_args()
	var shader = Shader.new()
	shader.code = FileAccess.get_file_as_string(args[0])
	var material = ShaderMaterial.new()
	material.shader = shader
	material.set_shader_parameter("revealing", true)
	var viewport = SubViewport.new()
	viewport.size = Vector2i(960, 540)
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	var overlay = ColorRect.new()
	overlay.size = Vector2(960, 540)
	overlay.material = material
	viewport.add_child(overlay)
	DirAccess.make_dir_recursive_absolute(args[1])
	for index in range(9):
		material.set_shader_parameter("threshold", 1.0-float(index)/8.0)
		await process_frame
		await RenderingServer.frame_post_draw
		var rendered = viewport.get_texture().get_image()
		for y in range(rendered.get_height()):
			for x in range(rendered.get_width()):
				var pixel = rendered.get_pixel(x,y)
				if abs(pixel.r-pixel.g) > 0.005 or abs(pixel.g-pixel.b) > 0.005:
					push_error("Clock transition must remain monochrome")
					quit(1)
					return
		rendered.save_png(args[1]+"/reveal_%02d.png" % index)
		if index == 0 and rendered.get_pixel(0,0).a < 0.999:
			push_error("Start must be fully covered")
			quit(1)
			return
		if index == 8 and rendered.get_used_rect().has_area():
			push_error("End must be fully transparent")
			quit(1)
			return
	print("PASS: nine monochrome reveal frames; opaque start and transparent end")
	quit()
