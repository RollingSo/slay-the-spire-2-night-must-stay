extends SceneTree
## Read the installed PCK; strip only the scene controller so the original
## particle resources can run in the isolated renderer without the game runtime.
## Original resources are written only into ignored .tmp, never into the mod.

var effect: Node2D
var frame := 0
var samples := [6, 12, 21, 33, 48, 72, 96]
const OUTPUT := "res://../../.tmp/vfx-scale/"

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], false):
		push_error("Pass the installed SlayTheSpire2.pck after --")
		quit(1)
		return
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUTPUT))
	root.size = Vector2i(1200, 900)
	root.transparent_bg = true
	var scene := load("res://scenes/vfx/vfx_poison_impact.tscn") as PackedScene
	ResourceSaver.save(scene, OUTPUT + "poison_source.tscn")
	var text := FileAccess.get_file_as_string(OUTPUT + "poison_source.tscn")
	var lines := PackedStringArray()
	for line in text.split("\n"):
		if line.begins_with('[ext_resource type="Script"') or line.begins_with("script = "):
			continue
		if line.begins_with("_impactParticles =") or line.begins_with("_horizontalSmokeContainer ="):
			continue
		if line.begins_with('[node name="vfx_poison_impact"'):
			line = '[node name="vfx_poison_impact" type="Node2D"]'
		lines.append(line)
	var file := FileAccess.open(OUTPUT + "poison_visual.tscn", FileAccess.WRITE)
	file.store_string("\n".join(lines))
	file.close()
	effect = load(OUTPUT + "poison_visual.tscn").instantiate()
	effect.position = Vector2(600, 450)
	root.add_child.call_deferred(effect)
	start.call_deferred()

func start() -> void:
	for node in effect.find_children("*", "GPUParticles2D", true, false):
		node.seed = 12345
		node.use_fixed_seed = true
		node.restart()

func _process(_delta: float) -> bool:
	frame += 1
	if frame in samples:
		capture.call_deferred(frame)
	if frame == 110:
		quit()
	return false

func capture(number: int) -> void:
	await RenderingServer.frame_post_draw
	var img := root.get_texture().get_image()
	var lo := Vector2i(1200, 900)
	var hi := Vector2i.ZERO
	var count := 0
	for y in range(img.get_height()):
		for x in range(img.get_width()):
			if img.get_pixel(x, y).a > 0.10:
				lo = lo.min(Vector2i(x, y))
				hi = hi.max(Vector2i(x, y))
				count += 1
	print("POISON_SAMPLE ", number, " bounds=", hi-lo+Vector2i.ONE if count else Vector2i.ZERO, " filled=", count)
	img.save_png(OUTPUT + "poison_%03d.png" % number)
