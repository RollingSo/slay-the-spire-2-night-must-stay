extends Node
## Frame animation driven by SpriteFrames resources.
##
## Each clip carries its own canvas size and anchor in the resource meta, so the
## owning Sprite2D receives an offset that puts that clip's ground-contact point
## on the node origin. Actions may therefore extend in any direction without
## clipping (see design/动画预览/revenant/基准_v03/使用说明.md).

signal finished

const CLIP_TRIGGERS := {
	"Idle": "idle",
	"Attack": "attack_a",
	"AttackA": "attack_a",
	"AttackB": "attack_b",
	"Hit": "hurt",
}
const CLIP_FILES := ["idle", "attack_a", "attack_b", "hurt"]
const FALLBACK := {"attack_b": "attack_a", "hurt": "idle"}

var sprite: Sprite2D
var clips: Dictionary = {}
var clip := "idle"
var elapsed := 0.0
var _index := -1


func configure(target: Sprite2D, directory: String) -> void:
	sprite = target
	clips.clear()
	for key in CLIP_FILES:
		var path: String = directory.path_join(key + ".res")
		if not ResourceLoader.exists(path):
			continue
		var frames := load(path) as SpriteFrames
		if frames == null:
			push_warning("Family animation is not SpriteFrames: " + path)
			continue
		clips[key] = {
			"frames": frames,
			"canvas": frames.get_meta("canvas", Vector2i(512, 512)),
			"anchor": frames.get_meta("anchor", Vector2(256, 480)),
		}
	if clips.is_empty():
		push_warning("No family animation resources under " + directory)
		return
	play_trigger("Idle")


func play_trigger(trigger: String) -> void:
	if clips.is_empty():
		return
	var key: String = CLIP_TRIGGERS.get(trigger, "idle")
	while not clips.has(key) and FALLBACK.has(key):
		key = FALLBACK[key]
	if not clips.has(key):
		key = "idle"
	clip = key
	elapsed = 0.0
	_index = -1
	_apply_clip_offset()
	_sample()


func has_clip(key: String) -> bool:
	return clips.has(key)


func _process(delta: float) -> void:
	advance(delta)


func advance(delta: float) -> void:
	if clips.is_empty():
		return
	elapsed += maxf(delta, 0.0)
	_sample()


func current_animation() -> StringName:
	var frames: SpriteFrames = clips[clip]["frames"]
	var wanted := StringName(clip)
	if frames.get_frame_count(wanted) > 0:
		return wanted
	var names := frames.get_animation_names()
	return names[0] if not names.is_empty() else wanted


func _apply_clip_offset() -> void:
	if not is_instance_valid(sprite):
		return
	var canvas: Vector2i = clips[clip]["canvas"]
	var anchor: Vector2 = clips[clip]["anchor"]
	# Centred Sprite2D draws texture pixel p at (p - size/2 + offset); putting the
	# anchor on the node origin means offset = size/2 - anchor.
	sprite.offset = Vector2(canvas.x * 0.5 - anchor.x, canvas.y * 0.5 - anchor.y)


func _sample() -> void:
	if not is_instance_valid(sprite) or clips.is_empty():
		return
	var entry: Dictionary = clips[clip]
	var frames: SpriteFrames = entry["frames"]
	var anim := current_animation()
	var count := frames.get_frame_count(anim)
	if count <= 0:
		return
	var fps := maxf(frames.get_animation_speed(anim), 1.0)
	var looping := frames.get_animation_loop(anim)
	var index := int(floor(elapsed * fps))
	if index >= count:
		if looping:
			index = index % count
		else:
			clip = "idle"
			elapsed = 0.0
			_index = -1
			_apply_clip_offset()
			_sample()
			finished.emit()
			return
	if index != _index:
		_index = index
		sprite.texture = frames.get_frame_texture(anim, index)
