extends SceneTree

const COMBAT_SCENE := "res://guardian_assets/combat_rig/guardian_combat_visuals.tscn"
const COMBAT_TEXTURE := "res://guardian_assets/combat_rig/guardian_combat_character.png"
const MERCHANT_SCENE := "res://guardian_assets/merchant/guardian_merchant.tscn"
const MERCHANT_TEXTURE := "res://guardian_assets/merchant/guardian_merchant.png"
const REQUIRED_ANIMATIONS := [
	"idle_loop",
	"guard",
	"attack",
	"counter_attack",
	"hit",
	"death",
]

func _initialize() -> void:
	var failures := 0
	var texture := ResourceLoader.load(COMBAT_TEXTURE) as Texture2D
	if texture == null:
		push_error("Guardian combat texture failed to load")
		failures += 1
	else:
		var image := texture.get_image()
		if image == null or image.get_size() != Vector2i(1024, 1536):
			push_error("Guardian combat texture has the wrong dimensions")
			failures += 1
		elif not image.detect_alpha():
			push_error("Guardian combat texture lacks alpha")
			failures += 1
		else:
			for corner in [Vector2i(0, 0), Vector2i(1023, 0), Vector2i(0, 1535), Vector2i(1023, 1535)]:
				if image.get_pixelv(corner).a != 0.0:
					push_error("Guardian combat texture corner is not transparent: " + str(corner))
					failures += 1

	var packed_scene := ResourceLoader.load(COMBAT_SCENE) as PackedScene
	if packed_scene == null:
		push_error("Guardian combat scene failed to load")
		failures += 1
	else:
		var instance := packed_scene.instantiate()
		var rig := instance.get_node_or_null("Visuals/Prototype")
		var player := rig.get_node_or_null("AnimationPlayer") as AnimationPlayer if rig != null else null
		if rig == null or not rig.has_method("play_trigger"):
			push_error("Guardian combat trigger API is missing")
			failures += 1
		if player == null:
			push_error("Guardian combat AnimationPlayer is missing")
			failures += 1
		else:
			for animation_name in REQUIRED_ANIMATIONS:
				if not player.has_animation(animation_name):
					push_error("Guardian combat animation is missing: " + animation_name)
					failures += 1
		instance.free()

	var merchant_texture := ResourceLoader.load(MERCHANT_TEXTURE) as Texture2D
	if merchant_texture == null:
		push_error("Guardian merchant texture failed to load")
		failures += 1
	else:
		var merchant_image := merchant_texture.get_image()
		if merchant_image == null or merchant_image.get_size() != Vector2i(1024, 1536):
			push_error("Guardian merchant texture has the wrong dimensions")
			failures += 1
		elif not merchant_image.detect_alpha():
			push_error("Guardian merchant texture lacks alpha")
			failures += 1
		else:
			for corner in [Vector2i(0, 0), Vector2i(1023, 0), Vector2i(0, 1535), Vector2i(1023, 1535)]:
				if merchant_image.get_pixelv(corner).a != 0.0:
					push_error("Guardian merchant texture corner is not transparent: " + str(corner))
					failures += 1

	var merchant_scene := ResourceLoader.load(MERCHANT_SCENE) as PackedScene
	if merchant_scene == null:
		push_error("Guardian merchant scene failed to load")
		failures += 1
	else:
		var merchant := merchant_scene.instantiate()
		var merchant_player := merchant.get_node_or_null("AnimationPlayer") as AnimationPlayer
		if merchant_player == null:
			push_error("Guardian merchant AnimationPlayer is missing")
			failures += 1
		else:
			for animation_name in ["relaxed_loop", "die"]:
				if not merchant_player.has_animation(animation_name):
					push_error("Guardian merchant animation is missing: " + animation_name)
					failures += 1
		merchant.free()

	if failures == 0:
		print("GUARDIAN_COMBAT_OK")
	quit(failures)
