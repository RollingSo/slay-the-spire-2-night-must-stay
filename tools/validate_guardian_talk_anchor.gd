extends SceneTree

func _initialize() -> void:
	var scene := load("res://guardian_assets/combat_rig/guardian_combat_visuals.tscn") as PackedScene
	var instance := scene.instantiate()
	var sprite := instance.get_node("Visuals/Prototype/Rig/Root/Sprite") as Sprite2D
	var talk := instance.get_node("TalkPos") as Marker2D
	# Beak tip in the approved 1024x1536 portrait, before centered Sprite2D offset.
	var beak := Vector2(750, 330) - sprite.texture.get_size() / 2.0
	var node: Node2D = sprite
	while node != instance:
		beak = node.transform * beak
		node = node.get_parent() as Node2D
	var offset := talk.position - beak
	if not talk.unique_name_in_owner or offset.x < 4.0 or offset.x > 28.0 or absf(offset.y) > 12.0:
		push_error("Guardian talk anchor is not in front of the beak: " + str(offset))
		instance.free()
		quit(1)
		return
	print("GUARDIAN_TALK_ANCHOR_OK beak=", beak, " talk=", talk.position)
	instance.free()
	quit(0)
