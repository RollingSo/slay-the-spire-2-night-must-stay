extends Node2D
## Shared SpriteFrames combat prototype.
## Keeps the existing C# bridge contract: Visuals/Prototype.play_trigger(trigger).
## Resources are loaded from animations/<character>/main on first use.

const CONFIGS := {
	"guardian": {
		"dir": "res://animations/guardian/main/",
		"height": 308.331,
		"idle": "combat_idle",
		"map": {
			"Attack": "halberd_attack",
			"GuardCounter": "guard_counter",
			"Cast": "cast_brace",
			"PowerUp": "power_activate",
			"Hit": "hurt",
			"Dead": "death",
		},
	},
	"duchess": {
		"dir": "res://animations/duchess/main/",
		"height": 307.546886,
		"idle": "combat_idle",
		"map": {
			"Attack": "dagger_slash",
			"Block": "dodge_defend",
			"Cast": "cast",
			"Hit": "hurt",
			"Dead": "death",
			"TimeCommand": "time_command",
		},
	},
	"revenant": {
		"dir": "res://animations/revenant/main/",
		"height": 215.7527232,
		"idle": "combat_idle",
		"map": {
			"Attack": "claw_attack",
			"Cast": "lyre_command",
			"Prayer": "prayer_attack",
			"Call": "lyre_command",
			"Resonance": "lyre_command",
			"Hit": "hurt",
			"Dead": "death",
		},
	},
}

@export var character_key := "guardian"
@export var force_loop := false

var sprite: AnimatedSprite2D
var _frames_cache: Dictionary = {}
var _config: Dictionary = {}
var _return_to_idle := false
var _dead := false
var _card_motion := ""
var _card_motion_active := false
var _awaiting_native_attack := false
var _time_command_active := false
var _idle_frames: SpriteFrames
var _base_scale := 1.0
var _warned: Dictionary = {}

func _ready() -> void:
	_config = CONFIGS.get(character_key, CONFIGS["guardian"])
	sprite = AnimatedSprite2D.new()
	sprite.name = "Sprite"
	sprite.centered = true
	add_child(sprite)
	sprite.animation_finished.connect(_on_animation_finished)
	_idle_frames = _get_frames(_config["idle"])
	if _idle_frames == null:
		push_error("SequencePrototype: no idle frames for " + character_key)
		return
	var tex := _idle_frames.get_frame_texture(_idle_frames.get_animation_names()[0], 0)
	var used := tex.get_image().get_used_rect()
	var parent_node := get_parent() as CanvasItem
	var parent_scale: float = parent_node.global_scale.y if parent_node != null else 1.0
	_base_scale = float(_config["height"]) / maxf(1.0, float(used.size.y) * parent_scale)
	_play_idle()

func _get_frames(action: String) -> SpriteFrames:
	var key := String(action)
	if _frames_cache.has(key):
		return _frames_cache[key]
	var path: String = String(_config["dir"]) + key + ".res"
	if not ResourceLoader.exists(path):
		if not _warned.has(path):
			push_warning("SequencePrototype: missing " + path)
			_warned[path] = true
		return null
	var frames := load(path) as SpriteFrames
	if frames == null:
		push_warning("SequencePrototype: not SpriteFrames " + path)
		return null
	_frames_cache[key] = frames
	return frames

func _apply_frames(frames: SpriteFrames, loop: bool) -> void:
	if frames == null or sprite == null:
		return
	var anim: StringName = frames.get_animation_names()[0]
	sprite.stop()
	sprite.sprite_frames = frames
	sprite.animation = anim
	sprite.frame = 0
	if force_loop:
		frames.set_animation_loop(anim, true)
	elif loop:
		frames.set_animation_loop(anim, true)
	else:
		frames.set_animation_loop(anim, false)
	var canvas: Vector2i = frames.get_meta("canvas", Vector2i.ZERO)
	var anchor: Vector2 = frames.get_meta("anchor", Vector2.ZERO)
	if canvas != Vector2i.ZERO:
		sprite.offset = Vector2(canvas.x * 0.5 - anchor.x, canvas.y * 0.5 - anchor.y)
		sprite.scale = Vector2.ONE * _base_scale
	sprite.play(anim)

func _play_idle() -> void:
	_return_to_idle = false
	_dead = false
	_awaiting_native_attack = false
	_time_command_active = false
	_apply_frames(_idle_frames, true)

func _play_one_shot(action: String, loop := false) -> void:
	var frames := _get_frames(action)
	if frames == null:
		_play_idle()
		return
	_return_to_idle = not loop
	_time_command_active = action == "time_command"
	_apply_frames(frames, loop)

func play_trigger(trigger: String) -> void:
	if _dead and trigger != "Revive":
		return
	# The native Cast/Attack hook can fire after OnPlay; do not cut the clock cinematic.
	if _time_command_active and trigger in ["Attack", "Cast"]:
		return
	match trigger:
		"Idle", "Relaxed", "Revive":
			_play_idle()
		"Dead":
			_dead = true
			_return_to_idle = false
			_play_one_shot(String(_config["map"]["Dead"]), false)
		"Attack", "Cast":
			if character_key == "revenant":
				if _awaiting_native_attack:
					_awaiting_native_attack = false
					return
				_play_revenant_attack()
			elif _config["map"].has(trigger):
				_play_one_shot(String(_config["map"][trigger]))
		_:
			if _config["map"].has(trigger):
				_play_one_shot(String(_config["map"][trigger]))

func _attack_action_revenant() -> String:
	if _card_motion == "Prayer":
		return "prayer_attack"
	if _card_motion == "Call" or _card_motion == "Resonance":
		return "lyre_command"
	return "claw_attack"

func _play_revenant_attack() -> void:
	_play_one_shot(_attack_action_revenant())

func queue_attack_motion(motion: String) -> void:
	_card_motion = motion

func play_card_motion(motion: String) -> void:
	if _dead:
		return
	_card_motion = motion
	_card_motion_active = true
	_awaiting_native_attack = true
	_play_one_shot(_attack_action_revenant())

func clear_card_motion() -> void:
	_card_motion = ""
	_card_motion_active = false
	_awaiting_native_attack = false

func set_playback_mode(_mode: String) -> void:
	pass

func set_action_timing(_trigger: String, _wait_time: float) -> void:
	pass

func _on_animation_finished(_anim: StringName) -> void:
	_time_command_active = false
	if _return_to_idle and not _dead:
		_play_idle()

func set_phase_visual(opacity: float, shimmer: float) -> void:
	if sprite == null:
		return
	sprite.self_modulate = Color(1.0 - 0.22 * shimmer, 1.0 - 0.08 * shimmer, 1.0, opacity)
