extends Node
## 复仇者本体的 SpriteFrames 播放器。
##
## 与家人那套的区别：本体在战斗里由 `revenant_combat_rig.gd` 驱动的是一组静态姿势
## 贴图（idle/attack/hit/claw/prayer/death）+ 位移缩放，没有逐帧动画。这里把同一批
## 动作换成 v03 的逐帧 SpriteFrames，公开接口与 rig 保持一致，方便之后决定是否替换。
##
## 每个 clip 的画布与锚点存在资源 meta 里，Sprite2D 的 offset 由它们推出，
## 动作往任何方向展开都不会被裁掉（见 design/动画预览/revenant/基准_v03/使用说明.md）。

signal finished

const CLIP_FILES := ["idle", "claw_attack", "lyre_command", "prayer_attack", "hurt", "death"]
const DEFAULT_CLIP := "idle"

## 与 rig 的 _attack_animation() 保持同一套路由。
const ATTACK_ROUTES := {
	"Prayer": "prayer_attack",
	"Call": "lyre_command",
	"Resonance": "lyre_command",
}
const ATTACK_DEFAULT := "claw_attack"

var sprite: Sprite2D
var clips: Dictionary = {}
var clip := DEFAULT_CLIP
var elapsed := 0.0
var _index := -1
var _dead := false
var _attack_motion := ""


func configure(target: Sprite2D, directory: String) -> void:
	sprite = target
	clips.clear()
	for key in CLIP_FILES:
		var path: String = directory.path_join(key + ".res")
		if not ResourceLoader.exists(path):
			continue
		var frames := load(path) as SpriteFrames
		if frames == null:
			push_warning("Revenant animation is not SpriteFrames: " + path)
			continue
		# 资源里的动画名和文件名不一定一致（RV01 里叫 combat_idle），加载时解析一次，
		# 避免之后用错名字去查帧数时触发引擎报错。
		var names: PackedStringArray = frames.get_animation_names()
		var anim: String = key
		if not names.has(key) and not names.is_empty():
			anim = String(names[0])
		clips[key] = {
			"frames": frames,
			"anim": anim,
			"canvas": frames.get_meta("canvas", Vector2i(256, 256)),
			"anchor": frames.get_meta("anchor", Vector2(128, 240)),
		}
	if clips.is_empty():
		push_warning("No revenant animation resources under " + directory)
		return
	play_trigger("Idle")


func has_clip(key: String) -> bool:
	return clips.has(key)


## 与 rig 同名同语义的信封接口。
func play_trigger(trigger: String) -> void:
	match trigger:
		"Attack", "Cast":
			_play(attack_clip())
		"Call", "Resonance":
			_play(ATTACK_ROUTES["Call"])
		"Prayer":
			_play(ATTACK_ROUTES["Prayer"])
		"Hit":
			if not _dead:
				_play("hurt")
		"Dead":
			_dead = true
			_play("death")
		"Revive":
			_dead = false
			_play_idle()
		"Idle", "Relaxed":
			_play_idle()


func queue_attack_motion(motion: String) -> void:
	_attack_motion = motion


func play_card_motion(motion: String) -> void:
	if _dead:
		return
	_attack_motion = motion
	_play(attack_clip())


func clear_card_motion() -> void:
	_attack_motion = ""


func attack_clip() -> String:
	return ATTACK_ROUTES.get(_attack_motion, ATTACK_DEFAULT)


func _play_idle() -> void:
	_play(DEFAULT_CLIP)


func _play(key: String) -> void:
	if clips.is_empty():
		return
	if not clips.has(key):
		key = DEFAULT_CLIP
	if not clips.has(key):
		key = clips.keys()[0]
	clip = key
	elapsed = 0.0
	_index = -1
	_apply_clip_offset()
	_sample()


func _process(delta: float) -> void:
	advance(delta)


func advance(delta: float) -> void:
	if clips.is_empty():
		return
	elapsed += maxf(delta, 0.0)
	_sample()


func current_animation() -> StringName:
	if clips.is_empty():
		return &""
	return StringName(clips[clip]["anim"])


func _apply_clip_offset() -> void:
	if not is_instance_valid(sprite):
		return
	var canvas: Vector2i = clips[clip]["canvas"]
	var anchor: Vector2 = clips[clip]["anchor"]
	# 居中 Sprite2D 把像素 p 画在 (p - size/2 + offset)；让锚点落在节点原点即 offset = size/2 - anchor
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
			var was_death := clip == "death"
			clip = DEFAULT_CLIP if not was_death else "death"
			elapsed = 0.0
			_index = -1
			if not clips.has(clip):
				clip = DEFAULT_CLIP
			_apply_clip_offset()
			if clip == DEFAULT_CLIP:
				_sample()
			finished.emit()
			return
	if index != _index:
		_index = index
		sprite.texture = frames.get_frame_texture(anim, index)
